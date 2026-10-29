import { api } from './api.js';
import { getLang } from './i18n.js';

// Session-wide state: who I am, my grants, and the name lists every picker and label needs.
export const store = { me: null, lookup: null };

const ORDER = ['None', 'View', 'Create', 'Edit', 'Manage'];

/** Mirrors the server's Grant.Allows so the UI hides what the server would refuse. */
export function can(module, action, related = false) {
  const me = store.me;
  if (!me) return false;
  if (me.user.isAdmin) return true;
  const g = me.grants[module];
  if (!g) return false;
  const need = ORDER.indexOf(action);
  const lvl = related ? Math.max(ORDER.indexOf(g.related), ORDER.indexOf(g.all)) : ORDER.indexOf(g.all);
  return lvl >= need;
}
export const canAny = (module, action) => can(module, action, true);

export async function loadLookup() {
  store.lookup = await api.get('/api/lookup');
  const idx = {};
  for (const [k, list] of Object.entries(store.lookup)) if (Array.isArray(list)) idx[k] = new Map(list.map((x) => [x.id, x]));
  store.index = idx;
  return store.lookup;
}

export function personName(p, { short = false } = {}) {
  if (!p) return '';
  const th = getLang() === 'th';
  const first = th ? p.firstNameTh || p.firstNameEn : p.firstNameEn || p.firstNameTh;
  const last = th ? p.lastNameTh || p.lastNameEn : p.lastNameEn || p.lastNameTh;
  if (short) return `${first} ${last}`.trim();
  const rank = { AsstProf: th ? 'ผศ.' : 'Asst. Prof.', AssocProf: th ? 'รศ.' : 'Assoc. Prof.', Prof: th ? 'ศ.' : 'Prof.' }[p.academicRank] || '';
  const dr = p.hasDoctorate ? (th ? 'ดร.' : 'Dr. ') : '';
  const title = rank || dr ? `${rank}${rank && th ? '' : rank ? ' ' : ''}${dr}` : (th ? p.titleTh || '' : p.titleEn ? p.titleEn + ' ' : '');
  return `${title}${first} ${last}`.trim();
}

export const nameOf = (kind, id) => {
  const x = store.index?.[kind]?.get(id);
  if (!x) return id || '';
  if (kind === 'people') return personName(x);
  if (kind === 'accounts') return `${x.code} · ${getLang() === 'en' ? x.nameEn || x.nameTh : x.nameTh}`;
  if (kind === 'projects') return (x.code ? x.code + ' · ' : '') + (getLang() === 'en' ? x.titleEn || x.titleTh : x.titleTh);
  if (kind === 'assets') return (x.assetCode ? x.assetCode + ' · ' : '') + (getLang() === 'en' ? x.nameEn || x.nameTh : x.nameTh);
  return getLang() === 'en' ? x.nameEn || x.nameTh || x.name : x.nameTh || x.nameEn || x.name;
};
