import { h, clear } from '../dom.js';
import { icon } from '../icons.js';
import { t, enumLabel, pick } from '../i18n.js';
import { api } from '../api.js';
import { store, can, canAny, nameOf } from '../store.js';
import { toastError, toastOk, empty, modal, promptBox, confirmBox } from '../ui/feedback.js';
import { openForm } from '../ui/form.js';
import { page, pageHead } from '../shell.js';
import { calendar } from '../ui/calendar.js';
import { dateTime, time, date } from '../fmt.js';
import { setQuery } from '../router.js';

export function bookingsPage(_, query) {
  const el = page('nav.bookings');
  const resources = store.lookup.resources.filter((r) => r.isActive);
  if (!resources.length) {
    el.append(pageHead(t('nav.bookings')), empty(t('bk.noResources'), 'box'),
      canAny('resources', 'Edit') ? h('p', { class: 'ui-empty' }, h('a', { class: 'ui-btn primary', href: '#/admin/resources' }, t('bk.addResource'))) : null);
    return;
  }
  let resourceId = query.resource && resources.some((r) => r.id === query.resource) ? query.resource : resources[0].id;
  const res = () => resources.find((r) => r.id === resourceId);
  const sel = h('select', { class: 'ui-select', style: { maxWidth: '320px' }, 'aria-label': t('f.resourceId'), onchange: () => { resourceId = sel.value; setQuery({ resource: resourceId }); info(); cal.reload(); } },
    resources.map((r) => h('option', { value: r.id }, pick(r, 'name') + (r.requiresApproval ? ` (${t('f.requiresApproval')})` : ''))));
  sel.value = resourceId;
  const infoBox = h('p', { class: 'small muted' });
  const info = () => {
    const r = res();
    let hours = '';
    try {
      const oh = r.openHoursJson ? JSON.parse(r.openHoursJson) : null;
      if (oh) hours = oh.map((d, i) => (d ? `${t('dows.' + i)} ${d[0]}–${d[1]}` : null)).filter(Boolean).join(', ');
    } catch { /* malformed hours: show nothing */ }
    infoBox.textContent = [r.location, hours && `${t('f.openHoursJson')}: ${hours}`, r.slotMinutes && `${t('f.slotMinutes')}: ${r.slotMinutes}`, r.managerPersonId && `${t('f.managerPersonId')}: ${nameOf('people', r.managerPersonId)}`].filter(Boolean).join(' · ');
  };
  info();

  const openDays = () => { try { const oh = JSON.parse(res().openHoursJson || 'null'); return oh; } catch { return null; } };
  const cal = calendar({
    view: query.view || 'week',
    hours: [7, 21],
    slotMinutes: () => res().slotMinutes || 30,
    closed: (dow) => { const oh = openDays(); return oh ? !oh[dow] : false; },
    toolbar: sel,
    load: async (from, to) => {
      const d = await api.get(`/api/bookings?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}&resourceId=${resourceId}`);
      return [
        ...d.bookings.map((b) => ({
          start: b.startAt, end: b.endAt, title: b.title || b.booker || t('bk.busy'), cls: b.mine ? (b.status === 'Pending' ? 'pending' : '') : 'other',
          onClick: () => showBooking(b, cal),
        })),
        ...d.blackouts.map((x) => ({ start: x.startAt, end: x.endAt, title: x.reason || t('bk.closed'), cls: 'blackout' })),
      ];
    },
    onSlot: canAny('bookings', 'Create') ? (s, e) => newBooking(resourceId, s, e, cal) : null,
    legend: [h('span', null, h('i', { style: { background: 'var(--c-navy)' } }), t('bk.legendMine')), h('span', null, h('i', { style: { background: 'var(--c-yellow)' } }), t('bk.legendPending')),
      h('span', null, h('i', { style: { background: 'var(--c-grey)' } }), t('bk.legendOther')), h('span', null, h('i', { style: { background: 'var(--c-maroon-50)', border: '1px dashed var(--c-maroon)' } }), t('bk.legendClosed'))],
  });
  el.append(pageHead(t('nav.bookings'),
    canAny('bookings', 'Create') ? h('button', { class: 'ui-btn primary', type: 'button', onclick: () => newBooking(resourceId, null, null, cal) }, icon('plus'), t('bk.new')) : null),
  h('p', { class: 'small muted' }, t('bk.intro')), infoBox, cal.el);
}

function newBooking(resourceId, startAt, endAt, cal) {
  return openForm({
    title: t('bk.new'),
    fields: [
      { key: 'resourceId', type: 'ref', ref: 'resources', required: true, filter: (r) => r.isActive },
      { key: 'title', required: true, label: 'bk.title' }, { key: 'startAt', type: 'datetime', required: true }, { key: 'endAt', type: 'datetime', required: true },
      { key: 'projectId', type: 'ref', ref: 'projects' }, { key: 'repeatWeeks', type: 'int', min: 1, max: 26, label: 'bk.repeat', hint: 'bk.repeatHint' }, { key: 'note', type: 'textarea' }],
    values: { resourceId, startAt, endAt, repeatWeeks: 1 },
    onSubmit: async (v) => {
      const res = await api.post('/api/bookings', v);
      toastOk(res.status === 'Pending' ? t('bk.sentForApproval') : t('bk.confirmed'));
      cal.reload();
    },
  });
}

function showBooking(b, cal) {
  const m = modal({ title: b.title || t('bk.busy'), size: 'narrow' });
  m.body.append(h('dl', { class: 'ui-dl' },
    h('dt', null, t('f.resourceId')), h('dd', null, nameOf('resources', b.resourceId)),
    h('dt', null, t('f.startAt')), h('dd', null, dateTime(b.startAt)),
    h('dt', null, t('f.endAt')), h('dd', null, dateTime(b.endAt)),
    h('dt', null, t('f.bookerPersonId')), h('dd', null, b.booker || ''),
    h('dt', null, t('f.status')), h('dd', null, enumLabel('BookingStatus', b.status)),
    b.projectId ? [h('dt', null, t('f.projectId')), h('dd', null, nameOf('projects', b.projectId))] : null,
    b.note ? [h('dt', null, t('f.note')), h('dd', null, b.note)] : null));
  if (b.canCancel) {
    const cancel = async (series) => {
      const reason = b.mine ? null : await promptBox(t('bk.cancelReason'));
      if (!b.mine && !reason) return;
      if (b.mine && !(await confirmBox(t('bk.cancelConfirm'), { danger: true }))) return;
      try { await api.post(`/api/bookings/${b.id}/cancel${series ? '?series=true' : ''}`, { reason }); toastOk(); m.close(); cal.reload(); } catch (e) { toastError(e); }
    };
    m.footer.append(b.recurrenceGroupId ? h('button', { class: 'ui-btn danger', type: 'button', onclick: () => cancel(true) }, t('bk.cancelSeries')) : null,
      h('button', { class: 'ui-btn danger', type: 'button', onclick: () => cancel(false) }, t('bk.cancel')));
  }
  m.footer.append(h('button', { class: 'ui-btn', type: 'button', onclick: () => m.close() }, t('common.close')));
}

export function calendarPage() {
  const el = page('nav.calendar');
  const cal = calendar({
    view: 'month',
    hours: [7, 21],
    load: async (from, to) => (await api.get(`/api/calendar?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`)).map((e) => ({
      start: e.start, end: e.end, date: e.date, title: (e.kind !== 'meeting' && e.kind !== 'booking' ? t('cal.kind.' + e.kind) + ': ' : '') + (e.kind === 'report' ? enumLabel('ReportKind', e.title) : e.title),
      cls: e.kind === 'booking' ? (e.status === 'Pending' ? 'pending' : '') : e.kind === 'meeting' ? '' : e.kind, onClick: () => (location.hash = e.link),
    })),
    legend: [h('span', null, h('i', { style: { background: 'var(--c-navy)' } }), t('cal.legendEvents')), h('span', null, h('i', { style: { background: 'var(--c-orange)' } }), t('cal.legendDue'))],
  });
  el.append(pageHead(t('nav.calendar'),
    canAny('meetings', 'Create') ? h('a', { class: 'ui-btn primary', href: '#/meetings?new=1' }, icon('plus'), t('mtg.new')) : null), cal.el);
}
