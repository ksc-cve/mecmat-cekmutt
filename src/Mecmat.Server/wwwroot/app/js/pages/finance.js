import { h, clear } from '../dom.js';
import { icon } from '../icons.js';
import { t, enumLabel, pick, getLang } from '../i18n.js';
import { r, api } from '../api.js';
import { store, can, canAny, nameOf } from '../store.js';
import { DEFS, tone } from '../defs.js';
import { staticTable, tabs, dataGrid } from '../ui/grid.js';
import { toastError, toastOk, empty, promptBox, confirmBox, modal, busy } from '../ui/feedback.js';
import { openForm, buildForm } from '../ui/form.js';
import { refPicker } from '../ui/picker.js';
import { page, pageHead } from '../shell.js';
import { editRecord, childGrid, backLink, afterSave, historyButton } from './common.js';
import { money, baht, date, dateTime, parseMoney, fiscalYear, todayBkk, isoDate } from '../fmt.js';
import { setQuery } from '../router.js';
import { approvalPanel } from './approvals.js';

const moneyCell = (v) => h('span', { class: `money ${v < 0 ? 'neg' : ''}` }, money(v));

export async function financePage(_, query) {
  const el = page('nav.finance');
  const tab = query.tab || 'accounts';
  const items = [{ key: 'accounts', label: 'fin.tab.accounts' }, { key: 'requests', label: 'fin.tab.requests' }, { key: 'advances', label: 'fin.tab.advances' }, { key: 'budget', label: 'fin.tab.budget' }];
  el.append(pageHead(t('nav.finance'),
    h('button', { class: 'ui-btn primary', type: 'button', onclick: () => requestForm() }, icon('plus'), t('fin.newRequest'))),
  h('p', { class: 'small muted' }, t('fin.intro')));
  const body = h('div');
  const bar = tabs(items, tab, (k) => { setQuery({ tab: k }); bar.querySelectorAll('button').forEach((b, i) => b.setAttribute('aria-selected', String(items[i].key === k))); draw(k); });
  el.append(bar, body);
  const draw = (k) => {
    clear(body).append(h('p', { class: 'muted' }, t('common.loading')));
    ({ accounts: accountsTab, requests: requestsTab, advances: advancesTab, budget: budgetTab }[k] || accountsTab)(body, query).catch((e) => { toastError(e); clear(body); });
  };
  draw(tab);
}

async function accountsTab(body) {
  const data = await api.get('/api/finance/tree');
  const byParent = new Map();
  for (const a of data.accounts) {
    const pid = data.accounts.some((x) => x.id === a.parentAccountId) ? a.parentAccountId : null;
    if (!byParent.has(pid)) byParent.set(pid, []);
    byParent.get(pid).push(a);
  }
  const collapsed = new Set(JSON.parse(sessionStorage.getItem('mecmat.fin.collapsed') || '[]'));
  const tbody = h('tbody');
  const drawRows = () => {
    clear(tbody);
    const walk = (pid, depth) => {
      for (const a of byParent.get(pid) || []) {
        const kids = byParent.get(a.id)?.length;
        const usedPct = a.budget ? Math.round((a.spentThisYear / a.budget) * 100) : null;
        const tr = h('tr', { class: `tree-row clickable ${kids ? 'parent' : ''}`, onclick: (e) => { if (!e.target.closest('button')) location.hash = `#/finance/accounts/${a.id}`; } },
          h('td', null, kids ? h('button', { class: 'tree-toggle', type: 'button', 'aria-label': t('common.more'), onclick: () => { collapsed.has(a.id) ? collapsed.delete(a.id) : collapsed.add(a.id); sessionStorage.setItem('mecmat.fin.collapsed', JSON.stringify([...collapsed])); drawRows(); } }, collapsed.has(a.id) ? '▸' : '▾') : h('span', { class: 'tree-toggle' }, '·'),
            h('span', { class: 'muted small' }, a.code + ' '), getLang() === 'en' ? a.nameEn || a.nameTh : a.nameTh),
          h('td', null, a.budgetCategory ? enumLabel('BudgetCategory', a.budgetCategory) : enumLabel('AccountKind', a.kind)),
          h('td', { class: 'num' }, moneyCell(a.balance)), h('td', { class: 'num' }, moneyCell(a.committed)), h('td', { class: 'num' }, moneyCell(a.available)),
          h('td', { class: 'num' }, a.budget ? moneyCell(a.budget) : ''),
          h('td', { style: { minWidth: '120px' } }, usedPct != null ? h('div', null, h('div', { class: `ui-bar ${usedPct > 100 ? 'over' : usedPct > 85 ? 'warn' : ''}` }, h('span', { style: { width: Math.min(100, usedPct) + '%' } })), h('span', { class: 'small muted' }, `${money(a.spentThisYear)} (${usedPct}%)`)) : a.spentThisYear ? h('span', { class: 'small muted' }, money(a.spentThisYear)) : ''));
        tr.style.setProperty('--depth', depth);
        tbody.append(tr);
        if (!collapsed.has(a.id)) walk(a.id, depth + 1);
      }
    };
    walk(null, 0);
    if (!data.accounts.length) tbody.append(h('tr', null, h('td', { class: 'empty', colSpan: 7 }, t('fin.noAccounts'))));
  };
  drawRows();
  clear(body).append(
    h('div', { class: 'ui-toolbar' }, h('span', { class: 'grow small muted' }, fiscalYear(data.fiscalYear)),
      data.canRecordIncome ? h('button', { class: 'ui-btn', type: 'button', onclick: () => incomeForm(null, () => accountsTab(body)) }, icon('download'), t('fin.recordIncome')) : null,
      data.canCreateRoot ? h('button', { class: 'ui-btn', type: 'button', onclick: () => editRecord('accounts', null, { defaults: { kind: 'FundingSource' }, onSaved: () => accountsTab(body) }) }, icon('plus'), t('fin.newAccount')) : null),
    h('div', { class: 'ui-table-wrap' }, h('table', { class: 'ui-table' },
      h('thead', null, h('tr', null, ['f.name', 'f.category', 'f.balance', 'f.committed', 'f.available', 'f.budget', 'f.spent'].map((k, i) => h('th', { class: i >= 2 && i <= 5 ? 'num' : '' }, t(k))))), tbody)),
    h('p', { class: 'small muted', style: { marginTop: '8px' } }, t('fin.treeHint')));
}

function incomeForm(accountId, done) {
  return openForm({
    title: t('fin.recordIncome'),
    fields: [{ key: 'accountId', type: 'ref', ref: 'accounts', required: true }, { key: 'amount', type: 'money', required: true }, { key: 'entryDate', type: 'date' },
      { key: 'description', required: true, span2: true }, { key: 'counterpartyOrganizationId', type: 'ref', ref: 'organizations' }, { key: 'counterpartyText' }, { key: 'referenceText' }],
    values: { accountId, entryDate: isoDate(todayBkk().y, todayBkk().m, todayBkk().d) },
    onSubmit: async (v) => { await api.post('/api/finance/income', v); toastOk(); done?.(); },
  });
}

export async function accountPage({ id }) {
  const el = page('nav.finance');
  let acc, tree, stmt;
  try {
    [acc, tree, stmt] = await Promise.all([r('accounts').get(id).catch(() => null), api.get('/api/finance/tree'), api.get(`/api/finance/accounts/${id}/statement`)]);
  } catch (e) { toastError(e); el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const node = tree.accounts.find((a) => a.id === id) || {};
  const kids = tree.accounts.filter((a) => a.parentAccountId === id);
  const name = acc ? pick(acc, 'name') : nameOf('accounts', id);
  el.append(h('div', { class: 'detail-head' }, h('div', { class: 'grow' }, backLink('#/finance'), h('h1', null, name),
    h('div', { class: 'muted' }, [acc?.code || node.code, acc?.projectId ? nameOf('projects', acc.projectId) : null, acc?.custodianPersonId ? `${t('f.custodianPersonId')}: ${nameOf('people', acc.custodianPersonId)}` : null].filter(Boolean).join(' · '))),
  h('div', { class: 'ui-toolbar' },
    node.canRequest ? h('button', { class: 'ui-btn primary', type: 'button', onclick: () => requestForm({ accountId: id }) }, icon('plus'), t('fin.newRequest')) : null,
    node.canEdit && node.isLeaf ? h('button', { class: 'ui-btn', type: 'button', onclick: () => incomeForm(id, () => accountPage({ id })) }, icon('download'), t('fin.recordIncome')) : null,
    acc?._can?.edit ? h('button', { class: 'ui-btn', type: 'button', onclick: () => editRecord('accounts', acc, { onSaved: () => accountPage({ id }) }) }, icon('edit'), t('common.edit')) : null,
    node.canEdit && (stmt.lines.length === 0 || kids.length) ? h('button', { class: 'ui-btn', type: 'button', onclick: () => editRecord('accounts', null, { defaults: { parentAccountId: id, kind: 'BudgetCategory', projectId: acc?.projectId, code: (acc?.code || '') + '-' }, onSaved: () => accountPage({ id }) }) }, icon('plus'), t('fin.newSubAccount')) : null,
    historyButton('Account', id))));
  el.append(h('div', { class: 'kv-cards' },
    kvc(t('f.balance'), money(node.balance)), kvc(t('f.committed'), money(node.committed)), kvc(t('f.available'), money(node.available)),
    kvc(t('f.budget') + ' ' + fiscalYear(tree.fiscalYear), node.budget ? money(node.budget) : '—'), kvc(t('f.spent'), money(node.spentThisYear))));
  if (kids.length) el.append(h('section', { class: 'section' }, h('h3', null, t('fin.subAccounts')),
    staticTable([{ key: 'code' }, { key: 'nameTh', label: 'f.name', get: (a) => (getLang() === 'en' ? a.nameEn || a.nameTh : a.nameTh) }, { key: 'balance', type: 'money' }, { key: 'available', type: 'money' }, { key: 'budget', type: 'money' }, { key: 'spentThisYear', type: 'money', label: 'f.spent' }],
      kids, { onRowClick: (a) => (location.hash = `#/finance/accounts/${a.id}`) })));
  if (acc) el.append(childGrid('account-budgets', 'accountId', id, { canAdd: !!node.canEdit, title: 'fin.budgets' }));
  el.append(h('section', { class: 'section' }, h('h3', null, t('fin.statement')),
    staticTable([
      { key: 'entryDate', type: 'date' }, { key: 'kind', type: 'enum', enum: 'LedgerKind' }, { key: 'description' },
      { key: 'counterparty', label: 'f.counterpartyText', get: (l) => (l.counterpartyPersonId ? nameOf('people', l.counterpartyPersonId) : l.counterpartyOrganizationId ? nameOf('organizations', l.counterpartyOrganizationId) : l.counterpartyText) },
      { key: 'delta', type: 'money' }, { key: 'balance', type: 'money' },
      { key: 'actions', label: '', render: (l) => h('span', null,
        l.moneyRequestId ? h('a', { class: 'ui-btn ghost sm', href: `#/finance/requests/${l.moneyRequestId}` }, l.moneyRequestId) : null,
        stmt.canReverse && l.kind !== 'Reversal' && !stmt.reversed.includes(l.id) ? h('button', { class: 'ui-btn ghost sm', type: 'button', onclick: async () => {
          const reason = await promptBox(t('fin.reverseReason'), { title: t('fin.reverse') });
          if (!reason) return;
          try { await api.post(`/api/finance/ledger/${l.id}/reverse`, { reason }); toastOk(); accountPage({ id }); } catch (e) { toastError(e); }
        } }, icon('restore'), t('fin.reverse')) : stmt.reversed.includes(l.id) ? h('span', { class: 'ui-badge maroon' }, t('fin.reversed')) : null) },
    ], stmt.lines.slice().reverse())));
}
const kvc = (k, v) => h('div', { class: 'ui-card' }, h('div', { class: 'k' }, k), h('div', { class: 'v money' }, v));

async function requestsTab(body, query) {
  const mine = query.mine === '1';
  const data = await api.get(`/api/finance/requests${mine ? '?mine=true' : ''}`);
  clear(body).append(
    h('div', { class: 'ui-toolbar' },
      h('div', { class: 'ui-tabs', style: { margin: 0, border: 0 } },
        h('button', { type: 'button', 'aria-selected': String(!mine), onclick: () => { setQuery({ mine: '' }); requestsTab(body, {}); } }, t('common.all')),
        h('button', { type: 'button', 'aria-selected': String(mine), onclick: () => { setQuery({ mine: '1' }); requestsTab(body, { mine: '1' }); } }, t('common.mine')))),
    staticTable([
      { key: 'id', label: 'f.code' }, { key: 'createdAt', type: 'date', label: 'f.date' }, { key: 'kind', type: 'enum', enum: 'MoneyRequestKind' },
      { key: 'purpose' }, { key: 'requesterPersonId', type: 'ref', ref: 'people' }, { key: 'accountId', type: 'ref', ref: 'accounts' },
      { key: 'totalAmount', type: 'money' }, { key: 'status', type: 'enum', enum: 'MoneyRequestStatus', tone: tone.status }],
    data.rows, { onRowClick: (x) => (location.hash = `#/finance/requests/${x.id}`), emptyText: t('fin.noRequests') }));
}

async function advancesTab(body) {
  const rows = await api.get('/api/finance/advances');
  clear(body).append(h('p', { class: 'small muted' }, t('fin.advancesHint')),
    staticTable([{ key: 'id', label: 'f.code' }, { key: 'requesterPersonId', type: 'ref', ref: 'people' }, { key: 'purpose' }, { key: 'accountId', type: 'ref', ref: 'accounts' },
      { key: 'outstanding', type: 'money' }, { key: 'advanceDueDate', type: 'date' },
      { key: 'overdue', label: 'f.status', render: (a) => (a.overdue ? h('span', { class: 'ui-badge maroon' }, t('fin.overdue')) : h('span', { class: 'ui-badge yellow' }, t('fin.outstanding'))) },
      { key: 'clear', label: '', render: (a) => (a.requesterPersonId === store.me.person.id ? h('button', { class: 'ui-btn sm primary', type: 'button', onclick: () => requestForm({ kind: 'AdvanceClearance', relatedAdvanceId: a.id, accountId: a.accountId, purpose: t('fin.clearOf') + ' ' + a.purpose }) }, t('fin.clear')) : '') }],
    rows, { emptyText: t('fin.noAdvances') }));
}

async function budgetTab(body, query) {
  const cur = (await api.get('/api/finance/tree')).fiscalYear;
  const fy = Number(query.fy) || cur;
  const data = await api.get(`/api/finance/budget?fy=${fy}`);
  const sel = h('select', { class: 'ui-select', style: { maxWidth: '200px' }, 'aria-label': t('f.fiscalYear'), onchange: () => { setQuery({ fy: sel.value }); budgetTab(body, { fy: sel.value }); } },
    [cur + 1, cur, cur - 1, cur - 2, cur - 3].map((y) => h('option', { value: y }, fiscalYear(y))));
  sel.value = fy;
  const rows = data.rows.filter((x) => x.budget || x.spent || x.income).map((x) => ({ ...x, remain: x.budget - x.spent, pct: x.budget ? Math.round((x.spent / x.budget) * 100) : null }));
  clear(body).append(h('div', { class: 'ui-toolbar' }, sel, h('span', { class: 'small muted' }, t('fin.budgetHint'))),
    staticTable([{ key: 'code' }, { key: 'nameTh', label: 'f.name', get: (a) => (getLang() === 'en' ? a.nameEn || a.nameTh : a.nameTh) },
      { key: 'budgetCategory', type: 'enum', enum: 'BudgetCategory' }, { key: 'budget', type: 'money' }, { key: 'income', type: 'money' }, { key: 'spent', type: 'money' },
      { key: 'remain', type: 'money', label: 'fin.remaining' }, { key: 'pct', label: 'fin.usedPct', num: true, render: (x) => (x.pct == null ? '' : `${x.pct}%`) }], rows, { onRowClick: (a) => (location.hash = `#/finance/accounts/${a.id}`) }));
}

/** Request form with numeric lines; the total is always the sum of the lines (ADR 0004). */
export async function requestForm(values = {}, existing = null) {
  const kinds = ['Reimbursement', 'Payment', 'Advance', 'Transfer', 'AdvanceClearance'];
  const tree = await api.get('/api/finance/tree');
  const requestable = new Set(tree.accounts.filter((a) => a.canRequest).map((a) => a.id));
  if (!requestable.size) { toastError(new Error(t('fin.noRequestable'))); return; }
  const leaf = new Set(tree.accounts.filter((a) => a.isLeaf).map((a) => a.id));
  const head = buildForm([
    { key: 'kind', type: 'enum', enum: 'MoneyRequestKind', values: values.kind === 'AdvanceClearance' ? ['AdvanceClearance'] : kinds.slice(0, 4), required: true },
    { key: 'accountId', type: 'ref', ref: 'accounts', required: true, filter: (a) => requestable.has(a.id), readonly: values.kind === 'AdvanceClearance' },
    { key: 'toAccountId', type: 'ref', ref: 'accounts', filter: (a) => leaf.has(a.id), show: (v) => v.kind === 'Transfer' },
    { key: 'purpose', required: true, span2: true },
    { key: 'payeePersonId', type: 'ref', ref: 'people', show: (v) => v.kind === 'Payment' || v.kind === 'Reimbursement' },
    { key: 'payeeOrganizationId', type: 'ref', ref: 'organizations', show: (v) => v.kind === 'Payment' }, { key: 'payeeText', show: (v) => v.kind === 'Payment' },
    { key: 'neededBy', type: 'date' }, { key: 'advanceDueDate', type: 'date', show: (v) => v.kind === 'Advance', hint: 'fin.advanceDueHint' },
  ], { kind: 'Reimbursement', ...values, ...(existing || {}) });

  let lines = existing?.lines?.map((l) => ({ ...l })) || [{ description: '', qty: 1, unitPrice: 0, budgetCategory: null }];
  const tbody = h('tbody');
  const total = h('strong', { class: 'money' });
  const avail = h('span', { class: 'small muted' });
  const drawLines = () => {
    clear(tbody);
    lines.forEach((l, i) => {
      const d = h('input', { class: 'ui-input', value: l.description, 'aria-label': t('f.description'), oninput: (e) => (l.description = e.target.value) });
      const q = h('input', { class: 'ui-input num', type: 'number', min: 0, step: 'any', value: l.qty, 'aria-label': t('f.qty'), oninput: (e) => { l.qty = Number(e.target.value); sum(); } });
      const u = h('input', { class: 'ui-input num', inputMode: 'decimal', value: money(l.unitPrice), 'aria-label': t('f.unitPrice'), oninput: (e) => { l.unitPrice = parseMoney(e.target.value) ?? 0; sum(); } });
      const c = h('select', { class: 'ui-select', 'aria-label': t('f.budgetCategory'), onchange: (e) => (l.budgetCategory = e.target.value || null) },
        h('option', { value: '' }, '—'), ['Compensation', 'Operating', 'Materials', 'Equipment', 'InstitutionFee', 'Other'].map((v) => h('option', { value: v }, enumLabel('BudgetCategory', v))));
      c.value = l.budgetCategory || '';
      tbody.append(h('tr', null, h('td', null, d), h('td', { style: { width: '90px' } }, q), h('td', { style: { width: '140px' } }, u), h('td', { style: { width: '170px' } }, c),
        h('td', { class: 'num money' }, money(Math.round((l.qty || 0) * (l.unitPrice || 0)))),
        h('td', { class: 'actions' }, lines.length > 1 ? h('button', { class: 'ui-btn ghost sm', type: 'button', 'aria-label': t('common.remove'), onclick: () => { lines.splice(i, 1); drawLines(); } }, icon('x')) : null)));
    });
    sum();
  };
  const sum = () => {
    const s = lines.reduce((a, l) => a + Math.round((l.qty || 0) * (l.unitPrice || 0)), 0);
    total.textContent = baht(s);
    [...tbody.rows].forEach((tr, i) => (tr.cells[4].textContent = money(Math.round((lines[i].qty || 0) * (lines[i].unitPrice || 0)))));
    const accId = head.controls.accountId.get();
    const node = tree.accounts.find((a) => a.id === accId);
    avail.textContent = node ? `${t('f.available')}: ${baht(node.available)}` : '';
  };
  head.el.addEventListener('change', sum);
  head.el.addEventListener('focusout', sum);
  drawLines();

  const m = modal({ title: existing ? t('fin.editRequest') : t('fin.newRequest'), size: 'wide' });
  m.body.append(head.el, h('h4', { style: { margin: '18px 0 8px' } }, t('f.lines')),
    h('div', { class: 'ui-table-wrap lines-table' }, h('table', { class: 'ui-table' },
      h('thead', null, h('tr', null, ['f.description', 'f.qty', 'f.unitPrice', 'f.budgetCategory', 'f.amount', ''].map((k) => h('th', null, k ? t(k) : '')))), tbody)),
    h('div', { class: 'ui-toolbar', style: { marginTop: '8px' } },
      h('button', { class: 'ui-btn sm', type: 'button', onclick: () => { lines.push({ description: '', qty: 1, unitPrice: 0, budgetCategory: null }); drawLines(); } }, icon('plus'), t('fin.addLine')),
      h('div', { class: 'grow' }), avail, h('span', null, t('common.total') + ': '), total),
    h('p', { class: 'small muted' }, t('fin.attachHint')));
  const save = async (submit) => {
    if (!head.validate()) return;
    if (lines.some((l) => !l.description.trim())) { toastError(new Error(t('fin.lineDescRequired'))); return; }
    const body = { ...head.values(), relatedAdvanceId: values.relatedAdvanceId || existing?.relatedAdvanceId, lines: lines.map((l) => ({ description: l.description.trim(), qty: l.qty, unitPrice: l.unitPrice, budgetCategory: l.budgetCategory })), syncStamp: existing?.syncStamp };
    try {
      const res = existing ? await api.put(`/api/finance/requests/${existing.id}`, body) : await api.post('/api/finance/requests', body);
      if (submit) await api.post(`/api/finance/requests/${res.id}/submit`);
      toastOk(submit ? t('fin.submitted') : t('fin.draftSaved'));
      m.close();
      location.hash = `#/finance/requests/${res.id}`;
    } catch (e) { toastError(e); }
  };
  m.footer.append(h('button', { class: 'ui-btn', type: 'button', onclick: () => m.close() }, t('common.cancel')),
    h('button', { class: 'ui-btn', type: 'button', onclick: (e) => busy(e.target, () => save(false)) }, t('fin.saveDraft')),
    h('button', { class: 'ui-btn primary', type: 'button', onclick: (e) => busy(e.target, () => save(true)) }, icon('send'), t('fin.saveSubmit')));
}

export async function requestPage({ id }) {
  const el = page('nav.finance');
  let d;
  try { d = await api.get(`/api/finance/requests/${id}`); } catch (e) { toastError(e); el.append(empty(t('errors.notFound'))); return; }
  const q = d.request;
  const reload = () => requestPage({ id });
  el.append(h('div', { class: 'detail-head' }, h('div', { class: 'grow' },
    backLink('#/finance?tab=requests'),
    h('h1', null, `${enumLabel('MoneyRequestKind', q.kind)} · ${baht(q.totalAmount)}`),
    h('div', { class: 'muted' }, `${q.id} · ${q.purpose}`),
    h('div', { class: 'ui-toolbar', style: { marginTop: '8px' } }, h('span', { class: `ui-badge ${tone.status(q.status)}` }, enumLabel('MoneyRequestStatus', q.status)))),
  h('div', { class: 'ui-toolbar' },
    d.can.edit ? h('button', { class: 'ui-btn', type: 'button', onclick: () => requestForm({ kind: q.kind, relatedAdvanceId: q.relatedAdvanceId }, { ...q, lines: d.lines }) }, icon('edit'), t('common.edit')) : null,
    d.can.submit ? h('button', { class: 'ui-btn primary', type: 'button', onclick: (e) => busy(e.target, async () => { await api.post(`/api/finance/requests/${id}/submit`); toastOk(t('fin.submitted')); reload(); }) }, icon('send'), t('common.submit')) : null,
    d.can.settle ? h('button', { class: 'ui-btn primary', type: 'button', onclick: () => openForm({
      title: t('fin.settle'), submitText: t('fin.settle'),
      fields: [{ key: 'entryDate', type: 'date', required: true }, { key: 'referenceText', hint: 'fin.settleRefHint' }],
      values: { entryDate: isoDate(todayBkk().y, todayBkk().m, todayBkk().d) },
      extra: h('p', { class: 'small muted' }, t('fin.settleHint')),
      onSubmit: async (v) => { await api.post(`/api/finance/requests/${id}/settle`, v); toastOk(); reload(); },
    }) }, icon('check'), t('fin.settle')) : null,
    d.can.cancel ? h('button', { class: 'ui-btn danger', type: 'button', onclick: async () => { if (await confirmBox(t('fin.cancelConfirm'), { danger: true })) { try { await api.post(`/api/finance/requests/${id}/cancel`); reload(); } catch (e) { toastError(e); } } } }, t('fin.cancelRequest')) : null,
    historyButton('MoneyRequest', id))));

  const info = h('dl', { class: 'ui-dl' });
  const add = (k, v) => { if (v !== null && v !== undefined && v !== '') info.append(h('dt', null, t(k)), h('dd', null, v)); };
  add('f.requesterPersonId', nameOf('people', q.requesterPersonId));
  add('f.accountId', h('a', { href: `#/finance/accounts/${q.accountId}` }, nameOf('accounts', q.accountId)));
  add('f.toAccountId', q.toAccountId ? nameOf('accounts', q.toAccountId) : null);
  add('f.projectId', q.projectId ? h('a', { href: `#/projects/${q.projectId}` }, nameOf('projects', q.projectId)) : null);
  add('f.payeePersonId', q.payeePersonId ? nameOf('people', q.payeePersonId) : null);
  add('f.payeeOrganizationId', q.payeeOrganizationId ? nameOf('organizations', q.payeeOrganizationId) : null);
  add('f.payeeText', q.payeeText);
  add('f.neededBy', date(q.neededBy));
  add('f.advanceDueDate', date(q.advanceDueDate));
  add('f.relatedAdvanceId', q.relatedAdvanceId ? h('a', { href: `#/finance/requests/${q.relatedAdvanceId}` }, q.relatedAdvanceId) : null);
  add('f.available', ['Draft', 'Returned', 'InApproval', 'Submitted'].includes(q.status) ? baht(d.available) : null);
  add('f.createdAt', dateTime(q.createdAt));

  const attach = h('div');
  const drawAttachments = async () => {
    const list = (await r('attachments').list({ ownerType: 'MoneyRequest', ownerId: id })).rows;
    const input = h('input', { type: 'file', class: 'sr-only', id: 'att-file', onchange: async (e) => {
      try { await api.upload(e.target.files[0], { ownerType: 'MoneyRequest', ownerId: id }); toastOk(); drawAttachments(); } catch (err) { toastError(err); }
    } });
    clear(attach).append(
      list.length ? h('ul', { class: 'small' }, list.map((a) => h('li', null, h('a', { href: `/api/files/${a.fileBlobId}`, target: '_blank' }, a.label || a.fileBlobId)))) : h('p', { class: 'small muted' }, t('common.noFile')),
      q.requesterPersonId === store.me.person.id && q.status !== 'Settled' ? h('label', { class: 'ui-btn sm', for: 'att-file' }, icon('upload'), t('fin.attach')) : null, input);
  };
  drawAttachments().catch(toastError);

  el.append(h('div', { class: 'ui-grid c2' },
    h('div', null,
      h('section', { class: 'ui-card section' }, h('header', null, h('h3', null, t('common.details'))), h('div', { class: 'body' }, info)),
      h('section', { class: 'ui-card section' }, h('header', null, h('h3', null, t('fin.attachments'))), h('div', { class: 'body' }, attach))),
    h('div', null,
      d.approval ? approvalPanel(d.approval, d.can.decide, reload) : h('section', { class: 'ui-card' }, h('div', { class: 'body small muted' }, t('fin.notSubmitted'))))),
  h('section', { class: 'section' }, h('h3', null, t('f.lines')),
    staticTable([{ key: 'description' }, { key: 'qty', num: true }, { key: 'unitPrice', type: 'money' }, { key: 'budgetCategory', type: 'enum', enum: 'BudgetCategory' }, { key: 'amount', type: 'money' }], d.lines)));
}
