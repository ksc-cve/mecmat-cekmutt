import { h, clear } from '../dom.js';
import { icon } from '../icons.js';
import { t, enumLabel, pick } from '../i18n.js';
import { r, api } from '../api.js';
import { store, can, canAny, nameOf } from '../store.js';
import { DEFS, tone } from '../defs.js';
import { dataGrid, staticTable, tabs } from '../ui/grid.js';
import { toastError, toastOk, empty, modal } from '../ui/feedback.js';
import { page, pageHead } from '../shell.js';
import { editRecord, crudPage } from './common.js';
import { dateTime } from '../fmt.js';
import { setQuery } from '../router.js';

export function newsPage() {
  crudPage({ titleKey: 'nav.news', key: 'news', module: 'cms', createText: t('news.add'), defaults: { kind: 'News', status: 'Draft' },
    intro: can('cms', 'Edit') ? t('news.introEditor') : t('news.introAuthor') });
}

export function recruitmentsPage() {
  const el = page('nav.recruitments');
  const grid = dataGrid({
    key: 'recruitments', columns: DEFS.recruitments.columns,
    canCreate: can('projects', 'Create') || canAny('projects', 'Edit'), createText: t('rec.add'),
    onCreate: () => editRecord('recruitments', null, { defaults: { positionType: 'ResearchAssistant', status: 'Draft', isPublic: true }, onSaved: () => grid.reload() }),
    onEdit: (row) => editRecord('recruitments', row, { onSaved: () => grid.reload() }),
  });
  el.append(pageHead(t('nav.recruitments')), h('p', { class: 'small muted' }, t('rec.intro')), grid.el);
}

export function inboxPage(_, query) {
  const el = page('nav.inbox');
  if (!can('inbox', 'View')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const tab = query.tab || 'messages';
  const items = [{ key: 'messages', label: 'inbox.messages' }, { key: 'applications', label: 'inbox.applications' }];
  const body = h('div');
  const draw = (k) => {
    clear(body);
    if (k === 'applications') {
      const recs = new Map((store.lookup.projects || []).map((p) => [p.id, p]));
      void recs;
      body.append(dataGrid({ key: 'applications', columns: DEFS.applications.columns.filter((c) => c.key !== 'recruitmentId'),
        onRowClick: (a) => showApplication(a, () => draw('applications')), allowDelete: false, canCreate: false }).el);
    } else {
      body.append(dataGrid({ key: 'contact-messages', columns: DEFS['contact-messages'].columns,
        onRowClick: (m) => showMessage(m, () => draw('messages')), allowDelete: false, canCreate: false }).el);
    }
  };
  const bar = tabs(items, tab, (k) => { setQuery({ tab: k }); bar.querySelectorAll('button').forEach((b, i) => b.setAttribute('aria-selected', String(items[i].key === k))); draw(k); });
  el.append(pageHead(t('nav.inbox')), h('p', { class: 'small muted' }, t('inbox.intro')), bar, body);
  draw(tab);
}

function showMessage(m, done) {
  const d = modal({ title: m.subject });
  d.body.append(h('dl', { class: 'ui-dl' },
    h('dt', null, t('f.name')), h('dd', null, m.name), h('dt', null, t('f.email')), h('dd', null, h('a', { href: `mailto:${m.email}` }, m.email)),
    m.phone ? [h('dt', null, t('f.phone')), h('dd', null, m.phone)] : null, h('dt', null, t('f.submittedAt')), h('dd', null, dateTime(m.submittedAt))),
  h('div', { class: 'ui-card', style: { padding: '14px', margin: '12px 0', whiteSpace: 'pre-wrap' } }, m.message));
  d.footer.append(h('button', { class: 'ui-btn', type: 'button', onclick: () => d.close() }, t('common.close')),
    m._can.edit ? h('button', { class: 'ui-btn primary', type: 'button', onclick: async () => { d.close(); await editRecord('contact-messages', m, { onSaved: done }); } }, t('inbox.update')) : null);
  if (m.status === 'New' && m._can.edit) r('contact-messages').update(m.id, { status: 'Read', syncStamp: m.syncStamp }).then(done).catch(() => {});
}

function showApplication(a, done) {
  const d = modal({ title: a.fullName });
  d.body.append(h('dl', { class: 'ui-dl' },
    h('dt', null, t('f.email')), h('dd', null, h('a', { href: `mailto:${a.email}` }, a.email)), a.phone ? [h('dt', null, t('f.phone')), h('dd', null, a.phone)] : null,
    h('dt', null, t('f.submittedAt')), h('dd', null, dateTime(a.submittedAt)), h('dt', null, t('f.status')), h('dd', null, enumLabel('ApplicationStatus', a.status)),
    a.cvFileId ? [h('dt', null, t('f.cvFileId')), h('dd', null, h('a', { href: `/api/files/${a.cvFileId}`, target: '_blank' }, t('common.download')))] : null),
  a.message ? h('div', { class: 'ui-card', style: { padding: '14px', margin: '12px 0', whiteSpace: 'pre-wrap' } }, a.message) : null,
  a.reviewerNotes ? h('p', { class: 'small muted' }, a.reviewerNotes) : null);
  d.footer.append(h('button', { class: 'ui-btn', type: 'button', onclick: () => d.close() }, t('common.close')),
    a._can.edit ? h('button', { class: 'ui-btn primary', type: 'button', onclick: async () => { d.close(); await editRecord('applications', a, { onSaved: done }); } }, t('inbox.update')) : null);
}

export function sitePage(_, query) {
  const el = page('nav.siteContent');
  if (!can('cms', 'Edit')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const tab = query.tab || 'pages';
  const items = [{ key: 'pages', label: 'site.pages' }, { key: 'areas', label: 'f.researchAreas' }, { key: 'settings', label: 'site.settings' }];
  const body = h('div');
  const draw = (k) => {
    clear(body);
    const key = { pages: 'site-pages', areas: 'research-areas', settings: 'site-settings' }[k];
    const grid = dataGrid({ key, columns: DEFS[key].columns, search: false,
      canCreate: k !== 'pages' && k !== 'settings',
      onCreate: () => editRecord(key, null, { onSaved: () => grid.reload() }),
      onEdit: (row) => editRecord(key, row, { onSaved: () => grid.reload() }), allowDelete: k === 'areas' });
    body.append(h('p', { class: 'small muted' }, t('site.hint.' + k)), grid.el);
  };
  const bar = tabs(items, tab, (k) => { setQuery({ tab: k }); bar.querySelectorAll('button').forEach((b, i) => b.setAttribute('aria-selected', String(items[i].key === k))); draw(k); });
  el.append(pageHead(t('nav.siteContent'), h('a', { class: 'ui-btn', href: '/', target: '_blank' }, icon('globe'), t('nav.viewSite'))), bar, body);
  draw(tab);
}
