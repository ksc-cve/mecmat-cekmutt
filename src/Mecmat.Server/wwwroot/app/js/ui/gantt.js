import { h } from '../dom.js';
import { t } from '../i18n.js';
import { monthNames, year, todayBkk } from '../fmt.js';

/** Month-grid Gantt for project activities (the NRIIS work plan is per month). */
export function gantt(activities, { onClick } = {}) {
  const dated = activities.filter((a) => a.startDate && a.endDate);
  if (!dated.length) return h('p', { class: 'muted small' }, t('prj.ganttEmpty'));
  const ym = (s) => { const [y, m] = s.split('-').map(Number); return y * 12 + (m - 1); };
  const from = Math.min(...dated.map((a) => ym(a.startDate)));
  const to = Math.max(...dated.map((a) => ym(a.endDate)));
  const now = todayBkk();
  const nowIdx = now.y * 12 + now.m;
  const months = [];
  for (let i = from; i <= to; i++) months.push(i);
  const head = h('tr', null, h('th', { class: 'name' }, t('f.name')),
    months.map((i) => h('th', { class: 'm' }, monthNames()[i % 12], h('br'), String(year(Math.floor(i / 12))).slice(-2))));
  const rows = activities.slice().sort((a, b) => (a.order ?? 0) - (b.order ?? 0)).map((a) => {
    const s = a.startDate ? ym(a.startDate) : null, e = a.endDate ? ym(a.endDate) : null;
    return h('tr', { class: onClick ? 'clickable' : '', onclick: () => onClick?.(a) },
      h('td', { class: 'name', title: a.name }, a.name, h('span', { class: 'muted' }, ` · ${a.progressPct ?? 0}%`)),
      months.map((i) => h('td', { class: `m ${s != null && i >= s && i <= e ? 'on' : ''} ${a.progressPct >= 100 ? 'done' : ''} ${i === nowIdx ? 'today' : ''}` })));
  });
  return h('div', { class: 'gantt' }, h('table', null, h('thead', null, head), h('tbody', null, rows)));
}
