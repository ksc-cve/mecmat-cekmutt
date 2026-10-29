import { h, clear } from '../dom.js';
import { icon } from '../icons.js';
import { t, enumLabel } from '../i18n.js';
import { r, api } from '../api.js';
import { store, can, canAny, nameOf, personName } from '../store.js';
import { DEFS, tone } from '../defs.js';
import { dataGrid, staticTable } from '../ui/grid.js';
import { toastError, toastOk, empty, promptBox, modal, busy } from '../ui/feedback.js';
import { openForm } from '../ui/form.js';
import { page, pageHead } from '../shell.js';
import { editRecord, childGrid, detailList, historyButton, backLink, afterSave } from './common.js';
import { year } from '../fmt.js';

export function publicationsPage(_, query) {
  const el = page('nav.publications');
  if (!canAny('publications', 'View')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const typeSel = h('select', { class: 'ui-select', style: { maxWidth: '240px' }, 'aria-label': t('f.type'), onchange: () => (location.hash = `#/publications?type=${typeSel.value}`) },
    h('option', { value: '' }, t('common.all')), ...['JournalArticle', 'ConferencePaper', 'BookChapter', 'Book', 'Textbook', 'AcademicArticle', 'TeachingMaterial', 'ResearchReport', 'SocialServiceWork', 'Patent', 'PettyPatent', 'Software', 'IndustryWork', 'CreativeWork', 'Thesis', 'Other']
      .map((v) => h('option', { value: v }, enumLabel('PublicationType', v))));
  typeSel.value = query.type || '';
  const grid = dataGrid({
    key: 'publications', params: { type: query.type || null }, toolbar: typeSel,
    columns: DEFS.publications.columns,
    canCreate: canAny('publications', 'Create'), createText: t('pub.add'),
    onCreate: () => newPublication(),
    onRowClick: (row) => (location.hash = `#/publications/${row.id}`),
  });
  el.append(pageHead(t('nav.publications'),
    canAny('publications', 'Create') ? h('button', { class: 'ui-btn', type: 'button', onclick: importDoi }, icon('link'), t('pub.importDoi')) : null), grid.el);
}

async function newPublication(values = {}, authors = []) {
  const saved = await editRecord('publications', null, { defaults: { type: 'JournalArticle', stage: 'Published', venueKind: 'Journal', isPublic: true, ...values } });
  if (!saved) return;
  // Authors from Crossref become rows; one that matches a member by name is linked to their profile.
  let order = 1;
  let mineAdded = false;
  for (const a of authors) {
    const match = store.lookup.people.find((p) => matchName(p, a.name));
    if (match?.id === store.me.person.id) mineAdded = true;
    await r('publication-authors').create({ publicationId: saved.id, order: order, personId: match?.id || null, externalName: match ? null : a.name, externalAffiliation: a.affiliation, isFirstAuthor: order === 1 });
    order++;
  }
  // Whoever records a publication without being on its author list would lose sight of it.
  if (!mineAdded && !authors.length) await r('publication-authors').create({ publicationId: saved.id, order: 1, personId: store.me.person.id, isFirstAuthor: true });
  location.hash = `#/publications/${saved.id}`;
}

function matchName(p, name) {
  const n = name.toLowerCase().replace(/\./g, '').trim();
  const last = (p.lastNameEn || '').toLowerCase();
  const first = (p.firstNameEn || '').toLowerCase();
  if (!last) return false;
  return n.endsWith(last) && (first && (n.startsWith(first) || n.startsWith(first[0] + ' ') || n.startsWith(first[0])));
}

async function importDoi() {
  const doi = await promptBox(t('pub.doiPrompt'), { title: t('pub.importDoi'), multiline: false });
  if (!doi) return;
  try {
    const d = await api.get('/api/doi?doi=' + encodeURIComponent(doi));
    const exists = await r('publications').list({ doi: d.doi });
    if (exists.total) { toastError(new Error(t('pub.doiExists'))); location.hash = `#/publications/${exists.rows[0].id}`; return; }
    await newPublication({
      type: d.type, titleEn: d.titleEn, venueName: d.venueName, volume: [d.volume, d.issue ? `(${d.issue})` : ''].join(''), issue: d.issue,
      pages: d.pages, articleNumber: d.articleNumber, year: d.year, publisher: d.publisher, issn: d.issn, isbn: d.isbn, doi: d.doi, url: d.url,
      citationCount: d.citationCount, venueKind: d.type === 'ConferencePaper' ? 'Conference' : 'Journal',
    }, d.authors);
  } catch (e) { toastError(e); }
}

export async function publicationPage({ id }) {
  const el = page('nav.publications');
  let p;
  try { p = await r('publications').get(id); } catch (e) { toastError(e); el.append(empty(t('errors.notFound'))); return; }
  const canEdit = p._can.edit;
  el.append(h('div', { class: 'detail-head' }, h('div', { class: 'grow' },
    backLink('#/publications'),
    h('h1', null, p.titleEn || p.titleTh),
    p.titleTh && p.titleEn ? h('div', { class: 'muted' }, p.titleTh) : null,
    h('div', { class: 'ui-toolbar', style: { marginTop: '8px' } },
      h('span', { class: 'ui-badge navy' }, enumLabel('PublicationType', p.type)),
      h('span', { class: `ui-badge ${tone.status(p.stage)}` }, enumLabel('PublicationStage', p.stage)),
      p.year ? h('span', { class: 'ui-badge' }, year(p.year)) : null,
      p.doi ? h('a', { class: 'ui-btn sm ghost', href: `https://doi.org/${p.doi}`, target: '_blank', rel: 'noopener' }, icon('link'), p.doi) : null)),
  h('div', { class: 'ui-toolbar' },
    canEdit ? h('button', { class: 'ui-btn primary', type: 'button', onclick: () => editRecord('publications', p, { onSaved: () => publicationPage({ id }) }) }, icon('edit'), t('common.edit')) : null,
    historyButton('Publication', p.id))));

  const authorsBox = h('section', { class: 'section' }, h('h3', null, t('pub.authors')),
    h('p', { class: 'small muted' }, t('pub.kpoHint')),
    childGrid('publication-authors', 'publicationId', p.id, {
      canAdd: canEdit,
      columns: [{ key: 'order', num: true }, { key: 'personId', label: 'f.name', render: (a) => (a.personId ? h('a', { href: `#/people/${a.personId}` }, nameOf('people', a.personId)) : a.externalName) },
        { key: 'externalAffiliation' },
        { key: 'roles', label: 'pub.kpoRole', render: (a) => [a.isFirstAuthor && t('f.isFirstAuthor'), a.isCorresponding && t('f.isCorresponding'), a.isEssentialContributor && t('f.isEssentialContributor')].filter(Boolean).join(', ') },
        { key: 'certifiedAt', type: 'date' }],
      defaults: { order: 99 },
    }));
  el.append(h('div', { class: 'ui-grid c2' },
    h('section', { class: 'ui-card' }, h('header', null, h('h3', null, t('common.details'))), h('div', { class: 'body' }, detailList(p, DEFS.publications.fields.filter((f) => !['titleEn', 'titleTh', 'type', 'stage', 'abstractEn', 'abstractTh', 'apaCitation'].includes(f.key))))),
    h('div', null,
      p.apaCitation ? h('section', { class: 'ui-card section' }, h('header', null, h('h3', null, t('f.apaCitation'))), h('div', { class: 'body small' }, p.apaCitation)) : null,
      p.abstractEn || p.abstractTh ? h('section', { class: 'ui-card section' }, h('header', null, h('h3', null, t('pub.abstract'))), h('div', { class: 'body small' }, p.abstractEn || p.abstractTh)) : null)),
  authorsBox,
  childGrid('publication-indexing', 'publicationId', p.id, { canAdd: canEdit, title: 'pub.indexing' }),
  childGrid('publication-usage', 'publicationId', p.id, { canAdd: canEdit, title: 'pub.usage' }),
  childGrid('publication-projects', 'publicationId', p.id, { canAdd: canEdit, title: 'pub.projects',
    columns: [{ key: 'projectId', type: 'ref', ref: 'projects' }], fields: [{ key: 'projectId', type: 'ref', ref: 'projects', required: true }],
    onRowClick: (x) => (location.hash = `#/projects/${x.projectId}`) }));
}
