import { h, clear } from '../dom.js';
import { icon } from '../icons.js';
import { t } from '../i18n.js';
import { r, api } from '../api.js';
import { can, canAny, loadLookup } from '../store.js';
import { DEFS } from '../defs.js';
import { dataGrid, cell } from '../ui/grid.js';
import { openForm } from '../ui/form.js';
import { toastOk, empty, modal } from '../ui/feedback.js';
import { page, pageHead } from '../shell.js';
import { dateTime } from '../fmt.js';
import { nameOf } from '../store.js';

/** Lookup lists change when people, projects or accounts change; refresh after such saves. */
const LOOKUP_KEYS = new Set(['people', 'organizations', 'projects', 'accounts', 'resources', 'positions', 'research-areas', 'folders', 'materials', 'assets']);
export async function afterSave(key) { if (LOOKUP_KEYS.has(key)) await loadLookup(); }

export function editRecord(key, row, { fields, title, onSaved, defaults = {} } = {}) {
  const def = DEFS[key] || {};
  return openForm({
    title: title || (row ? t('common.edit') : t('common.add')),
    fields: fields || def.fields,
    values: row || defaults,
    size: (fields || def.fields).length > 12 ? 'wide' : '',
    onSubmit: async (values) => {
      const body = { ...defaults, ...values };
      const saved = row ? await r(key).update(row.id, { ...body, syncStamp: row.syncStamp }) : await r(key).create(body);
      await afterSave(key);
      toastOk();
      onSaved?.(saved);
      return saved;
    },
  });
}

/** Standard list page for a collection: search, create, edit, delete, row click. */
export function crudPage({ titleKey, key, module, columns, params, onRowClick, createText, defaults, head, intro }) {
  const el = page(titleKey);
  if (!canAny(module, 'View')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const def = DEFS[key];
  const grid = dataGrid({
    key, columns: columns || def.columns, params,
    canCreate: canAny(module, 'Create'),
    onCreate: () => editRecord(key, null, { defaults, onSaved: (s) => (onRowClick ? onRowClick(s) : grid.reload()) }),
    createText,
    onEdit: onRowClick ? null : (row) => editRecord(key, row, { onSaved: () => grid.reload() }),
    onRowClick,
  });
  el.append(pageHead(t(titleKey), ...(head ? [].concat(head) : [])), intro ? h('p', { class: 'muted small' }, intro) : null, grid.el);
  return grid;
}

/** Grid of child rows under a parent record (education under a person, members under a project …). */
export function childGrid(key, parentField, parentId, { columns, fields, canAdd = true, defaults = {}, onChange, rowActions, onRowClick, title, params = {} } = {}) {
  const def = DEFS[key] || {};
  const grid = dataGrid({
    key, columns: columns || def.columns, params: { [parentField]: parentId, ...params }, search: false, pageSize: 200,
    canCreate: canAdd, createText: t('common.add'),
    onCreate: () => editRecord(key, null, { fields, defaults: { [parentField]: parentId, ...defaults }, onSaved: () => { grid.reload(); onChange?.(); } }),
    onEdit: onRowClick ? null : (row) => editRecord(key, row, { fields, onSaved: () => { grid.reload(); onChange?.(); } }),
    onChange, rowActions, onRowClick,
  });
  return title ? h('section', { class: 'section' }, h('h3', null, t(title)), grid.el) : grid.el;
}

/** Read-only definition list from field descriptors. */
export function detailList(row, fields) {
  const dl = h('dl', { class: 'ui-dl' });
  for (const f of fields) {
    if (f.type === 'section' || f.type === 'html' || f.type === 'hours') continue;
    const v = row[f.key];
    if (v === null || v === undefined || v === '' || v === false) continue;
    const type = { enum: 'enum', ref: 'ref', date: 'date', datetime: 'datetime', money: 'money', bool: 'bool', year: 'year' }[f.type];
    dl.append(h('dt', null, t(f.label || 'f.' + f.key)), h('dd', null, cell({ key: f.key, type, enum: f.enum, ref: f.ref }, row)));
  }
  return dl;
}

export function historyButton(entityType, id) {
  return h('button', { class: 'ui-btn sm ghost', type: 'button', onclick: async () => {
    const rows = await api.get(`/api/audit?entityType=${entityType}&entityId=${encodeURIComponent(id)}`);
    const m = modal({ title: t('common.history'), size: 'wide' });
    if (!rows.length) { m.body.append(empty()); return; }
    for (const l of rows) {
      let after = {}, before = {};
      try { after = l.afterJson ? JSON.parse(l.afterJson) : {}; before = l.beforeJson ? JSON.parse(l.beforeJson) : {}; } catch { /* ignore */ }
      const changes = Object.keys(after).filter((k) => !['Id', 'CreatedAt', 'CreatedBy', 'RecordStatus'].includes(k) || l.action !== 'Create');
      m.body.append(h('div', { class: 'ui-card', style: { padding: '10px 14px', marginBottom: '8px' } },
        h('div', { class: 'small' }, h('strong', null, t('audit.action.' + l.action)), ' · ', dateTime(l.at), ' · ', l.personId ? nameOf('people', l.personId) : t('audit.system')),
        l.action === 'Update' ? h('ul', { class: 'small', style: { margin: '6px 0 0', paddingLeft: '18px' } },
          changes.map((k) => h('li', null, h('span', { class: 'muted' }, t('f.' + k[0].toLowerCase() + k.slice(1)) + ': '), String(before[k] ?? '—'), ' → ', String(after[k] ?? '—')))) : null));
    }
  } }, icon('history'), t('common.history'));
}

export const backLink = (href, label) => h('a', { class: 'ui-btn ghost sm', href }, icon('chevL'), t(label || 'common.back'));
export { can, canAny };
