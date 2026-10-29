import th from './i18n/th.js';
import en from './i18n/en.js';

// Keys are the source; both dictionaries must be complete (English left in Thai mode is a defect).
// tools/uicheck/i18n.mjs compares the two key sets.
const dicts = { th, en };
let lang = 'th';

export function setLang(l) { lang = l === 'en' ? 'en' : 'th'; document.documentElement.lang = lang; }
export function getLang() { return lang; }

export function t(key, params) {
  if (key == null) return '';
  let s = dicts[lang][key] ?? dicts.th[key] ?? dicts.en[key];
  if (s === undefined) {
    if (location.hostname === '127.0.0.1' || location.hostname === 'localhost') console.warn('[i18n] missing', key);
    s = key;
  }
  if (params) s = s.replace(/\{(\w+)\}/g, (_, k) => (params[k] ?? ''));
  return s;
}

/** Label for an enum value sent by the server, e.g. en('ProjectStatus','Active'). */
export function enumLabel(type, value) { return value == null ? '' : t(`enum.${type}.${value}`); }

/** Bilingual record fields: pick the current language, fall back to the other. */
export function pick(obj, base) {
  if (!obj) return '';
  const th = obj[base + 'Th'], en = obj[base + 'En'];
  return lang === 'en' ? (en || th || '') : (th || en || '');
}
