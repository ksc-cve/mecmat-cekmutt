import { h, clear, $ } from './dom.js';
import { icon } from './icons.js';
import { t, getLang } from './i18n.js';
import { api } from './api.js';
import { store, can, canAny, personName } from './store.js';
import { relative } from './fmt.js';
import { parse } from './router.js';

export const NAV = () => [
  { key: 'home', href: '#/', icon: 'home', label: 'nav.home' },
  { key: 'approvals', href: '#/approvals', icon: 'checkCircle', label: 'nav.approvals', badge: 'approvals' },
  { group: 'nav.groupWork' },
  { key: 'projects', href: '#/projects', icon: 'briefcase', label: 'nav.projects', show: () => canAny('projects', 'View') },
  { key: 'publications', href: '#/publications', icon: 'book', label: 'nav.publications', show: () => canAny('publications', 'View') },
  { key: 'people', href: '#/people', icon: 'users', label: 'nav.people', show: () => canAny('people', 'View') },
  { key: 'organizations', href: '#/organizations', icon: 'building', label: 'nav.organizations', show: () => canAny('organizations', 'View') },
  { key: 'tasks', href: '#/tasks', icon: 'kanban', label: 'nav.tasks', show: () => canAny('tasks', 'View') },
  { key: 'meetings', href: '#/meetings', icon: 'chat', label: 'nav.meetings', show: () => canAny('meetings', 'View') },
  { key: 'calendar', href: '#/calendar', icon: 'calendar', label: 'nav.calendar' },
  { key: 'documents', href: '#/documents', icon: 'folder', label: 'nav.documents', show: () => canAny('documents', 'View') },
  { key: 'bookings', href: '#/bookings', icon: 'clock', label: 'nav.bookings', show: () => canAny('bookings', 'Create') || canAny('bookings', 'View') },
  { key: 'assets', href: '#/assets', icon: 'tool', label: 'nav.assets', show: () => canAny('assets', 'View') },
  { key: 'finance', href: '#/finance', icon: 'wallet', label: 'nav.finance', show: () => canAny('finance', 'View') || canAny('finance', 'Create') },
  { group: 'nav.website', show: () => canAny('cms', 'Create') || can('inbox', 'View') || canAny('projects', 'Edit') },
  { key: 'news', href: '#/news', icon: 'news', label: 'nav.news', show: () => canAny('cms', 'Create') },
  { key: 'recruitments', href: '#/recruitments', icon: 'target', label: 'nav.recruitments', show: () => canAny('projects', 'Edit') || can('projects', 'Create') },
  { key: 'inbox', href: '#/inbox', icon: 'inbox', label: 'nav.inbox', show: () => can('inbox', 'View') },
  { key: 'site', href: '#/site', icon: 'globe', label: 'nav.siteContent', show: () => can('cms', 'Edit') },
  { group: 'nav.admin', show: () => can('users', 'View') || can('approvals-config', 'View') || can('audit', 'View') || canAny('resources', 'Edit') },
  { key: 'users', href: '#/admin/users', icon: 'key', label: 'nav.users', show: () => can('users', 'View') },
  { key: 'roles', href: '#/admin/roles', icon: 'shield', label: 'nav.roles', show: () => can('users', 'View') },
  { key: 'authority', href: '#/admin/authority', icon: 'settings', label: 'nav.authority', show: () => can('approvals-config', 'View') },
  { key: 'resources', href: '#/admin/resources', icon: 'box', label: 'nav.resources', show: () => canAny('resources', 'Edit') },
  { key: 'audit', href: '#/admin/audit', icon: 'history', label: 'nav.audit', show: () => can('audit', 'View') },
];

let shellEl, contentEl, titleEl, navEl, bellDot, panel;
const badges = { approvals: 0 };

export function mountShell(root, { onLang, onTheme, onSignOut }) {
  navEl = h('nav', { class: 'nav', 'aria-label': t('nav.menu') });
  titleEl = h('div', { class: 'title' });
  contentEl = h('main', { class: 'content', id: 'content', tabIndex: -1 });
  bellDot = h('span', { class: 'dot hidden' });
  const me = store.me;
  const avatar = h('span', { class: 'ui-avatar' }, me.person.photoFileId ? h('img', { src: `/api/files/${me.person.photoFileId}?inline=1`, alt: '' }) : initials(me.person));
  const bell = h('button', { class: 'ui-btn ghost icon bell', type: 'button', 'aria-label': t('nav.notifications'), onclick: togglePanel }, icon('bell'), bellDot);
  shellEl = h('div', { class: 'shell' },
    h('aside', { class: 'sidebar' },
      h('a', { class: 'brand', href: '#/' }, h('span', { class: 'mark' }, 'M'), h('span', null, h('div', { class: 'name' }, 'MECMAT'), h('div', { class: 'sub' }, t('app.dept')))),
      navEl),
    h('div', { class: 'main' },
      h('header', { class: 'topbar' },
        h('button', { class: 'ui-btn ghost icon menu-btn', type: 'button', 'aria-label': t('nav.menu'), onclick: () => shellEl.classList.toggle('nav-open') }, icon('menu')),
        titleEl,
        h('a', { class: 'ui-btn ghost sm', href: '/', target: '_blank' }, icon('globe'), h('span', { class: 'hide-sm' }, t('nav.viewSite'))),
        h('button', { class: 'ui-btn ghost sm', type: 'button', onclick: onLang }, t('nav.language')),
        h('button', { class: 'ui-btn ghost icon', type: 'button', 'aria-label': t('nav.theme'), onclick: onTheme }, icon(document.documentElement.dataset.theme === 'dark' ? 'sun' : 'moon')),
        bell,
        h('a', { class: 'user-chip', href: '#/profile', title: t('nav.profile') }, avatar, h('span', { class: 'small hide-sm' }, personName(me.person, { short: true }))),
        h('button', { class: 'ui-btn ghost icon', type: 'button', 'aria-label': t('nav.signOut'), title: t('nav.signOut'), onclick: onSignOut }, icon('logout'))),
      contentEl));
  clear(root).append(h('div', { class: 'splash' }), shellEl);
  drawNav();
  pollNotifications();
  setInterval(pollNotifications, 30000);
  document.addEventListener('click', (e) => { if (panel && !panel.contains(e.target) && !bell.contains(e.target)) closePanel(); });
}

export function initials(p) {
  const th = getLang() === 'th';
  const a = (th ? p.firstNameTh : p.firstNameEn || p.firstNameTh) || '?';
  const b = (th ? p.lastNameTh : p.lastNameEn || p.lastNameTh) || '';
  return (a[0] + (b[0] || '')).toUpperCase();
}

export function drawNav() {
  if (!navEl) return;
  const path = parse().path;
  clear(navEl);
  for (const item of NAV()) {
    if (item.show && !item.show()) continue;
    if (item.group) { navEl.append(h('div', { class: 'group' }, t(item.group))); continue; }
    const active = item.href === '#/' ? path === '/' : path.startsWith(item.href.slice(1));
    const count = item.badge ? badges[item.badge] : 0;
    navEl.append(h('a', { href: item.href, class: active ? 'active' : '', 'aria-current': active ? 'page' : null, onclick: () => shellEl.classList.remove('nav-open') },
      icon(item.icon), t(item.label), count ? h('span', { class: 'count' }, count) : null));
  }
}

/** Every page renders into this. Returns the content element, cleared. */
export function page(titleKey, { title } = {}) {
  const text = title || t(titleKey);
  titleEl.textContent = text;
  document.title = `${text} · MECMAT`;
  drawNav();
  clear(contentEl);
  contentEl.focus({ preventScroll: true });
  window.scrollTo(0, 0);
  return contentEl;
}

export function pageHead(title, ...actions) {
  return h('div', { class: 'page-head' }, h('h1', null, title), ...actions);
}

let lastNotes = { unread: 0, rows: [] };
async function pollNotifications() {
  try {
    lastNotes = await api.get('/api/notifications');
    bellDot.textContent = lastNotes.unread > 99 ? '99+' : lastNotes.unread;
    bellDot.classList.toggle('hidden', !lastNotes.unread);
    const inbox = await api.get('/api/approvals/inbox');
    if (badges.approvals !== inbox.waiting.length) { badges.approvals = inbox.waiting.length; drawNav(); }
    if (panel) drawPanel();
  } catch { /* offline or signed out: the next call reports it */ }
}

function togglePanel() { if (panel) closePanel(); else { panel = h('div', { class: 'panel', role: 'dialog', 'aria-label': t('nav.notifications') }); document.body.append(panel); drawPanel(); } }
function closePanel() { panel?.remove(); panel = null; }

function drawPanel() {
  clear(panel);
  panel.append(h('div', { class: 'ui-toolbar', style: { padding: '10px 14px', margin: 0, borderBottom: '1px solid var(--border)' } },
    h('strong', { class: 'grow' }, t('nav.notifications')),
    lastNotes.unread ? h('button', { class: 'ui-btn sm', type: 'button', onclick: async () => { await api.post('/api/notifications/read-all'); pollNotifications(); } }, t('notify.markAll')) : null));
  if (!lastNotes.rows.length) { panel.append(h('div', { class: 'ui-empty' }, t('notify.empty'))); return; }
  for (const n of lastNotes.rows) {
    let params = {};
    try { params = n.body ? JSON.parse(n.body) : {}; } catch { /* plain text body */ }
    panel.append(h('a', { class: `item ${n.readAt ? '' : 'unread'}`, href: n.link || '#/', onclick: async () => { if (!n.readAt) await api.post(`/api/notifications/${n.id}/read`); closePanel(); pollNotifications(); } },
      t(n.title, params), n.count > 1 ? h('span', { class: 'muted' }, ' · ' + t('notify.andMore', { n: n.count - 1 })) : null,
      h('time', null, relative(n.modifiedAt))));
  }
}

export const refreshBadges = pollNotifications;
