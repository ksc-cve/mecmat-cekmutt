import { h, clear } from '../dom.js';
import { icon } from '../icons.js';
import { t, enumLabel, getLang } from '../i18n.js';
import { date, year as beYear, monthNames } from '../fmt.js';
import { initials } from '../shell.js';

const ICON = {
  education: 'book', position: 'briefcase', rank: 'award', award: 'award', publication: 'file', project: 'target', supervision: 'users',
  news: 'news', visit: 'globe', talk: 'chat', collaboration: 'link', grant: 'coins', milestone: 'checkCircle', other: 'clock',
};

/** Words encoded by the server ("rank:AssocProf", "role:PI", "mentor:MainAdvisor|Doctoral") become labels here. */
function label(text) {
  if (!text) return text;
  if (text.startsWith('rank:')) return enumLabel('AcademicRank', text.slice(5));
  if (text.startsWith('role:')) return enumLabel('ProjectRole', text.slice(5));
  if (text.startsWith('mentor:')) {
    const [role, degree] = text.slice(7).split('|');
    return enumLabel('MentorRole', role) + (degree ? ' · ' + enumLabel('EducationLevel', degree) : '');
  }
  return text;
}

function when(i) {
  const fmt = (s, yearOnly) => {
    if (!s || s.startsWith('0000')) return '';
    if (yearOnly) return String(beYear(+s.slice(0, 4)));
    const [y, m, d] = s.split('-').map(Number);
    return d === 1 || d === 31 || d === 30 ? `${monthNames()[m - 1]} ${beYear(y)}` : date(s);
  };
  const a = fmt(i.date, i.yearOnly);
  if (!i.endDate) return ['position', 'project'].includes(i.kind) ? `${a} – ${t('tl.present')}` : a;
  return `${a} – ${fmt(i.endDate, i.yearOnly)}`;
}

export function timelineView(items, { onItem } = {}) {
  const kinds = [...new Set(items.map((i) => i.kind))];
  let active = new Set(kinds);
  const list = h('div');
  const filters = h('div', { class: 'tl-filters', role: 'group', 'aria-label': t('tl.filter') });
  const drawFilters = () => {
    clear(filters);
    for (const k of kinds) filters.append(h('button', { type: 'button', 'aria-pressed': String(active.has(k)), onclick: () => { active.has(k) ? active.delete(k) : active.add(k); if (!active.size) active = new Set(kinds); drawFilters(); draw(); } },
      t('tl.kind.' + k), ' ', h('span', { class: 'muted' }, items.filter((i) => i.kind === k).length)));
  };
  const draw = () => {
    clear(list);
    const shown = items.filter((i) => active.has(i.kind));
    if (!shown.length) { list.append(h('p', { class: 'muted' }, t('tl.empty'))); return; }
    let lastYear = null;
    const ul = h('ol', { class: 'tl' });
    for (const i of shown) {
      const y = i.date.startsWith('0000') ? t('tl.undated') : beYear(+i.date.slice(0, 4));
      if (y !== lastYear) { ul.append(h('li', { class: 'tl-year' }, y)); lastYear = y; }
      const en = getLang() === 'en';
      const title = label(en ? i.titleEn || i.titleTh : i.titleTh || i.titleEn);
      const sub = (en ? i.subEn || i.subTh : i.subTh || i.subEn);
      const titleNode = i.link ? h('a', { href: i.link }, title) : i.url ? h('a', { href: i.url, target: '_blank', rel: 'noopener' }, title) : title;
      ul.append(h('li', { class: `tl-item k-${i.kind}` },
        h('span', { class: 'tl-dot', 'aria-hidden': 'true' }, icon(ICON[i.kind] || 'clock')),
        h('div', { class: 'tl-body' },
          h('div', { class: 'tl-kind' }, t('tl.kind.' + i.kind)),
          h('div', { class: 'tl-title' }, titleNode),
          sub ? h('div', { class: 'tl-sub' }, sub.split('\n').map(label).join('\n')) : null,
          h('div', { class: 'tl-when' }, when(i),
            onItem && ['PersonEvent', 'PersonMentorship', 'PersonEducation', 'PersonEmployment'].includes(i.sourceType)
              ? h('button', { class: 'ui-btn ghost sm', type: 'button', 'aria-label': t('common.edit'), onclick: () => onItem(i) }, icon('edit')) : null))));
    }
    list.append(ul);
  };
  drawFilters();
  draw();
  return h('div', null, kinds.length > 1 ? filters : null, list);
}

export function networkPanel(view, { personHref }) {
  const en = getLang() === 'en';
  const person = (p, detail) => {
    const name = en ? p.nameEn : p.nameTh;
    const av = h('span', { class: 'ui-avatar' }, p.photoFileId ? h('img', { src: `/api/files/${p.photoFileId}?inline=1`, alt: '' }) : name.replace(/^(ศ\.|รศ\.|ผศ\.|ดร\.|Prof\.|Assoc\. Prof\.|Asst\. Prof\.|Dr\.)\s*/g, '').slice(0, 1));
    const inner = [av, h('span', null, h('span', { class: 'nn' }, name), detail ? h('div', { class: 'nd' }, detail) : null)];
    const href = personHref(p);
    return href ? h('a', { href }, ...inner) : h('div', { class: 'np' }, ...inner);
  };
  const mentorDetail = (p) => {
    const [deg, from, to] = (p.detail || '').split('|');
    return [enumLabel('MentorRole', p.relation), deg ? enumLabel('EducationLevel', deg) : '', from ? `${beYear(+from)}${to ? '–' + beYear(+to) : ''}` : ''].filter(Boolean).join(' · ');
  };
  const section = (title, nodes) => (nodes.length ? h('section', { class: 'ui-card section' }, h('header', null, h('h3', null, t(title))), h('div', { class: 'body net' }, nodes)) : null);
  return h('div', null,
    section('tl.mentors', view.mentors.map((p) => person(p, mentorDetail(p)))),
    section('tl.students', view.students.map((p) => person(p, mentorDetail(p)))),
    section('tl.coauthors', view.coAuthors.slice(0, 12).map((p) => person(p, t('tl.sharedPubs', { n: p.count })))),
    section('tl.orgs', view.organizations.slice(0, 12).map((o) => h('div', { class: 'np' }, icon('building'), h('span', null, h('span', { class: 'nn' }, en ? o.nameEn || o.nameTh : o.nameTh), h('div', { class: 'nd' }, [enumLabel('OrganizationKind', o.kind), o.country].filter(Boolean).join(' · ')))))));
}
