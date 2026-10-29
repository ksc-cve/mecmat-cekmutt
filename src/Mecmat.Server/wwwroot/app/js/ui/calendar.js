import { h, clear } from '../dom.js';
import { icon } from '../icons.js';
import { t } from '../i18n.js';
import { monthFull, year, bkkParts, bkkToIso, todayBkk, time, date } from '../fmt.js';

const DAY = 86400000;
/** Monday of the week containing the Bangkok date (y, m, d). */
function weekStart(y, m, d) {
  const dt = new Date(Date.UTC(y, m, d));
  const dow = (dt.getUTCDay() + 6) % 7;
  return new Date(dt.getTime() - dow * DAY);
}
const ymd = (dt) => ({ y: dt.getUTCFullYear(), m: dt.getUTCMonth(), d: dt.getUTCDate() });

/**
 * Calendar with month / week / day views. Times are Bangkok wall clock.
 * opts: { view, anchor:{y,m,d}, load(fromIso,toIso) → events[{start,end,date,title,kind,cls,onClick}], hours:[startHour,endHour],
 *         onSlot(startIso,endIso) (week/day drag-select), closed(dayIndex0Mon) → bool, legend }
 */
export function calendar(opts) {
  let view = opts.view || 'week';
  let anchor = opts.anchor || todayBkk();
  const [h0, h1] = opts.hours || [7, 20];
  const root = h('div', { class: 'cal' });
  const label = h('div', { class: 'label' });
  const grid = h('div');
  const viewBtn = (v) => h('button', { class: `ui-btn sm ${view === v ? 'navy' : ''}`, type: 'button', onclick: () => { view = v; draw(); } }, t('cal.' + v));
  const head = h('div', { class: 'cal-head' });
  root.append(head, grid, opts.legend ? h('div', { class: 'legend' }, opts.legend) : null);

  const shift = (n) => {
    if (view === 'month') { const m = anchor.m + n; anchor = { y: anchor.y + Math.floor(m / 12), m: ((m % 12) + 12) % 12, d: 1 }; }
    else { const dt = new Date(Date.UTC(anchor.y, anchor.m, anchor.d) + n * (view === 'week' ? 7 : 1) * DAY); anchor = ymd(dt); }
    draw();
  };

  async function draw() {
    clear(head).append(
      h('button', { class: 'ui-btn sm icon', type: 'button', 'aria-label': t('cal.prev'), onclick: () => shift(-1) }, icon('chevL')),
      h('button', { class: 'ui-btn sm', type: 'button', onclick: () => { anchor = todayBkk(); draw(); } }, t('common.today')),
      h('button', { class: 'ui-btn sm icon', type: 'button', 'aria-label': t('cal.next'), onclick: () => shift(1) }, icon('chevR')),
      label, viewBtn('day'), viewBtn('week'), viewBtn('month'), opts.toolbar || null);
    let from, days;
    if (view === 'month') {
      const first = new Date(Date.UTC(anchor.y, anchor.m, 1));
      from = weekStart(anchor.y, anchor.m, 1);
      const last = new Date(Date.UTC(anchor.y, anchor.m + 1, 0));
      days = Math.ceil(((last - from) / DAY + 1) / 7) * 7;
      label.textContent = `${monthFull(anchor.m)} ${year(anchor.y)}`;
      void first;
    } else if (view === 'week') {
      from = weekStart(anchor.y, anchor.m, anchor.d); days = 7;
      const end = new Date(from.getTime() + 6 * DAY);
      label.textContent = `${date(isoOf(from))} – ${date(isoOf(end))}`;
    } else { from = new Date(Date.UTC(anchor.y, anchor.m, anchor.d)); days = 1; label.textContent = date(isoOf(from)); }
    const fromIso = bkkToIso(from.getUTCFullYear(), from.getUTCMonth(), from.getUTCDate());
    const toDt = new Date(from.getTime() + days * DAY);
    const toIso = bkkToIso(toDt.getUTCFullYear(), toDt.getUTCMonth(), toDt.getUTCDate());
    clear(grid).append(h('div', { class: 'ui-empty' }, t('common.loading')));
    let events = [];
    try { events = await opts.load(fromIso, toIso); } catch { events = []; }
    if (view === 'month') drawMonth(from, days, events); else drawWeek(from, days, events);
  }

  const isoOf = (dt) => `${dt.getUTCFullYear()}-${String(dt.getUTCMonth() + 1).padStart(2, '0')}-${String(dt.getUTCDate()).padStart(2, '0')}`;
  const dayKey = (e) => (e.date ? e.date : (() => { const p = bkkParts(e.start); return `${p.y}-${String(p.m + 1).padStart(2, '0')}-${String(p.d).padStart(2, '0')}`; })());

  function drawMonth(from, days, events) {
    const today = todayBkk();
    const g = h('div', { class: 'cal-month' }, [0, 1, 2, 3, 4, 5, 6].map((i) => h('div', { class: 'dow' }, t('dows.' + i))));
    for (let i = 0; i < days; i++) {
      const dt = new Date(from.getTime() + i * DAY);
      const k = isoOf(dt);
      const isToday = dt.getUTCFullYear() === today.y && dt.getUTCMonth() === today.m && dt.getUTCDate() === today.d;
      const cell = h('div', { class: `day ${dt.getUTCMonth() !== anchor.m ? 'other' : ''} ${isToday ? 'today' : ''}`, onclick: (e) => { if (e.target.closest('.ev')) return; anchor = ymd(dt); view = 'day'; draw(); } },
        h('span', { class: 'n' }, dt.getUTCDate()));
      const list = events.filter((e) => dayKey(e) === k);
      for (const e of list.slice(0, 4)) cell.append(h('span', { class: `ev ${e.cls || e.kind || ''}`, title: e.title, onclick: () => e.onClick?.() }, e.start ? time(e.start) + ' ' : '', e.title));
      if (list.length > 4) cell.append(h('span', { class: 'small muted' }, `+${list.length - 4}`));
      g.append(cell);
    }
    clear(grid).append(g);
  }

  function drawWeek(from, days, events) {
    const today = todayBkk();
    const g = h('div', { class: 'cal-week' });
    g.style.setProperty('--days', days);
    g.append(h('div', { class: 'colhead' }));
    const cols = [];
    for (let i = 0; i < days; i++) {
      const dt = new Date(from.getTime() + i * DAY);
      const isToday = dt.getUTCFullYear() === today.y && dt.getUTCMonth() === today.m && dt.getUTCDate() === today.d;
      g.append(h('div', { class: `colhead ${isToday ? 'today' : ''}` }, t('dows.' + ((dt.getUTCDay() + 6) % 7)), ' ', dt.getUTCDate()));
    }
    const hours = h('div', { class: 'hours' });
    for (let hr = h0; hr < h1; hr++) hours.append(h('div', { class: 'hour' }, `${String(hr).padStart(2, '0')}:00`));
    g.append(hours);
    const pxPerMin = 48 / 60;
    for (let i = 0; i < days; i++) {
      const dt = new Date(from.getTime() + i * DAY);
      const dow = (dt.getUTCDay() + 6) % 7;
      const col = h('div', { class: `col ${opts.closed?.(dow) ? 'closed' : ''}` });
      col.style.height = `${(h1 - h0) * 48}px`;
      const k = isoOf(dt);
      for (const e of events.filter((x) => x.start && dayKey(x) === k)) {
        const s = bkkParts(e.start), en = bkkParts(e.end);
        const startMin = (+s.hh - h0) * 60 + +s.mm;
        const sameDay = en.d === s.d;
        const endMin = sameDay ? (+en.hh - h0) * 60 + +en.mm : (h1 - h0) * 60;
        const blk = h('div', { class: `blk ${e.cls || ''}`, title: `${time(e.start)}–${time(e.end)} ${e.title}`, onclick: (ev) => { ev.stopPropagation(); e.onClick?.(); } },
          h('strong', null, `${time(e.start)}–${time(e.end)}`), h('div', null, e.title));
        blk.style.top = `${Math.max(0, startMin) * pxPerMin}px`;
        blk.style.height = `${Math.max(18, (endMin - Math.max(0, startMin)) * pxPerMin - 2)}px`;
        col.append(blk);
      }
      if (opts.onSlot) enableDrag(col, dt, pxPerMin);
      cols.push(col);
      g.append(col);
    }
    clear(grid).append(g);
  }

  // Drag down a day column to pick a time range; a plain click picks one hour.
  function enableDrag(col, dt, pxPerMin) {
    let startY = null, ghost = null;
    const step = opts.slotMinutes?.() || 30;
    const snap = (y) => Math.round(y / pxPerMin / step) * step;
    const toIso = (min) => bkkToIso(dt.getUTCFullYear(), dt.getUTCMonth(), dt.getUTCDate(), h0, min);
    col.addEventListener('pointerdown', (e) => {
      if (e.target !== col) return;
      const r = col.getBoundingClientRect();
      startY = snap(e.clientY - r.top);
      ghost = h('div', { class: 'blk pending' }, '…');
      ghost.style.top = `${startY * pxPerMin}px`; ghost.style.height = `${step * pxPerMin}px`;
      col.append(ghost);
      col.setPointerCapture(e.pointerId);
    });
    col.addEventListener('pointermove', (e) => {
      if (startY == null) return;
      const r = col.getBoundingClientRect();
      const cur = Math.max(startY + step, snap(e.clientY - r.top));
      ghost.style.height = `${(cur - startY) * pxPerMin}px`;
      ghost.textContent = `${fmtMin(startY)}–${fmtMin(cur)}`;
    });
    col.addEventListener('pointerup', (e) => {
      if (startY == null) return;
      const r = col.getBoundingClientRect();
      let end = Math.max(startY + step, snap(e.clientY - r.top));
      if (end - startY === step && step < 60) end = startY + 60;
      ghost.remove();
      const s = startY; startY = null;
      opts.onSlot(toIso(s), toIso(end));
    });
    const fmtMin = (m) => { const tot = h0 * 60 + m; return `${String(Math.floor(tot / 60)).padStart(2, '0')}:${String(tot % 60).padStart(2, '0')}`; };
  }

  draw();
  return { el: root, reload: draw, setView: (v) => { view = v; draw(); } };
}
