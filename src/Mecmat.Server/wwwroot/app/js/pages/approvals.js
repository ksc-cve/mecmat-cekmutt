import { h, clear } from '../dom.js';
import { icon } from '../icons.js';
import { t, enumLabel } from '../i18n.js';
import { api } from '../api.js';
import { nameOf } from '../store.js';
import { staticTable } from '../ui/grid.js';
import { toastError, toastOk, empty, promptBox, busy } from '../ui/feedback.js';
import { page, pageHead, refreshBadges } from '../shell.js';
import { baht, dateTime, relative } from '../fmt.js';
import { tone } from '../defs.js';

/** Route of an approval with who decided what; buttons only when it is this user's turn. */
export function approvalPanel(a, canDecide, reload) {
  const steps = h('ol', { class: 'ui-steps' });
  for (const s of a.steps) {
    const acts = a.actions.filter((x) => x.stepOrder === s.step);
    const done = a.status === 'Approved' || s.step < a.currentStep;
    const rejected = acts.some((x) => x.decision === 'Reject');
    const current = a.status === 'Pending' && s.step === a.currentStep;
    steps.append(h('li', { class: rejected ? 'rejected' : done ? 'done' : current ? 'current' : '' },
      h('span', { class: 'dot' }, done ? '✓' : s.step),
      h('div', null, h('div', null, s.approvers.map((p) => nameOf('people', p)).join(', '), h('span', { class: 'muted small' }, ` · ${enumLabel('StepRule', s.rule)}`)),
        acts.map((x) => h('div', { class: 'small' }, h('strong', null, enumLabel('ApprovalDecision', x.decision)), ' — ', nameOf('people', x.actorPersonId),
          x.onBehalfOfPersonId ? ` (${t('appr.onBehalf', { name: nameOf('people', x.onBehalfOfPersonId) })})` : '', ' · ', dateTime(x.at),
          x.comment ? h('div', { class: 'muted' }, '“', x.comment, '”') : null)))));
  }
  const decide = async (decision, btn) => {
    let comment = null;
    if (decision !== 'Approve') { comment = await promptBox(t(decision === 'Reject' ? 'appr.rejectReason' : 'appr.returnReason')); if (!comment) return; }
    await busy(btn, async () => { await api.post(`/api/approvals/${a.id}/decide`, { decision, comment }); toastOk(); refreshBadges(); reload(); });
  };
  return h('section', { class: 'ui-card' },
    h('header', null, h('h3', null, t('appr.route')), h('span', { class: `ui-badge ${tone.status(a.status)}` }, enumLabel('ApprovalStatus', a.status))),
    h('div', { class: 'body' }, steps,
      canDecide ? h('div', { class: 'ui-toolbar', style: { marginTop: '16px' } },
        h('button', { class: 'ui-btn primary', type: 'button', onclick: (e) => decide('Approve', e.currentTarget).catch(() => {}) }, icon('check'), enumLabel('ApprovalDecision', 'Approve')),
        h('button', { class: 'ui-btn', type: 'button', onclick: (e) => decide('Return', e.currentTarget).catch(() => {}) }, enumLabel('ApprovalDecision', 'Return')),
        h('button', { class: 'ui-btn danger', type: 'button', onclick: (e) => decide('Reject', e.currentTarget).catch(() => {}) }, enumLabel('ApprovalDecision', 'Reject'))) : null));
}

export async function approvalsPage() {
  const el = page('nav.approvals');
  let d;
  try { d = await api.get('/api/approvals/inbox'); } catch (e) { toastError(e); return; }
  const link = (x) => (x.subjectType === 'MoneyRequest' ? `#/finance/requests/${x.subjectId}` : `#/bookings?focus=${x.subjectId}`);
  const quick = async (x, decision, btn) => {
    let comment = null;
    if (decision !== 'Approve') { comment = await promptBox(t('appr.rejectReason')); if (!comment) return; }
    await busy(btn, async () => { await api.post(`/api/approvals/${x.id}/decide`, { decision, comment }); toastOk(); refreshBadges(); approvalsPage(); });
  };
  el.append(pageHead(t('nav.approvals')), h('p', { class: 'small muted' }, t('appr.intro')),
    h('h3', null, t('appr.waiting')),
    staticTable([
      { key: 'subjectType', label: 'f.subjectType', render: (x) => t('appr.subject.' + x.subjectType) },
      { key: 'summary', label: 'f.description', render: (x) => (x.subjectType === 'MoneyRequest' ? `${enumLabel('MoneyRequestKind', x.summary?.kind)} · ${x.summary?.purpose}` : `${x.summary?.title} · ${nameOf('resources', x.summary?.resourceId)} · ${dateTime(x.summary?.startAt)}`) },
      { key: 'amount', label: 'f.amount', render: (x) => (x.summary?.totalAmount != null ? baht(x.summary.totalAmount) : '') },
      { key: 'requesterPersonId', type: 'ref', ref: 'people' },
      { key: 'submittedAt', label: 'f.submittedAt', render: (x) => relative(x.submittedAt) },
      { key: 'step', label: 'f.currentStep', render: (x) => `${x.currentStep}/${x.steps}${x.onBehalfOf ? ' · ' + t('appr.onBehalf', { name: nameOf('people', x.onBehalfOf) }) : ''}` },
      { key: 'go', label: '', render: (x) => h('span', { class: 'nowrap' },
        h('a', { class: 'ui-btn sm', href: link(x) }, t('common.open')), ' ',
        h('button', { class: 'ui-btn sm primary', type: 'button', onclick: (e) => quick(x, 'Approve', e.currentTarget).catch(() => {}) }, icon('check')),
        ' ', h('button', { class: 'ui-btn sm danger', type: 'button', 'aria-label': enumLabel('ApprovalDecision', 'Reject'), onclick: (e) => quick(x, 'Reject', e.currentTarget).catch(() => {}) }, icon('x'))) },
    ], d.waiting, { emptyText: t('appr.nothing') }),
    h('h3', { style: { marginTop: '24px' } }, t('appr.mine')),
    staticTable([
      { key: 'subjectType', label: 'f.subjectType', render: (x) => t('appr.subject.' + x.subjectType) },
      { key: 'subjectId', label: 'f.code', render: (x) => h('a', { href: link(x) }, x.subjectId) },
      { key: 'submittedAt', type: 'datetime' }, { key: 'status', type: 'enum', enum: 'ApprovalStatus', tone: tone.status }], d.submitted));
}
