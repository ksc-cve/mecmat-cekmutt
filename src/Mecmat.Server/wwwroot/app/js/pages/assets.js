import { h, clear } from '../dom.js';
import { icon } from '../icons.js';
import { t, enumLabel, pick } from '../i18n.js';
import { r, api } from '../api.js';
import { store, can, canAny, nameOf } from '../store.js';
import { DEFS, tone } from '../defs.js';
import { dataGrid, staticTable, tabs } from '../ui/grid.js';
import { toastError, toastOk, empty, busy } from '../ui/feedback.js';
import { page, pageHead } from '../shell.js';
import { editRecord, childGrid, detailList, historyButton, backLink } from './common.js';
import { money, baht, date, dateTime, fileSize, num, todayBkk } from '../fmt.js';
import { setQuery } from '../router.js';

const CAPITAL = 1000000; // 10,000 baht in satang — ครุภัณฑ์ threshold (Asset.CapitalThreshold)

/** Straight-line depreciation worked out on read, so it can never go stale (data-model M6). */
export function depreciation(a) {
  if (!a.cost || !a.usefulLifeYears || !a.acquiredDate) return null;
  const [y, m, d] = a.acquiredDate.split('-').map(Number);
  const now = todayBkk();
  const years = Math.max(0, (now.y - y) + (now.m - (m - 1)) / 12 + (now.d - d) / 365);
  const base = a.cost - (a.salvageValue || 0);
  const acc = Math.min(base, Math.round((base * years) / a.usefulLifeYears));
  return { perYear: Math.round(base / a.usefulLifeYears), accumulated: acc, book: a.cost - acc };
}

export function assetsPage(_, query) {
  const el = page('nav.assets');
  if (!canAny('assets', 'View')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const tab = query.tab || 'assets';
  const items = [{ key: 'assets', label: 'asset.tab.assets' }, { key: 'materials', label: 'nav.materials' }];
  const body = h('div');
  const draw = (k) => {
    clear(body);
    if (k === 'materials') return materials(body);
    const grid = dataGrid({
      key: 'assets', columns: [...DEFS.assets.columns, { key: 'class', label: 'asset.class', render: (a) => (a.cost >= CAPITAL ? h('span', { class: 'ui-badge navy' }, t('asset.capital')) : a.cost ? h('span', { class: 'ui-badge' }, t('asset.control')) : '') }],
      canCreate: canAny('assets', 'Create'), createText: t('asset.add'),
      onCreate: () => editRecord('assets', null, { defaults: { status: 'InUse', acquisitionMethod: 'Purchase' }, onSaved: (s) => (location.hash = `#/assets/${s.id}`) }),
      onRowClick: (a) => (location.hash = `#/assets/${a.id}`),
    });
    body.append(h('p', { class: 'small muted' }, t('asset.intro')), grid.el);
  };
  const bar = tabs(items, tab, (k) => { setQuery({ tab: k }); bar.querySelectorAll('button').forEach((b, i) => b.setAttribute('aria-selected', String(items[i].key === k))); draw(k); });
  el.append(pageHead(t('nav.assets')), bar, body);
  draw(tab);
}

export async function assetPage({ id }) {
  const el = page('nav.assets');
  let a;
  try { a = await r('assets').get(id); } catch (e) { toastError(e); el.append(empty(t('errors.notFound'))); return; }
  const dep = depreciation(a);
  const canEdit = a._can.edit;
  el.append(h('div', { class: 'detail-head' },
    a.photoFileId ? h('img', { src: `/api/files/${a.photoFileId}?inline=1`, alt: '', style: { width: '120px', borderRadius: '12px' } }) : null,
    h('div', { class: 'grow' }, backLink('#/assets'), h('h1', null, pick(a, 'name')), h('div', { class: 'muted' }, [a.assetCode, a.brand, a.model, a.serialNo].filter(Boolean).join(' · ')),
      h('div', { class: 'ui-toolbar', style: { marginTop: '8px' } }, h('span', { class: `ui-badge ${tone.status(a.status)}` }, enumLabel('AssetStatus', a.status)),
        a.cost >= CAPITAL ? h('span', { class: 'ui-badge navy' }, t('asset.capital')) : a.cost ? h('span', { class: 'ui-badge' }, t('asset.control')) : null)),
    h('div', { class: 'ui-toolbar' }, canEdit ? h('button', { class: 'ui-btn primary', type: 'button', onclick: () => editRecord('assets', a, { onSaved: () => assetPage({ id }) }) }, icon('edit'), t('common.edit')) : null, historyButton('Asset', id))));
  if (dep) el.append(h('div', { class: 'kv-cards' },
    kv(t('f.cost'), money(a.cost)), kv(t('asset.perYear'), money(dep.perYear)), kv(t('asset.accumulated'), money(dep.accumulated)), kv(t('asset.bookValue'), money(dep.book))));
  el.append(h('div', { class: 'ui-grid c2' },
    h('section', { class: 'ui-card' }, h('header', null, h('h3', null, t('common.details'))), h('div', { class: 'body' }, detailList(a, DEFS.assets.fields.filter((f) => !['nameTh', 'nameEn', 'photoFileId'].includes(f.key))))),
    h('div', null,
      childGrid('asset-loans', 'assetId', id, { canAdd: can('assets', 'Edit') || canEdit, title: 'asset.loans' }),
      childGrid('maintenance', 'assetId', id, { canAdd: can('assets', 'Edit') || canEdit, title: 'asset.maintenance' }))));
}
const kv = (k, v) => h('div', { class: 'ui-card' }, h('div', { class: 'k' }, k), h('div', { class: 'v money' }, v));

async function materials(body) {
  const [mats, moves] = await Promise.all([r('materials').list({ take: 1000 }), r('material-movements').list({ take: 5000 })]);
  const stock = new Map();
  for (const m of moves.rows) stock.set(m.materialId, (stock.get(m.materialId) || 0) + (m.kind === 'Out' ? -m.qty : m.qty));
  const rows = mats.rows.map((m) => ({ ...m, stock: stock.get(m.id) || 0 }));
  clear(body).append(
    h('div', { class: 'ui-toolbar' }, h('span', { class: 'grow small muted' }, t('mat.intro')),
      canAny('assets', 'Create') ? h('button', { class: 'ui-btn primary', type: 'button', onclick: () => editRecord('materials', null, { onSaved: () => materials(body) }) }, icon('plus'), t('mat.add')) : null),
    staticTable([{ key: 'code' }, { key: 'nameTh', label: 'f.name', get: (m) => pick(m, 'name') }, { key: 'category' }, { key: 'location' },
      { key: 'stock', label: 'mat.stock', num: true, render: (m) => h('span', { class: m.minStock != null && m.stock < m.minStock ? 'ui-badge maroon' : '' }, `${num(m.stock, 2)} ${m.unit}`) },
      { key: 'go', label: '', render: (m) => h('span', { class: 'nowrap' },
        canAny('assets', 'Create') ? h('button', { class: 'ui-btn sm', type: 'button', onclick: () => editRecord('material-movements', null, { defaults: { materialId: m.id, kind: 'Out', date: new Date().toISOString().slice(0, 10), personId: store.me.person.id }, onSaved: () => materials(body) }) }, t('mat.move')) : null,
        ' ', m._can?.edit ? h('button', { class: 'ui-btn ghost sm', type: 'button', 'aria-label': t('common.edit'), onclick: () => editRecord('materials', m, { onSaved: () => materials(body) }) }, icon('edit')) : null) }], rows),
    h('h3', { style: { marginTop: '20px' } }, t('mat.movements')),
    staticTable([{ key: 'date', type: 'date' }, { key: 'materialId', type: 'ref', ref: 'materials' }, ...DEFS['material-movements'].columns.slice(1)], moves.rows.slice(0, 100)));
}

export function documentsPage(_, query) {
  const el = page('nav.documents');
  if (!canAny('documents', 'View')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const folderId = query.folder || null;
  const folders = store.lookup.folders;
  const trail = [];
  let cur = folders.find((f) => f.id === folderId);
  while (cur) { trail.unshift(cur); cur = folders.find((f) => f.id === cur.parentFolderId); }
  const crumbs = h('div', { class: 'ui-toolbar small' }, h('a', { href: '#/documents' }, t('doc.root')), trail.map((f) => [' / ', h('a', { href: `#/documents?folder=${f.id}` }, f.name)]));
  const sub = folders.filter((f) => (f.parentFolderId || null) === folderId);
  el.append(pageHead(t('nav.documents'),
    canAny('documents', 'Create') ? h('button', { class: 'ui-btn', type: 'button', onclick: () => editRecord('folders', null, { defaults: { parentFolderId: folderId, visibility: 'Private' }, onSaved: () => documentsPage(_, query) }) }, icon('folder'), t('doc.newFolder')) : null,
    canAny('documents', 'Create') ? h('button', { class: 'ui-btn primary', type: 'button', onclick: () => newDocument(folderId) }, icon('upload'), t('doc.upload')) : null),
  crumbs,
  sub.length ? h('div', { class: 'ui-grid c4 section' }, sub.map((f) => h('a', { class: 'ui-card', href: `#/documents?folder=${f.id}`, style: { padding: '14px', display: 'flex', gap: '10px', alignItems: 'center' } }, icon('folder'), f.name))) : null,
  dataGrid({ key: 'documents', params: { folderId: folderId || 'null' }, search: true,
    columns: [{ key: 'title', label: 'f.title' }, { key: 'kind', type: 'enum', enum: 'DocumentKind' }, { key: 'projectId', type: 'ref', ref: 'projects' }, { key: 'ownerPersonId', type: 'ref', ref: 'people' }, { key: 'visibility', type: 'enum', enum: 'FolderVisibility' }, { key: 'modifiedAt', type: 'datetime' }],
    onRowClick: (d) => (location.hash = `#/documents/${d.id}`), canCreate: false }).el);
}

async function newDocument(folderId, projectId) {
  const doc = await editRecord('documents', null, { defaults: { folderId, projectId, kind: 'Other', visibility: 'Private' } });
  if (doc) location.hash = `#/documents/${doc.id}`;
}

export async function documentPage({ id }) {
  const el = page('nav.documents');
  let d;
  try { d = await r('documents').get(id); } catch (e) { toastError(e); el.append(empty(t('errors.notFound'))); return; }
  const versions = (await r('document-versions').list({ documentId: id })).rows;
  const input = h('input', { type: 'file', class: 'sr-only', id: 'ver-file' });
  const note = h('input', { class: 'ui-input', placeholder: t('f.changeNote'), style: { maxWidth: '320px' } });
  input.addEventListener('change', async () => {
    if (!input.files[0]) return;
    try { await api.uploadTo(`/api/files/documents/${id}/versions`, input.files[0], { changeNote: note.value || null }); toastOk(); documentPage({ id }); } catch (e) { toastError(e); }
  });
  const current = versions.find((v) => v.id === d.currentVersionId) || versions[0];
  el.append(h('div', { class: 'detail-head' }, h('div', { class: 'grow' }, backLink(d.folderId ? `#/documents?folder=${d.folderId}` : '#/documents'), h('h1', null, d.title),
    h('div', { class: 'ui-toolbar' }, h('span', { class: 'ui-badge navy' }, enumLabel('DocumentKind', d.kind)), h('span', { class: 'ui-badge' }, enumLabel('FolderVisibility', d.visibility)),
      d.projectId ? h('a', { class: 'small', href: `#/projects/${d.projectId}` }, nameOf('projects', d.projectId)) : null)),
  h('div', { class: 'ui-toolbar' },
    current ? h('a', { class: 'ui-btn primary', href: `/api/files/${current.fileBlobId}` }, icon('download'), t('doc.downloadLatest')) : null,
    d._can.edit ? h('button', { class: 'ui-btn', type: 'button', onclick: () => editRecord('documents', d, { onSaved: () => documentPage({ id }) }) }, icon('edit'), t('common.edit')) : null)),
  d._can.edit ? h('section', { class: 'ui-card section' }, h('div', { class: 'body ui-toolbar' }, note, h('label', { class: 'ui-btn', for: 'ver-file' }, icon('upload'), t('doc.newVersion')), input, h('span', { class: 'small muted' }, t('doc.versionHint')))) : null,
  h('h3', null, t('doc.versions')),
  staticTable([{ key: 'versionNo', label: 'f.versionNo', num: true }, { key: 'changeNote' }, { key: 'createdBy', label: 'f.authorPersonId', render: (v) => v.createdBy || '' }, { key: 'createdAt', type: 'datetime' },
    { key: 'dl', label: '', render: (v) => h('a', { class: 'ui-btn sm', href: `/api/files/${v.fileBlobId}` }, icon('download')) }], versions));
}
