import { h, clear } from '../dom.js';
import { icon } from '../icons.js';
import { t, enumLabel, pick } from '../i18n.js';
import { r, api } from '../api.js';
import { store, can, canAny, nameOf, personName } from '../store.js';
import { DEFS, tone } from '../defs.js';
import { dataGrid, staticTable, tabs } from '../ui/grid.js';
import { toastError, toastOk, empty, modal, confirmBox, busy } from '../ui/feedback.js';
import { openForm } from '../ui/form.js';
import { page, pageHead } from '../shell.js';
import { editRecord, childGrid, crudPage } from './common.js';
import { dateTime, date, relative, money } from '../fmt.js';
import { ENUMS } from '../enums.js';
import { setQuery } from '../router.js';

const MODULES = ['people', 'sensitive', 'organizations', 'users', 'approvals-config', 'publications', 'projects', 'finance', 'assets', 'resources', 'bookings', 'tasks', 'meetings', 'documents', 'cms', 'inbox', 'dashboard', 'audit'];

export async function usersPage(_, query) {
  const el = page('nav.users');
  if (!can('users', 'View')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  let d;
  try { d = await api.get('/api/users'); } catch (e) { toastError(e); return; }
  const reload = () => usersPage(_, query);
  const editable = can('users', 'Edit');
  const roleOpts = d.roles.map((x) => x.key);
  const roleName = (k) => { const ro = d.roles.find((x) => x.key === k); return ro ? pick(ro, 'name') : k; };
  const showOtp = (otp, user) => {
    const m = modal({ title: t('users.otpTitle'), size: 'narrow' });
    m.body.append(h('p', null, t('users.otpIntro', { user })), h('div', { class: 'code-box' }, otp), h('p', { class: 'small muted' }, t('users.otpHint')));
    m.footer.append(h('button', { class: 'ui-btn', type: 'button', onclick: () => { navigator.clipboard?.writeText(otp); toastOk(t('common.copied')); } }, t('common.copy')),
      h('button', { class: 'ui-btn primary', type: 'button', onclick: () => m.close() }, t('common.close')));
    m.onClose = reload;
  };
  const rolesField = (values) => ({ key: 'roleKeys', label: 'f.roles', type: 'enum', values: roleOpts.filter((k) => k !== 'admin' || store.me.user.isAdmin), required: true });
  const actions = (u) => {
    if (!editable) return [];
    const b = [];
    if (u.status === 'PendingApproval') {
      b.push(h('button', { class: 'ui-btn sm primary', type: 'button', onclick: () => openForm({
        title: t('users.approve') + ' · ' + u.username,
        fields: [rolesField(), { key: 'expiresAt', type: 'date', hint: 'users.expiryHint' }, { key: 'linkPersonId', type: 'ref', ref: 'people', label: 'users.linkPerson', hint: 'users.linkHint' }],
        values: { roleKeys: ['MasterStudent', 'PhdStudent', 'BachelorStudent', 'Intern'].includes(u.person?.category) ? 'student' : 'researcher' },
        onSubmit: async (v) => { await api.post(`/api/users/${u.id}/approve`, { roleKeys: [v.roleKeys], expiresAt: v.expiresAt, linkPersonId: v.linkPersonId }); toastOk(); reload(); },
      }) }, t('users.approve')));
      b.push(h('button', { class: 'ui-btn sm danger', type: 'button', onclick: async () => { if (await confirmBox(t('users.rejectConfirm'), { danger: true })) { await api.post(`/api/users/${u.id}/reject`); reload(); } } }, t('users.reject')));
      return b;
    }
    if (u.id !== store.me.user.id) {
      b.push(h('button', { class: 'ui-btn sm', type: 'button', onclick: async () => {
        if (!(await confirmBox(t('users.resetConfirm', { user: u.username })))) return;
        try { const res = await api.post(`/api/users/${u.id}/reset-password`); showOtp(res.oneTimePassword, u.username); } catch (e) { toastError(e); }
      } }, icon('key'), t('users.reset')));
      b.push(u.status === 'Active'
        ? h('button', { class: 'ui-btn sm ghost', type: 'button', onclick: async () => { if (await confirmBox(t('users.suspendConfirm'), { danger: true })) { await api.post(`/api/users/${u.id}/suspend`).catch(toastError); reload(); } } }, t('users.suspend'))
        : h('button', { class: 'ui-btn sm ghost', type: 'button', onclick: async () => { await api.post(`/api/users/${u.id}/activate`).catch(toastError); reload(); } }, t('users.activate')));
      if (u.totpEnabled) b.push(h('button', { class: 'ui-btn sm ghost', type: 'button', onclick: async () => { if (await confirmBox(t('users.resetTotpConfirm'))) { await api.post(`/api/users/${u.id}/reset-totp`).catch(toastError); reload(); } } }, t('users.resetTotp')));
    }
    b.push(h('button', { class: 'ui-btn sm ghost', type: 'button', onclick: () => openForm({
      title: t('f.roles') + ' · ' + u.username,
      fields: [...roleOpts.filter((k) => k !== 'admin' || store.me.user.isAdmin).map((k) => ({ key: 'r_' + k, type: 'bool', label: 'role.' + k })), { key: 'expiresAt', type: 'date', hint: 'users.expiryHint' }],
      values: { ...Object.fromEntries(u.roles.map((k) => ['r_' + k, true])), expiresAt: u.expiresAt },
      onSubmit: async (v) => {
        const keys = roleOpts.filter((k) => v['r_' + k]);
        await api.put(`/api/users/${u.id}/roles`, { roleKeys: keys });
        if (v.expiresAt !== u.expiresAt) await api.put(`/api/users/${u.id}/expiry`, { expiresAt: v.expiresAt });
        toastOk(); reload();
      },
    }) }, icon('shield'), t('f.roles')));
    return b;
  };
  const filter = query.status || '';
  const rows = d.users.filter((u) => !filter || u.status === filter);
  el.append(pageHead(t('nav.users'),
    editable ? h('button', { class: 'ui-btn primary', type: 'button', onclick: () => openForm({
      title: t('users.create'), fields: [{ key: 'personId', type: 'ref', ref: 'people', required: true, filter: (p) => !d.users.some((u) => u.person?.id === p.id) }, { key: 'username', required: true }, rolesField(), { key: 'expiresAt', type: 'date' }],
      onSubmit: async (v) => { const res = await api.post('/api/users', { ...v, roleKeys: [v.roleKeys] }); showOtp(res.oneTimePassword, v.username); },
    }) }, icon('plus'), t('users.create')) : null),
  h('p', { class: 'small muted' }, t('users.intro')));
  if (d.resetRequests.length) el.append(h('section', { class: 'ui-card section' }, h('header', null, h('h3', null, t('users.resetRequests'))), h('div', { class: 'body' },
    staticTable([{ key: 'usernameTyped', label: 'auth.username' }, { key: 'note' }, { key: 'createdAt', type: 'datetime' },
      { key: 'x', label: '', render: (x) => h('span', null,
        x.userId ? h('button', { class: 'ui-btn sm primary', type: 'button', onclick: async () => {
          if (!(await confirmBox(t('users.resetVerify', { user: x.usernameTyped })))) return;
          try { const res = await api.post(`/api/users/${x.userId}/reset-password`); showOtp(res.oneTimePassword, x.usernameTyped); } catch (e) { toastError(e); }
        } }, t('users.reset')) : h('span', { class: 'muted small' }, t('users.noSuchUser')), ' ',
        h('button', { class: 'ui-btn sm ghost', type: 'button', onclick: async () => { await api.post(`/api/users/reset-requests/${x.id}/dismiss`); reload(); } }, t('users.dismiss'))) }], d.resetRequests))));
  el.append(h('div', { class: 'ui-tabs' }, ['', 'PendingApproval', 'Active', 'Suspended', 'Expired'].map((s) =>
    h('button', { type: 'button', 'aria-selected': String(s === filter), onclick: () => (location.hash = `#/admin/users${s ? '?status=' + s : ''}`) }, s ? enumLabel('AccountStatus', s) : t('common.all'), ' ', h('span', { class: 'muted' }, s ? d.users.filter((u) => u.status === s).length : d.users.length)))),
  staticTable([
    { key: 'username', label: 'auth.username', render: (u) => h('span', null, u.username, u.isAdmin ? h('span', { class: 'ui-badge orange', style: { marginLeft: '6px' } }, 'admin') : null) },
    { key: 'person', label: 'f.name', render: (u) => (u.person ? h('a', { href: `#/people/${u.person.id}` }, `${u.person.firstNameTh} ${u.person.lastNameTh}`) : '') },
    { key: 'category', label: 'f.category', render: (u) => enumLabel('PersonCategory', u.person?.category) },
    { key: 'roles', label: 'f.roles', render: (u) => u.roles.map(roleName).join(', ') },
    { key: 'status', type: 'enum', enum: 'AccountStatus', tone: tone.status },
    { key: 'expiresAt', type: 'date' }, { key: 'lastLoginAt', label: 'f.lastLoginAt', render: (u) => (u.lastLoginAt ? relative(u.lastLoginAt) : '—') },
    { key: 'totpEnabled', label: 'users.twoFactor', type: 'bool' },
    { key: 'a', label: '', render: (u) => h('span', { class: 'nowrap' }, ...actions(u)) }], rows));
}

export async function rolesPage() {
  const el = page('nav.roles');
  if (!can('users', 'View')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const [roles, perms] = await Promise.all([r('roles').list({ take: 100 }), r('role-permissions').list({ take: 2000 })]);
  const editable = can('users', 'Edit');
  const levels = ['None', 'View', 'Create', 'Edit', 'Manage'];
  const short = { None: '–', View: 'V', Create: 'C', Edit: 'E', Manage: 'M' };
  const table = h('table', { class: 'ui-table' });
  table.append(h('thead', null, h('tr', null, h('th', null, t('f.module')), roles.rows.map((ro) => h('th', { style: { textAlign: 'center' } }, pick(ro, 'name'))))));
  const tbody = h('tbody');
  for (const mod of MODULES) {
    const tr = h('tr', null, h('td', null, h('strong', null, t('mod.' + mod)), h('div', { class: 'small muted' }, t('mod.' + mod + '.hint'))));
    for (const ro of roles.rows) {
      const p = perms.rows.find((x) => x.roleId === ro.id && x.module === mod);
      const sel = (field) => {
        const s = h('select', { class: 'ui-select', style: { minWidth: '60px', padding: '2px 4px', minHeight: '28px' }, disabled: !editable || ro.key === 'admin', 'aria-label': `${pick(ro, 'name')} ${t('mod.' + mod)} ${t('f.' + field)}`,
          title: t('f.' + field) }, levels.map((l) => h('option', { value: l }, short[l] + ' ' + enumLabel('PermAction', l))));
        s.value = p?.[field] || 'None';
        s.addEventListener('change', async () => {
          try {
            const body = { relatedAction: field === 'relatedAction' ? s.value : p?.relatedAction || 'None', allAction: field === 'allAction' ? s.value : p?.allAction || 'None' };
            const saved = p ? await r('role-permissions').update(p.id, { ...body, syncStamp: p.syncStamp }) : await r('role-permissions').create({ roleId: ro.id, module: mod, ...body });
            Object.assign(p || {}, saved);
            if (!p) perms.rows.push(saved);
            toastOk();
          } catch (e) { toastError(e); rolesPage(); }
        });
        return s;
      };
      tr.append(h('td', { style: { textAlign: 'center' } }, h('div', { class: 'small muted' }, t('role.related')), sel('relatedAction'), h('div', { class: 'small muted' }, t('role.all')), sel('allAction')));
    }
    tbody.append(tr);
  }
  table.append(tbody);
  el.append(pageHead(t('nav.roles'), editable ? h('button', { class: 'ui-btn', type: 'button', onclick: () => openForm({
    title: t('role.add'), fields: [{ key: 'key', required: true, hint: 'role.keyHint' }, { key: 'nameTh', required: true }, { key: 'nameEn', required: true }, { key: 'description', type: 'textarea' }],
    onSubmit: async (v) => { await r('roles').create(v); toastOk(); rolesPage(); },
  }) }, icon('plus'), t('role.add')) : null),
  h('p', { class: 'small muted' }, t('role.intro')), h('div', { class: 'ui-table-wrap' }, table));
}

export function authorityPage(_, query) {
  const el = page('nav.authority');
  if (!can('approvals-config', 'View')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const tab = query.tab || 'positions';
  const items = [{ key: 'positions', label: 'auth2.positions' }, { key: 'policies', label: 'auth2.policies' }, { key: 'delegations', label: 'auth2.delegations' }];
  const body = h('div');
  const editable = can('approvals-config', 'Edit');
  const draw = (k) => {
    clear(body);
    if (k === 'positions') {
      body.append(h('p', { class: 'small muted' }, t('auth2.positionsHint')),
        h('div', { class: 'ui-grid c2' },
          h('section', null, h('h3', null, t('auth2.positions')), dataGrid({ key: 'positions', columns: DEFS.positions.columns, search: false, canCreate: editable,
            onCreate: () => editRecord('positions', null, { defaults: { rank: 10, isPublic: true }, onSaved: () => draw(k) }), onEdit: (row) => editRecord('positions', row, { onSaved: () => draw(k) }) }).el),
          h('section', null, h('h3', null, t('auth2.holders')), dataGrid({ key: 'position-assignments', columns: DEFS['position-assignments'].columns, search: false, canCreate: editable,
            onCreate: () => editRecord('position-assignments', null, { onSaved: () => draw(k) }), onEdit: (row) => editRecord('position-assignments', row, { onSaved: () => draw(k) }) }).el)));
    } else if (k === 'delegations') {
      body.append(h('p', { class: 'small muted' }, t('auth2.delegationsHint')),
        dataGrid({ key: 'delegations', columns: DEFS.delegations.columns, canCreate: editable, onCreate: () => editRecord('delegations', null, { onSaved: () => draw(k) }), onEdit: (row) => editRecord('delegations', row, { onSaved: () => draw(k) }) }).el);
    } else policies(body, editable);
  };
  const bar = tabs(items, tab, (k) => { setQuery({ tab: k }); bar.querySelectorAll('button').forEach((b, i) => b.setAttribute('aria-selected', String(items[i].key === k))); draw(k); });
  el.append(pageHead(t('nav.authority')), bar, body);
  draw(tab);
}

async function policies(body, editable) {
  const body0 = body;
  const [pols, steps] = await Promise.all([r('approval-policies').list({ take: 200 }), r('approval-policy-steps').list({ take: 1000 })]);
  const stepLabel = (s) => {
    const who = s.approverKind === 'Position' ? nameOf('positions', s.approverRef) : s.approverKind === 'Person' ? nameOf('people', s.approverRef) : s.approverKind === 'ProjectRole' ? enumLabel('ProjectRole', s.approverRef) : '';
    return `${s.stepOrder}. ${enumLabel('ApproverKind', s.approverKind)}${who ? ': ' + who : ''} (${enumLabel('StepRule', s.rule)})`;
  };
  // One stored column (ApproverRef) holds a position, a person or a project role depending on
  // the approver kind, so the three pickers are folded into it before saving.
  const stepForm = (pol, st) => openForm({
    title: t('auth2.addStep'),
    fields: DEFS['approval-policy-steps'].fields,
    values: st ? { ...st, approverPerson: st.approverKind === 'Person' ? st.approverRef : null, approverRole: st.approverKind === 'ProjectRole' ? st.approverRef : null,
      approverRef: st.approverKind === 'Position' ? st.approverRef : null }
      : { stepOrder: steps.rows.filter((x) => x.policyId === pol.id).length + 1, rule: 'Any', approverKind: 'Position' },
    onSubmit: async (v) => {
      const ref = v.approverKind === 'Position' ? v.approverRef : v.approverKind === 'Person' ? v.approverPerson : v.approverKind === 'ProjectRole' ? v.approverRole : null;
      const body = { policyId: pol.id, stepOrder: v.stepOrder, approverKind: v.approverKind, approverRef: ref, rule: v.rule };
      if (st) await r('approval-policy-steps').update(st.id, { ...body, syncStamp: st.syncStamp }); else await r('approval-policy-steps').create(body);
      toastOk();
      policies(body0, editable);
    },
  });
  clear(body).append(h('p', { class: 'small muted' }, t('auth2.policiesHint')),
    editable ? h('div', { class: 'ui-toolbar' }, h('button', { class: 'ui-btn primary', type: 'button', onclick: () => editRecord('approval-policies', null, { defaults: { subjectType: 'MoneyRequest', priority: 100, isActive: true }, onSaved: () => policies(body, editable) }) }, icon('plus'), t('auth2.addPolicy'))) : null,
    ...pols.rows.map((p) => h('section', { class: 'ui-card section' },
      h('header', null, h('h3', null, p.name), p.isActive ? h('span', { class: 'ui-badge green' }, enumLabel('RecordStatus', 'Active')) : h('span', { class: 'ui-badge' }, t('auth2.inactive')),
        editable ? h('button', { class: 'ui-btn sm ghost', type: 'button', onclick: () => editRecord('approval-policies', p, { onSaved: () => policies(body, editable) }) }, icon('edit')) : null),
      h('div', { class: 'body' },
        h('p', { class: 'small muted' }, [t('appr.subject.' + p.subjectType), p.requestKind && enumLabel('MoneyRequestKind', p.requestKind),
          p.minAmount != null || p.maxAmount != null ? `${money(p.minAmount ?? 0)} – ${p.maxAmount != null ? money(p.maxAmount) : '∞'}` : null,
          p.accountScopeId && nameOf('accounts', p.accountScopeId), p.projectScopeId && nameOf('projects', p.projectScopeId), `${t('f.priority')} ${p.priority}`].filter(Boolean).join(' · ')),
        h('ol', { class: 'ui-steps' }, steps.rows.filter((s) => s.policyId === p.id).sort((a, b) => a.stepOrder - b.stepOrder).map((s) => h('li', null, h('span', { class: 'dot' }, s.stepOrder),
          h('div', null, stepLabel(s).slice(3), editable ? [' ', h('button', { class: 'ui-btn ghost sm', type: 'button', onclick: () => stepForm(p, s) }, icon('edit')),
            h('button', { class: 'ui-btn ghost sm', type: 'button', onclick: async () => { if (await confirmBox(t('common.confirmDelete'), { danger: true })) { await r('approval-policy-steps').remove(s.id); policies(body, editable); } } }, icon('trash'))] : null)))),
        editable ? h('button', { class: 'ui-btn sm', type: 'button', style: { marginTop: '10px' }, onclick: () => stepForm(p, null) }, icon('plus'), t('auth2.addStep')) : null))));
}

export function resourcesAdminPage() {
  const el = page('nav.resources');
  if (!canAny('resources', 'Edit')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const grid = dataGrid({
    key: 'resources', columns: DEFS.resources.columns, canCreate: can('resources', 'Create'), createText: t('bk.addResource'),
    onCreate: () => editRecord('resources', null, { defaults: { kind: 'Equipment', isActive: true, slotMinutes: 30, managerPersonId: store.me.person.id }, onSaved: () => grid.reload() }),
    onEdit: (row) => editRecord('resources', row, { onSaved: () => grid.reload() }),
    onRowClick: (row) => showBlackouts(row),
  });
  el.append(pageHead(t('nav.resources')), h('p', { class: 'small muted' }, t('bk.resourcesHint')), grid.el);
}

function showBlackouts(res) {
  const m = modal({ title: `${t('bk.blackouts')} · ${pick(res, 'name')}`, size: 'wide' });
  m.body.append(childGrid('resource-blackouts', 'resourceId', res.id, { canAdd: res._can.edit }));
}

export async function auditPage(_, query) {
  const el = page('nav.audit');
  if (!can('audit', 'View')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const type = h('input', { class: 'ui-input', placeholder: t('audit.entityType'), value: query.type || '', style: { maxWidth: '200px' } });
  const id = h('input', { class: 'ui-input', placeholder: t('audit.entityId'), value: query.id || '', style: { maxWidth: '220px' } });
  const box = h('div');
  const load = async () => {
    setQuery({ type: type.value, id: id.value });
    const rows = await api.get(`/api/audit?take=300${type.value ? '&entityType=' + encodeURIComponent(type.value) : ''}${id.value ? '&entityId=' + encodeURIComponent(id.value) : ''}`);
    clear(box).append(staticTable([{ key: 'at', type: 'datetime' }, { key: 'personId', type: 'ref', ref: 'people' }, { key: 'action', label: 'f.action', render: (l) => t('audit.action.' + l.action) },
      { key: 'entityType', label: 'audit.entityType' }, { key: 'entityId', label: 'audit.entityId' },
      { key: 'afterJson', label: 'audit.change', render: (l) => h('code', { class: 'small', style: { whiteSpace: 'pre-wrap', wordBreak: 'break-all' } }, (l.beforeJson ? l.beforeJson + ' → ' : '') + (l.afterJson || '').slice(0, 400)) }], rows));
  };
  el.append(pageHead(t('nav.audit')), h('p', { class: 'small muted' }, t('audit.intro')),
    h('div', { class: 'ui-toolbar' }, type, id, h('button', { class: 'ui-btn', type: 'button', onclick: () => load().catch(toastError) }, icon('search'), t('common.search'))), box);
  load().catch(toastError);
}
