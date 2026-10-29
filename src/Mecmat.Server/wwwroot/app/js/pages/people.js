import { h, clear, setHtml } from '../dom.js';
import { icon } from '../icons.js';
import { t, enumLabel, pick } from '../i18n.js';
import { r, api } from '../api.js';
import { store, can, canAny, personName, nameOf } from '../store.js';
import { DEFS, tone } from '../defs.js';
import { dataGrid, staticTable, tabs } from '../ui/grid.js';
import { toastError, toastOk, empty, modal } from '../ui/feedback.js';
import { page, pageHead, initials } from '../shell.js';
import { setQuery } from '../router.js';
import { editRecord, childGrid, detailList, historyButton, backLink } from './common.js';
import { year } from '../fmt.js';
import { timelineView, networkPanel } from '../ui/timeline.js';

function avatar(p, lg) {
  return h('span', { class: `ui-avatar ${lg ? 'lg' : ''}` }, p.photoFileId ? h('img', { src: `/api/files/${p.photoFileId}?inline=1`, alt: '' }) : initials(p));
}

export function peoplePage(_, query) {
  const el = page('nav.people');
  if (!canAny('people', 'View')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const kind = query.kind || 'Internal';
  const head = pageHead(t('nav.people'));
  const filters = h('div', { class: 'ui-tabs' }, ['Internal', 'External', 'Lineage'].map((k) =>
    h('button', { type: 'button', 'aria-selected': String(k === kind), onclick: () => (location.hash = `#/people?kind=${k}`) }, k === 'Lineage' ? t('tl.lineage') : enumLabel('PersonKind', k))));
  if (kind === 'Lineage') { el.append(head, filters, lineageView()); return; }
  const grid = dataGrid({
    key: 'people', params: { kind },
    columns: [
      { key: 'firstNameTh', label: 'f.name', render: (p) => h('span', { style: { display: 'inline-flex', gap: '10px', alignItems: 'center' } }, avatar(p), h('span', null, personName(p), p.nickname ? h('span', { class: 'muted small' }, ` (${p.nickname})`) : null)) },
      { key: 'category', type: 'enum', enum: 'PersonCategory' },
      { key: 'positionText' }, { key: 'workEmail' },
      { key: 'organizationId', type: 'ref', ref: 'organizations' },
    ],
    canCreate: can('people', 'Create'),
    onCreate: () => editRecord('people', null, { defaults: { kind, category: kind === 'External' ? 'Collaborator' : 'Researcher' }, onSaved: (s) => (location.hash = `#/people/${s.id}`) }),
    onRowClick: (p) => (location.hash = `#/people/${p.id}`),
    allowDelete: false,
  });
  el.append(head, filters, grid.el);
}

/** Academic family tree from PersonMentorship: roots are people nobody in the system advised. */
function lineageView() {
  const box = h('div', null, h('p', { class: 'muted' }, t('common.loading')));
  (async () => {
    const links = (await r('person-mentorships').list({ take: 2000 })).rows;
    const students = new Set(links.map((l) => l.studentPersonId));
    const roots = [...new Set(links.map((l) => l.mentorPersonId))].filter((id) => !students.has(id));
    const node = (id, via, path) => {
      const kids = links.filter((l) => l.mentorPersonId === id && !path.includes(l.studentPersonId));
      return h('li', null,
        h('a', { class: 'tl-node', href: `#/people/${id}` }, nameOf('people', id),
          via ? h('span', { class: 'muted small' }, ` · ${enumLabel('MentorRole', via.role)}${via.degree ? ' · ' + enumLabel('EducationLevel', via.degree) : ''}`) : null),
        kids.length ? h('ul', null, kids.map((l) => node(l.studentPersonId, l, [...path, l.studentPersonId]))) : null);
    };
    clear(box).append(h('p', { class: 'small muted' }, t('tl.lineageHint')),
      roots.length ? h('ul', { class: 'lineage-tree' }, roots.map((id) => node(id, null, [id]))) : empty(t('tl.empty')));
  })().catch(toastError);
  return box;
}

const TABS = [
  { key: 'career', label: 'people.tab.career' },
  { key: 'profile', label: 'people.tab.profile' },
  { key: 'education', label: 'people.tab.education' },
  { key: 'work', label: 'people.tab.work' },
  { key: 'academic', label: 'people.tab.academic' },
  { key: 'publications', label: 'nav.publications' },
  { key: 'projects', label: 'nav.projects' },
  { key: 'payout', label: 'people.tab.payout' },
  { key: 'public', label: 'people.tab.public' },
];

export async function personPage({ id }, query, { asProfile = false } = {}) {
  const el = page(asProfile ? 'nav.profile' : 'nav.people');
  let p;
  try { p = await r('people').get(id); } catch (e) { toastError(e); el.append(empty(t('errors.notFound'))); return; }
  const mine = p.id === store.me.person.id;
  const canEdit = p._can.edit;
  const tab = query.tab || 'career';

  el.append(h('div', { class: 'detail-head' },
    avatar(p, true),
    h('div', { class: 'grow' },
      asProfile ? null : backLink('#/people'),
      h('h1', null, personName(p)),
      h('div', { class: 'muted' }, [p.positionText, nameOf('organizations', p.organizationId)].filter(Boolean).join(' · ')),
      h('div', { class: 'ui-toolbar', style: { marginTop: '8px' } },
        h('span', { class: 'ui-badge navy' }, enumLabel('PersonCategory', p.category)),
        h('span', { class: 'ui-badge' }, enumLabel('PersonKind', p.kind)),
        p.publicSlug ? h('a', { class: 'ui-btn sm ghost', href: `/people/${p.publicSlug}`, target: '_blank' }, icon('globe'), t('people.publicPage')) : null)),
    h('div', { class: 'ui-toolbar' },
      canEdit ? h('button', { class: 'ui-btn primary', type: 'button', onclick: () => editRecord('people', p, { onSaved: () => personPage({ id }, query, { asProfile }) }) }, icon('edit'), t('common.edit')) : null,
      historyButton('Person', p.id))));

  const body = h('div');
  const select = (k) => { setQuery({ tab: k }); clear(body).append(renderTab(k)); bar.querySelectorAll('button').forEach((b, i) => b.setAttribute('aria-selected', String(TABS[i].key === k))); };
  const visibleTabs = TABS.filter((tb) => (tb.key === 'payout' ? mine || can('sensitive', 'View') : tb.key === 'public' ? mine : true));
  const bar = tabs(visibleTabs, tab, select);
  el.append(bar, body);
  const childAdd = canEdit;

  function renderTab(k) {
    switch (k) {
      case 'education': return childGrid('person-education', 'personId', p.id, { canAdd: childAdd });
      case 'work': return h('div', null,
        childGrid('person-employment', 'personId', p.id, { canAdd: childAdd, title: 'people.employment' }),
        childGrid('licenses', 'personId', p.id, { canAdd: childAdd, title: 'people.licenses' }));
      case 'academic': return h('div', null,
        h('p', { class: 'small muted' }, t('people.kpoHint')),
        childGrid('academic-appointments', 'personId', p.id, { canAdd: childAdd, title: 'people.appointments' }),
        childGrid('teaching-records', 'personId', p.id, { canAdd: childAdd, title: 'people.teaching' }));
      case 'publications': return personPublications(p);
      case 'projects': return personProjects(p);
      case 'career': return careerTab(p, canEdit);
      case 'payout': return payoutTab(p, mine);
      case 'public': return publicTab(p);
      default: return profileTab(p);
    }
  }
  clear(body).append(renderTab(visibleTabs.some((x) => x.key === tab) ? tab : 'career'));
}

/** LinkedIn-style: everything dated about the person, newest first, with their network beside it. */
function careerTab(p, canEdit) {
  const box = h('div', null, h('p', { class: 'muted' }, t('common.loading')));
  const draw = async () => {
    const view = await api.get(`/api/people/${p.id}/career`);
    const editItem = (i) => {
      const key = { PersonEvent: 'person-events', PersonMentorship: 'person-mentorships', PersonEducation: 'person-education', PersonEmployment: 'person-employment' }[i.sourceType];
      r(key).get(i.sourceId).then((row) => editRecord(key, row, { onSaved: draw })).catch(toastError);
    };
    clear(box).append(
      h('div', { class: 'ui-toolbar' }, h('span', { class: 'grow small muted' }, t('tl.intro')),
        canEdit ? h('button', { class: 'ui-btn', type: 'button', onclick: () => editRecord('person-events', null, { defaults: { personId: p.id, kind: 'News', isPublic: true }, onSaved: draw }) }, icon('plus'), t('tl.addEvent')) : null,
        canEdit ? h('button', { class: 'ui-btn', type: 'button', onclick: () => editRecord('person-mentorships', null, { defaults: { studentPersonId: p.id, role: 'MainAdvisor', degree: 'Doctoral', isPublic: true }, onSaved: draw }) }, icon('users'), t('tl.addMentor')) : null),
      h('div', { class: 'career-grid' },
        h('section', { class: 'ui-card' }, h('div', { class: 'body' }, timelineView(view.items, { onItem: canEdit ? editItem : null }))),
        networkPanel(view, { personHref: (x) => `#/people/${x.id}` })));
  };
  draw().catch(toastError);
  return box;
}

function profileTab(p) {
  const fields = DEFS.people.fields.filter((f) => !['photoFileId', 'bioTh', 'bioEn'].includes(f.key));
  const wrap = h('div', { class: 'ui-grid c2' },
    h('section', { class: 'ui-card' }, h('header', null, h('h3', null, t('people.tab.profile'))), h('div', { class: 'body' }, detailList(p, fields),
      p.nationalId ? h('button', { class: 'ui-btn sm', type: 'button', style: { marginTop: '10px' }, onclick: async (e) => {
        try { const v = await api.get(`/api/sensitive/people/${p.id}/nationalId`); e.target.replaceWith(h('div', { class: 'code-box' }, v.value)); } catch (err) { toastError(err); }
      } }, icon('eye'), t('people.reveal')) : null)),
    h('section', { class: 'ui-card' }, h('header', null, h('h3', null, t('f.bioTh'))), h('div', { class: 'body' }, setHtml(h('div'), pick(p, 'bio') || `<p class="muted">${t('common.empty')}</p>`))));
  return wrap;
}

function personPublications(p) {
  const box = h('div', null, h('p', { class: 'muted' }, t('common.loading')));
  (async () => {
    try {
      const authors = await r('publication-authors').list({ personId: p.id, take: 1000 });
      const ids = new Set(authors.rows.map((a) => a.publicationId));
      const pubs = await r('publications').list({ take: 1000 });
      const rows = pubs.rows.filter((x) => ids.has(x.id)).map((x) => {
        const a = authors.rows.find((y) => y.publicationId === x.id);
        return { ...x, _role: [a.isFirstAuthor && t('f.isFirstAuthor'), a.isCorresponding && t('f.isCorresponding'), a.isEssentialContributor && t('f.isEssentialContributor')].filter(Boolean).join(', ') };
      });
      clear(box).append(staticTable([...DEFS.publications.columns, { key: '_role', label: 'pub.kpoRole' }], rows, { onRowClick: (x) => (location.hash = `#/publications/${x.id}`) }));
    } catch (e) { toastError(e); }
  })();
  return box;
}

function personProjects(p) {
  const box = h('div');
  (async () => {
    try {
      const m = await r('project-members').list({ personId: p.id, take: 500 });
      clear(box).append(staticTable([{ key: 'projectId', type: 'ref', ref: 'projects' }, { key: 'role', type: 'enum', enum: 'ProjectRole' }, { key: 'fromDate', type: 'date' }, { key: 'toDate', type: 'date' }],
        m.rows, { onRowClick: (x) => (location.hash = `#/projects/${x.projectId}`) }));
    } catch (e) { toastError(e); }
  })();
  return box;
}

function payoutTab(p, mine) {
  return h('div', null, h('p', { class: 'small muted' }, t('people.payoutHint')),
    childGrid('payout-channels', 'personId', p.id, {
      canAdd: mine || can('sensitive', 'Edit'),
      rowActions: (row) => h('button', { class: 'ui-btn ghost sm', type: 'button', 'aria-label': t('people.reveal'), onclick: async (e) => {
        try {
          const field = row.kind === 'PromptPay' ? 'promptPayId' : 'accountNo';
          const v = await api.get(`/api/sensitive/payout-channels/${row.id}/${field}`);
          e.currentTarget.replaceWith(h('code', null, v.value));
        } catch (err) { toastError(err); }
      } }, icon('eye')),
    }));
}

/** Each person decides what the public site shows about them (ADR 0007). */
function publicTab(p) {
  const box = h('div');
  (async () => {
    const rows = (await r('public-visibility').list({ personId: p.id, take: 100 })).rows;
    const map = new Map(rows.map((x) => [x.fieldKey, x]));
    const list = h('div', { class: 'ui-grid c2' });
    for (const key of store.lookup.publicFieldKeys) {
      const cur = map.get(key);
      const input = h('input', { type: 'checkbox' });
      input.checked = !!cur?.isPublic;
      input.addEventListener('change', async () => {
        try {
          const existing = map.get(key);
          const saved = existing ? await r('public-visibility').update(existing.id, { isPublic: input.checked, syncStamp: existing.syncStamp })
            : await r('public-visibility').create({ personId: p.id, fieldKey: key, isPublic: input.checked });
          map.set(key, saved);
          toastOk();
        } catch (e) { input.checked = !input.checked; toastError(e); }
      });
      list.append(h('label', { class: 'ui-check ui-card', style: { padding: '10px 14px' } }, input, t('pubfield.' + key)));
    }
    clear(box).append(h('p', { class: 'small muted' }, t('people.publicHint')), list,
      h('p', { class: 'small muted', style: { marginTop: '12px' } }, t('people.neverPublic')));
  })().catch(toastError);
  return box;
}

export function profilePage(_, query) {
  return personPage({ id: store.me.person.id }, query, { asProfile: true }).then(() => {
    const el = document.getElementById('content');
    el.append(h('section', { class: 'section ui-card', style: { marginTop: '24px' } },
      h('header', null, h('h3', null, t('profile.account'))),
      h('div', { class: 'body ui-toolbar' },
        h('span', { class: 'grow small muted' }, `${t('auth.username')}: ${store.me.user.username}`, store.me.user.expiresAt ? ` · ${t('f.expiresAt')}: ${store.me.user.expiresAt}` : ''),
        h('button', { class: 'ui-btn', type: 'button', onclick: changePasswordDialog }, icon('key'), t('auth.changeButton')),
        store.me.user.totpEnabled ? h('span', { class: 'ui-badge green' }, t('profile.totpOn')) : h('a', { class: 'ui-btn', href: '#/profile/totp' }, icon('shield'), t('profile.totpSetup')))));
  });
}

function changePasswordDialog() {
  const m = modal({ title: t('auth.changeTitle'), size: 'narrow' });
  const mk = (ac) => h('input', { class: 'ui-input', type: 'password', autocomplete: ac });
  const cur = mk('current-password'), nw = mk('new-password'), cf = mk('new-password');
  m.body.append(h('div', { class: 'ui-form', style: { gridTemplateColumns: '1fr' } },
    h('div', { class: 'ui-field' }, h('label', null, t('auth.currentPassword')), cur),
    h('div', { class: 'ui-field' }, h('label', null, t('auth.newPassword')), nw),
    h('div', { class: 'ui-field' }, h('label', null, t('auth.confirmPassword')), cf),
    h('p', { class: 'small muted' }, t('auth.passwordRules'))));
  m.footer.append(h('button', { class: 'ui-btn', type: 'button', onclick: () => m.close() }, t('common.cancel')),
    h('button', { class: 'ui-btn primary', type: 'button', onclick: async () => {
      if (nw.value !== cf.value) { toastError(new Error(t('auth.passwordsDiffer'))); return; }
      try { await api.post('/api/auth/password', { current: cur.value, new: nw.value }); toastOk(t('auth.changed')); m.close(); } catch (e) { toastError(e); }
    } }, t('auth.changeButton')));
}

export function organizationsPage() {
  const el = page('nav.organizations');
  if (!canAny('organizations', 'View')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const grid = dataGrid({
    key: 'organizations', columns: DEFS.organizations.columns,
    canCreate: canAny('organizations', 'Create'),
    onCreate: () => editRecord('organizations', null, { onSaved: () => grid.reload() }),
    onEdit: (row) => editRecord('organizations', row, { onSaved: () => grid.reload() }),
  });
  el.append(pageHead(t('nav.organizations')), h('p', { class: 'small muted' }, t('org.intro')), grid.el);
}
