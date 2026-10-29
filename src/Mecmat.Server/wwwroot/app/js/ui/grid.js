import { h, clear } from '../dom.js';
import { icon } from '../icons.js';
import { t, enumLabel } from '../i18n.js';
import { r } from '../api.js';
import { date, dateTime, money, year, num } from '../fmt.js';
import { nameOf } from '../store.js';
import { confirmBox, toastError, toastOk, empty } from './feedback.js';

/** Renders a value by column type; shared by grids and detail views. */
export function cell(col, row) {
  const v = col.get ? col.get(row) : row[col.key];
  if (col.render) return col.render(row, v);
  if (v === null || v === undefined || v === '') return '';
  switch (col.type) {
    case 'date': return date(v);
    case 'datetime': return dateTime(v);
    case 'money': return h('span', { class: `money ${v < 0 ? 'neg' : ''}` }, money(v));
    case 'enum': return h('span', { class: `ui-badge ${col.tone?.(v) || ''}` }, enumLabel(col.enum, v));
    case 'ref': return nameOf(col.ref, v);
    case 'bool': return v ? '✓' : '';
    case 'year': return year(v);
    case 'number': return num(v, 2);
    default: return String(v);
  }
}

/**
 * Data grid over /api/r/{key}.
 * opts: { key, columns, params, search, onRowClick, onCreate, onEdit, canCreate, rowActions(row) → nodes, pageSize, title }
 */
export function dataGrid(opts) {
  const { key, columns } = opts;
  const pageSize = opts.pageSize || 50;
  let skip = 0, total = 0, rows = [], q = '', order = null;
  const root = h('div');
  const searchBox = opts.search === false ? null : h('input', { class: 'ui-input search', type: 'search', placeholder: t('common.search'), 'aria-label': t('common.search') });
  const tableWrap = h('div', { class: 'ui-table-wrap' });
  const pager = h('div', { class: 'ui-toolbar small muted', style: { marginTop: '8px' } });
  const toolbar = h('div', { class: 'ui-toolbar' }, searchBox, h('div', { class: 'grow' }), opts.toolbar || null,
    opts.onCreate && opts.canCreate !== false ? h('button', { class: 'ui-btn primary', type: 'button', onclick: () => opts.onCreate() }, icon('plus'), opts.createText || t('common.add')) : null);
  root.append(toolbar, tableWrap, pager);

  let timer;
  searchBox?.addEventListener('input', () => { clearTimeout(timer); timer = setTimeout(() => { q = searchBox.value.trim(); skip = 0; load(); }, 250); });

  async function load() {
    try {
      const params = { ...(opts.params || {}), q, take: pageSize, skip, order };
      const res = await r(key).list(params);
      rows = opts.filter ? res.rows.filter(opts.filter) : res.rows;
      total = res.total;
      draw();
    } catch (e) { clear(tableWrap).append(empty(t('errors.loadFailed'))); toastError(e); }
  }

  function draw() {
    const head = h('tr', null, columns.map((c) => h('th', { class: c.num || c.type === 'money' ? 'num' : '' },
      c.sort === false ? t(c.label || 'f.' + c.key) : h('button', { type: 'button', onclick: () => { order = order === c.key ? '-' + c.key : c.key; load(); } },
        t(c.label || 'f.' + c.key), order === c.key ? '↑' : order === '-' + c.key ? '↓' : ''))),
      opts.rowActions || opts.onEdit || opts.allowDelete !== false ? h('th', { class: 'actions' }, h('span', { class: 'sr-only' }, t('common.actions'))) : null);
    const body = h('tbody');
    if (!rows.length) body.append(h('tr', null, h('td', { class: 'empty', colSpan: columns.length + 1 }, opts.emptyText || t('common.empty'))));
    for (const row of rows) {
      const tr = h('tr', { class: opts.onRowClick ? 'clickable' : '' }, columns.map((c) => h('td', { class: c.num || c.type === 'money' ? 'num' : c.class || '' }, cell(c, row))));
      if (opts.onRowClick) tr.addEventListener('click', (e) => { if (!e.target.closest('button, a')) opts.onRowClick(row); });
      const actions = h('td', { class: 'actions' });
      if (opts.rowActions) actions.append(...[].concat(opts.rowActions(row) || []));
      if (opts.onEdit && row._can?.edit) actions.append(h('button', { class: 'ui-btn ghost sm', type: 'button', 'aria-label': t('common.edit'), onclick: () => opts.onEdit(row) }, icon('edit')));
      if (opts.allowDelete !== false && row._can?.delete) actions.append(h('button', { class: 'ui-btn ghost sm', type: 'button', 'aria-label': t('common.delete'), onclick: async () => {
        if (!(await confirmBox(t('common.confirmDelete'), { danger: true, okText: t('common.delete') }))) return;
        try { await r(key).remove(row.id); toastOk(t('common.deleted')); load(); opts.onChange?.(); } catch (e) { toastError(e); }
      } }, icon('trash')));
      tr.append(actions);
      body.append(tr);
    }
    clear(tableWrap).append(h('table', { class: 'ui-table' }, h('thead', null, head), body));
    clear(pager);
    if (total > pageSize) {
      pager.append(t('grid.showing', { from: skip + 1, to: Math.min(skip + pageSize, total), total }),
        h('button', { class: 'ui-btn sm', type: 'button', disabled: skip === 0, onclick: () => { skip = Math.max(0, skip - pageSize); load(); } }, icon('chevL')),
        h('button', { class: 'ui-btn sm', type: 'button', disabled: skip + pageSize >= total, onclick: () => { skip += pageSize; load(); } }, icon('chevR')));
    } else if (total) pager.append(t('grid.count', { n: total }));
  }

  load();
  return { el: root, reload: load, rows: () => rows };
}

/** Simple table for rows already in hand (statements, budget, inbox …). */
export function staticTable(columns, rows, { onRowClick, emptyText } = {}) {
  const body = h('tbody');
  if (!rows.length) body.append(h('tr', null, h('td', { class: 'empty', colSpan: columns.length }, emptyText || t('common.empty'))));
  for (const row of rows) {
    const tr = h('tr', { class: onRowClick ? 'clickable' : row._class || '' }, columns.map((c) => h('td', { class: c.num || c.type === 'money' ? 'num' : c.class || '' }, cell(c, row))));
    if (row._style) Object.assign(tr.style, row._style);
    if (onRowClick) tr.addEventListener('click', (e) => { if (!e.target.closest('button, a')) onRowClick(row); });
    body.append(tr);
  }
  return h('div', { class: 'ui-table-wrap' }, h('table', { class: 'ui-table' },
    h('thead', null, h('tr', null, columns.map((c) => h('th', { class: c.num || c.type === 'money' ? 'num' : '' }, c.label === '' ? '' : t(c.label || 'f.' + c.key))))), body));
}

export function tabs(items, active, onSelect) {
  const bar = h('div', { class: 'ui-tabs', role: 'tablist' });
  for (const it of items) {
    bar.append(h('button', { type: 'button', role: 'tab', 'aria-selected': String(it.key === active), onclick: () => onSelect(it.key) }, t(it.label)));
  }
  return bar;
}
