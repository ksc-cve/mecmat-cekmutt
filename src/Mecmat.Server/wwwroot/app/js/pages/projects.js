import { h, clear, setHtml } from '../dom.js';
import { icon } from '../icons.js';
import { t, enumLabel, pick } from '../i18n.js';
import { r, api } from '../api.js';
import { store, can, canAny, nameOf } from '../store.js';
import { DEFS, tone } from '../defs.js';
import { dataGrid, staticTable, tabs } from '../ui/grid.js';
import { toastError, toastOk, empty, confirmBox } from '../ui/feedback.js';
import { page, pageHead } from '../shell.js';
import { setQuery } from '../router.js';
import { editRecord, childGrid, detailList, historyButton, backLink, afterSave } from './common.js';
import { gantt } from '../ui/gantt.js';
import { baht, date } from '../fmt.js';

export function projectsPage(_, query) {
  const el = page('nav.projects');
  if (!canAny('projects', 'View')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const status = h('select', { class: 'ui-select', style: { maxWidth: '220px' }, 'aria-label': t('f.status'), onchange: () => (location.hash = `#/projects?status=${status.value}`) },
    h('option', { value: '' }, t('common.all')), ...['Idea', 'Drafting', 'Submitted', 'UnderReview', 'Funded', 'Active', 'Extended', 'Completed', 'Closed', 'Rejected', 'Cancelled'].map((v) => h('option', { value: v }, enumLabel('ProjectStatus', v))));
  status.value = query.status || '';
  const grid = dataGrid({
    key: 'projects', params: { status: query.status || null }, toolbar: status,
    canCreate: can('projects', 'Create'), createText: t('prj.add'),
    onCreate: () => editRecord('projects', null, { defaults: { kind: 'Project', status: 'Idea' }, onSaved: (s) => (location.hash = `#/projects/${s.id}`) }),
    onRowClick: (row) => (location.hash = `#/projects/${row.id}`),
    columns: DEFS.projects.columns,
  });
  el.append(pageHead(t('nav.projects')), grid.el);
}

const TABS = [
  { key: 'overview', label: 'prj.tab.overview' }, { key: 'team', label: 'prj.tab.team' }, { key: 'plan', label: 'prj.tab.plan' },
  { key: 'results', label: 'prj.tab.results' }, { key: 'reports', label: 'prj.tab.reports' }, { key: 'money', label: 'prj.tab.money' },
  { key: 'publications', label: 'nav.publications' }, { key: 'work', label: 'prj.tab.work' },
];

export async function projectPage({ id }, query) {
  const el = page('nav.projects');
  let p;
  try { p = await r('projects').get(id); } catch (e) { toastError(e); el.append(empty(t('errors.notFound'))); return; }
  const canEdit = p._can.edit;
  const tab = query.tab || 'overview';
  const members = (await r('project-members').list({ projectId: id, take: 500 })).rows;
  const meLead = members.some((m) => m.personId === store.me.person.id && ['PI', 'CoPI'].includes(m.role));
  const leadEdit = can('projects', 'Edit') || meLead;

  el.append(h('div', { class: 'detail-head' }, h('div', { class: 'grow' },
    backLink('#/projects'),
    h('h1', null, pick(p, 'title')),
    h('div', { class: 'muted' }, [p.code, p.externalCode, nameOf('organizations', p.funderOrganizationId)].filter(Boolean).join(' · ')),
    h('div', { class: 'ui-toolbar', style: { marginTop: '8px' } },
      h('span', { class: 'ui-badge navy' }, enumLabel('ProjectKind', p.kind)), h('span', { class: `ui-badge ${tone.status(p.status)}` }, enumLabel('ProjectStatus', p.status)),
      p.isPublic ? h('span', { class: 'ui-badge green' }, t('common.public')) : null,
      p.parentProjectId ? h('a', { class: 'small', href: `#/projects/${p.parentProjectId}` }, nameOf('projects', p.parentProjectId)) : null)),
  h('div', { class: 'ui-toolbar' },
    canEdit ? h('button', { class: 'ui-btn primary', type: 'button', onclick: () => editRecord('projects', p, { onSaved: () => projectPage({ id }, query) }) }, icon('edit'), t('common.edit')) : null,
    historyButton('Project', p.id))));

  el.append(h('div', { class: 'kv-cards' },
    kv(t('f.totalBudget'), p.totalBudget != null ? baht(p.totalBudget) : '—'),
    kv(t('prj.period'), p.startDate ? `${date(p.startDate)} – ${date(p.extendedEndDate || p.endDate)}` : '—'),
    kv('TRL', p.trlStart || p.trlTarget ? `${p.trlStart ?? '?'} → ${p.trlTarget ?? '?'} (${t('prj.now')} ${p.trlCurrent ?? '?'})` : '—'),
    kv(t('prj.tab.team'), members.length)));

  const body = h('div');
  const bar = tabs(TABS, tab, (k) => { setQuery({ tab: k }); bar.querySelectorAll('button').forEach((b, i) => b.setAttribute('aria-selected', String(TABS[i].key === k))); clear(body).append(render(k)); });
  el.append(bar, body);

  function render(k) {
    switch (k) {
      case 'team': return h('div', null,
        childGrid('project-members', 'projectId', id, { canAdd: leadEdit, title: 'prj.members', onChange: () => {} }),
        childGrid('project-partners', 'projectId', id, { canAdd: leadEdit, title: 'prj.partners' }));
      case 'plan': return planTab(id, canEdit);
      case 'results': return h('div', null,
        h('p', { class: 'small muted' }, t('prj.resultsHint')),
        childGrid('project-outputs', 'projectId', id, { canAdd: canEdit, title: 'prj.outputs' }),
        childGrid('project-outcomes', 'projectId', id, { canAdd: canEdit, title: 'prj.outcomes' }),
        childGrid('project-impacts', 'projectId', id, { canAdd: canEdit, title: 'prj.impacts' }),
        childGrid('key-results', 'projectId', id, { canAdd: leadEdit, title: 'prj.okr' }));
      case 'reports': return childGrid('progress-reports', 'projectId', id, { canAdd: canEdit });
      case 'money': return moneyTab(p, leadEdit);
      case 'publications': return projectPublications(id, canEdit);
      case 'work': return h('div', null,
        h('h3', null, t('nav.tasks')),
        dataGrid({ key: 'tasks', params: { projectId: id }, search: false, columns: [{ key: 'title', label: 'f.title' }, { key: 'dueDate', type: 'date' }, { key: 'priority', type: 'enum', enum: 'Priority', tone: tone.status }, { key: 'status', type: 'enum', enum: 'TaskState', tone: tone.status }],
          canCreate: canAny('tasks', 'Create'), onCreate: () => editRecord('tasks', null, { defaults: { projectId: id, status: 'Todo', priority: 'Normal' }, onSaved: (s) => (location.hash = `#/tasks/${s.id}`) }),
          onRowClick: (x) => (location.hash = `#/tasks/${x.id}`), allowDelete: false }).el,
        h('h3', { style: { marginTop: '20px' } }, t('nav.meetings')),
        dataGrid({ key: 'meetings', params: { projectId: id }, search: false, columns: DEFS.meetings.columns, canCreate: false, onRowClick: (x) => (location.hash = `#/meetings/${x.id}`), allowDelete: false }).el,
        h('h3', { style: { marginTop: '20px' } }, t('nav.documents')),
        dataGrid({ key: 'documents', params: { projectId: id }, search: false, columns: [{ key: 'title', label: 'f.title' }, { key: 'kind', type: 'enum', enum: 'DocumentKind' }, { key: 'modifiedAt', type: 'datetime' }], canCreate: false, onRowClick: (x) => (location.hash = `#/documents/${x.id}`), allowDelete: false }).el);
      default: return overviewTab(p);
    }
  }
  clear(body).append(render(tab));
}

const kv = (k, v) => h('div', { class: 'ui-card' }, h('div', { class: 'k' }, k), h('div', { class: 'v' }, v));

function overviewTab(p) {
  const text = (k) => (p[k] ? h('section', { class: 'section' }, h('h3', null, t('f.' + k)), setHtml(h('div'), p[k])) : null);
  return h('div', { class: 'ui-grid c2' },
    h('section', { class: 'ui-card' }, h('header', null, h('h3', null, t('common.details'))), h('div', { class: 'body' }, detailList(p, DEFS.projects.fields.filter((f) => !['titleTh', 'titleEn', 'kind', 'status'].includes(f.key))))),
    h('div', null, text('summary'), text('rationale'), text('objectives'), text('framework'), text('methodology'),
      !p.summary && !p.objectives ? h('p', { class: 'muted' }, t('prj.noContent')) : null));
}

function planTab(id, canEdit) {
  const box = h('div');
  const draw = async () => {
    const acts = (await r('activities').list({ projectId: id, take: 500 })).rows;
    clear(box).append(
      h('div', { class: 'ui-toolbar' }, h('h3', { class: 'grow', style: { margin: 0 } }, t('prj.gantt')),
        canEdit ? h('button', { class: 'ui-btn primary', type: 'button', onclick: () => editRecord('activities', null, { defaults: { projectId: id, order: acts.length + 1, progressPct: 0 }, onSaved: draw }) }, icon('plus'), t('prj.addActivity')) : null),
      gantt(acts, { onClick: canEdit ? (a) => editRecord('activities', a, { onSaved: draw }) : null }),
      h('div', { style: { height: '20px' } }),
      childGrid('milestones', 'projectId', id, { canAdd: canEdit, title: 'prj.milestones' }));
  };
  draw().catch(toastError);
  return box;
}

const NRIIS = ['Compensation', 'Operating', 'Materials', 'Equipment', 'InstitutionFee'];

function moneyTab(p, leadEdit) {
  const box = h('div');
  (async () => {
    const accounts = store.lookup.accounts.filter((a) => a.projectId === p.id);
    const canMake = can('finance', 'Create');
    const roots = accounts.filter((a) => !accounts.some((b) => b.id === a.parentAccountId));
    clear(box).append(
      h('p', { class: 'small muted' }, t('prj.moneyHint')),
      roots.length ? staticTable([{ key: 'code' }, { key: 'nameTh', label: 'f.name' }], accounts, { onRowClick: (a) => (location.hash = `#/finance/accounts/${a.id}`) })
        : h('div', { class: 'ui-card body', style: { padding: '16px' } }, h('p', null, t('prj.noAccount')),
          canMake ? h('button', { class: 'ui-btn primary', type: 'button', onclick: async (e) => {
            e.target.disabled = true;
            try {
              const code = (p.code || p.id).toUpperCase().replace(/[^A-Z0-9-]/g, '');
              const root = await r('accounts').create({ code, nameTh: p.titleTh.slice(0, 120), nameEn: p.titleEn?.slice(0, 120), kind: 'Project', projectId: p.id });
              for (const c of NRIIS)
                await r('accounts').create({ code: `${code}-${c.slice(0, 3).toUpperCase()}`, nameTh: enumLabel('BudgetCategory', c), kind: 'BudgetCategory', budgetCategory: c, parentAccountId: root.id, projectId: p.id });
              await afterSave('accounts');
              toastOk();
              location.hash = `#/finance/accounts/${root.id}`;
            } catch (err) { toastError(err); e.target.disabled = false; }
          } }, icon('plus'), t('prj.makeAccounts')) : null));
  })().catch(toastError);
  return box;
}

function projectPublications(id, canEdit) {
  const box = h('div');
  (async () => {
    const links = (await r('publication-projects').list({ projectId: id, take: 500 })).rows;
    const pubs = (await r('publications').list({ take: 1000 })).rows.filter((x) => links.some((l) => l.publicationId === x.id));
    clear(box).append(h('p', { class: 'small muted' }, t('prj.pubHint')),
      staticTable(DEFS.publications.columns, pubs, { onRowClick: (x) => (location.hash = `#/publications/${x.id}`) }));
  })().catch(toastError);
  return box;
}
