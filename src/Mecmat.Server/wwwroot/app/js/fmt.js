import { getLang, t } from './i18n.js';

const TH_MONTHS = ['ม.ค.', 'ก.พ.', 'มี.ค.', 'เม.ย.', 'พ.ค.', 'มิ.ย.', 'ก.ค.', 'ส.ค.', 'ก.ย.', 'ต.ค.', 'พ.ย.', 'ธ.ค.'];
const EN_MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
export const monthNames = () => (getLang() === 'th' ? TH_MONTHS : EN_MONTHS);
export const TH_MONTHS_FULL = ['มกราคม', 'กุมภาพันธ์', 'มีนาคม', 'เมษายน', 'พฤษภาคม', 'มิถุนายน', 'กรกฎาคม', 'สิงหาคม', 'กันยายน', 'ตุลาคม', 'พฤศจิกายน', 'ธันวาคม'];
const EN_MONTHS_FULL = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
export const monthFull = (m) => (getLang() === 'th' ? TH_MONTHS_FULL : EN_MONTHS_FULL)[m];

/** Year as people in Thailand read it: พ.ศ. in Thai mode, ค.ศ. in English. Stored values are always ค.ศ. */
export const year = (y) => (y == null ? '' : getLang() === 'th' ? y + 543 : y);
export const fiscalYear = (y) => (getLang() === 'th' ? `ปีงบประมาณ ${y + 543}` : `FY${y}`);

// All times are shown in Bangkok time whatever the browser's zone, so a booking at 10:00 reads
// 10:00 for everyone in the group.
const TZ = 'Asia/Bangkok';
function parts(d) {
  const f = new Intl.DateTimeFormat('en-GB', { timeZone: TZ, year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hour12: false, weekday: 'short' });
  const o = {};
  for (const p of f.formatToParts(d)) o[p.type] = p.value;
  return { y: +o.year, m: +o.month - 1, d: +o.day, hh: o.hour === '24' ? '00' : o.hour, mm: o.minute, wd: o.weekday };
}

export function date(v) {
  if (!v) return '';
  if (typeof v === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(v)) {
    const [y, m, d] = v.split('-').map(Number);
    return `${d} ${monthNames()[m - 1]} ${year(y)}`;
  }
  const p = parts(new Date(v));
  return `${p.d} ${monthNames()[p.m]} ${year(p.y)}`;
}
export function time(v) { if (!v) return ''; const p = parts(new Date(v)); return `${p.hh}:${p.mm}`; }
export function dateTime(v) { return v ? `${date(v)} ${time(v)}` : ''; }
export function bkkParts(v) { return parts(new Date(v)); }

/** Bangkok wall-clock → ISO UTC string for the API. */
export function bkkToIso(y, m, d, hh = 0, mm = 0) {
  return new Date(Date.UTC(y, m, d, hh - 7, mm)).toISOString();
}
export function todayBkk() { const p = parts(new Date()); return { y: p.y, m: p.m, d: p.d }; }
export function isoDate(y, m, d) { return `${y}-${String(m + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`; }

/** Money arrives in satang (ADR 0004). */
export function money(satang, { sign = false } = {}) {
  if (satang == null) return '';
  const v = Number(satang) / 100;
  const s = v.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  return sign && v > 0 ? '+' + s : s;
}
export function baht(satang) { return satang == null ? '' : `${money(satang)} ${t('common.baht')}`; }
export function parseMoney(text) {
  const n = Number(String(text ?? '').replace(/[,\s฿]/g, ''));
  return Number.isFinite(n) ? Math.round(n * 100) : null;
}
export function num(v, digits = 0) { return v == null ? '' : Number(v).toLocaleString('en-US', { maximumFractionDigits: digits }); }

export function relative(v) {
  const s = (Date.now() - new Date(v).getTime()) / 1000;
  if (s < 60) return t('time.justNow');
  if (s < 3600) return t('time.minutesAgo', { n: Math.floor(s / 60) });
  if (s < 86400) return t('time.hoursAgo', { n: Math.floor(s / 3600) });
  if (s < 86400 * 7) return t('time.daysAgo', { n: Math.floor(s / 86400) });
  return date(v);
}

export function fileSize(b) {
  if (b == null) return '';
  if (b < 1024) return b + ' B';
  if (b < 1048576) return (b / 1024).toFixed(0) + ' KB';
  return (b / 1048576).toFixed(1) + ' MB';
}
