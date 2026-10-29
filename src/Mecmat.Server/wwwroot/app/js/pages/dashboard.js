import { h } from '../dom.js';
import { t, enumLabel } from '../i18n.js';
import { api } from '../api.js';
import { store, personName, nameOf } from '../store.js';
import { page, pageHead } from '../shell.js';
import { baht, date, dateTime, year, num } from '../fmt.js';
import { staticTable } from '../ui/grid.js';
import { toastError } from '../ui/feedback.js';
import { tone } from '../defs.js';

function stat(label, value, sub) {
  return h('div', { class: 'ui-card ui-stat' }, h('div', { class: 'label' }, label), h('div', { class: 'value' }, value), sub ? h('div', { class: 'sub' }, sub) : null);
}

function bars(rows, labelFn) {
  const max = Math.max(1, ...rows.map((r) => r.count));
  return h('div', { class: 'chart-bars', role: 'img', 'aria-label': rows.map((r) => `${labelFn(r)}: ${r.count}`).join(', ') },
    rows.map((r) => h('div', { class: 'b' }, h('em', null, r.count), h('span', { style: { height: `${(r.count / max) * 120}px` } }), labelFn(r))));
}

const card = (title, body, link) => h('section', { class: 'ui-card' }, h('header', null, h('h3', null, title), link || null), h('div', { class: 'body' }, body));

export async function dashboardPage() {
  const el = page('nav.home');
  let d;
  try { d = await api.get('/api/dashboard'); } catch (e) { toastError(e); return; }
  const hello = t('dash.hello', { name: personName(store.me.person, { short: true }) });
  el.append(pageHead(hello));

  const stats = h('div', { class: 'ui-grid c4 section' },
    stat(t('dash.pubs'), num(d.publications.total), t('dash.pubsThisYear', { n: d.publications.thisYear })),
    stat(t('dash.q1q2'), num(d.publications.scopusQ1Q2), 'Scopus Q1–Q2'),
    stat(t('dash.activeProjects'), num(d.projects.active), d.projects.endingSoon ? t('dash.endingSoon', { n: d.projects.endingSoon }) : ''),
    d.finance ? stat(t('dash.balance'), baht(d.finance.balance), t('dash.committed', { v: baht(d.finance.committed) })) : stat(t('dash.totalBudget'), baht(d.projects.totalBudget)));
  el.append(h('p', { class: 'small muted' }, d.groupView ? t('dash.scopeGroup') : t('dash.scopeMine')), stats);

  const my = d.my;
  const left = h('div', { class: 'ui-grid', style: { alignContent: 'start' } },
    card(t('dash.myTasks'), staticTable([
      { key: 'title', label: 'f.title' }, { key: 'dueDate', type: 'date' }, { key: 'status', type: 'enum', enum: 'TaskState', tone: tone.status }],
    my.tasks, { onRowClick: (r) => (location.hash = `#/tasks/${r.id}`), emptyText: t('dash.noTasks') }), h('a', { href: '#/tasks', class: 'small' }, t('common.all'))),
    card(t('dash.myMeetings'), staticTable([{ key: 'startAt', type: 'datetime' }, { key: 'title', label: 'f.title' }, { key: 'location' }], my.meetings,
      { onRowClick: (r) => (location.hash = `#/meetings/${r.id}`), emptyText: t('dash.noMeetings') }), h('a', { href: '#/calendar', class: 'small' }, t('nav.calendar'))),
    card(t('dash.myBookings'), staticTable([{ key: 'startAt', type: 'datetime' }, { key: 'resourceId', type: 'ref', ref: 'resources' }, { key: 'title', label: 'f.title' },
      { key: 'status', type: 'enum', enum: 'BookingStatus', tone: tone.status }], my.bookings, { emptyText: t('dash.noBookings') }), h('a', { href: '#/bookings', class: 'small' }, t('nav.bookings'))),
    card(t('dash.myRequests'), staticTable([{ key: 'kind', type: 'enum', enum: 'MoneyRequestKind' }, { key: 'purpose' }, { key: 'totalAmount', type: 'money' },
      { key: 'status', type: 'enum', enum: 'MoneyRequestStatus', tone: tone.status }], my.requests, { onRowClick: (r) => (location.hash = `#/finance/requests/${r.id}`), emptyText: t('dash.noRequests') })));

  const byYear = d.publications.byYear.slice(-8);
  const right = h('div', { class: 'ui-grid', style: { alignContent: 'start' } },
    card(t('dash.pubsByYear'), byYear.length ? bars(byYear, (r) => year(r.year)) : h('p', { class: 'muted' }, t('common.empty'))),
    card(t('dash.outputs'), staticTable([{ key: 'type', label: 'f.outputType', type: 'enum', enum: 'OutputType' }, { key: 'target', type: 'number', num: true, label: 'f.target' }, { key: 'actual', type: 'number', num: true, label: 'f.actual' }], d.outputs)),
    d.people ? card(t('dash.people'), staticTable([{ key: 'category', type: 'enum', enum: 'PersonCategory' }, { key: 'count', num: true, label: 'common.total' }], d.people)) : null,
    d.finance ? card(t('dash.finance'), staticTable([{ key: 'code' }, { key: 'nameTh', label: 'f.name' }, { key: 'balance', type: 'money' }, { key: 'committed', type: 'money' }, { key: 'available', type: 'money' }],
      d.finance.accounts, { onRowClick: () => (location.hash = '#/finance') })) : null);
  el.append(h('div', { class: 'ui-grid c2' }, left, right));
}
