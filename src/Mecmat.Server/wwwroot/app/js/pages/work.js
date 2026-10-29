import { h, clear } from '../dom.js';
import { icon } from '../icons.js';
import { t, enumLabel } from '../i18n.js';
import { r, api } from '../api.js';
import { store, can, canAny, nameOf } from '../store.js';
import { DEFS, tone } from '../defs.js';
import { dataGrid, staticTable } from '../ui/grid.js';
import { toastError, toastOk, empty, confirmBox, busy } from '../ui/feedback.js';
import { openForm } from '../ui/form.js';
import { multiPicker } from '../ui/picker.js';
import { page, pageHead } from '../shell.js';
import { editRecord, childGrid, detailList, historyButton, backLink } from './common.js';
import { date, dateTime, relative, todayBkk, isoDate } from '../fmt.js';
import { setQuery } from '../router.js';

const COLS = ['Todo', 'InProgress', 'Review', 'Done'];

export async function tasksPage(_, query) {
  const el = page('nav.tasks');
  if (!canAny('tasks', 'View')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const mine = query.scope !== 'all';
  const projectId = query.project || null;
  const scopeTabs = h('div', { class: 'ui-tabs', style: { margin: 0, border: 0 } },
    h('button', { type: 'button', 'aria-selected': String(mine), onclick: () => (location.hash = '#/tasks') }, t('task.mine')),
    h('button', { type: 'button', 'aria-selected': String(!mine), onclick: () => (location.hash = '#/tasks?scope=all') }, t('common.all')));
  el.append(pageHead(t('nav.tasks'), scopeTabs,
    canAny('tasks', 'Create') ? h('button', { class: 'ui-btn primary', type: 'button', onclick: () => newTask(projectId) }, icon('plus'), t('task.new')) : null));
  const board = h('div', { class: 'kanban' });
  el.append(board);
  try {
    const [tasks, assignees] = await Promise.all([r('tasks').list({ take: 1000, projectId }), r('task-assignees').list({ take: 5000 })]);
    const who = new Map();
    for (const a of assignees.rows) { if (!who.has(a.taskId)) who.set(a.taskId, []); who.get(a.taskId).push(a.personId); }
    const me = store.me.person.id;
    let rows = tasks.rows.filter((x) => x.status !== 'Cancelled');
    if (mine) rows = rows.filter((x) => (who.get(x.id) || []).includes(me) || x.creatorPersonId === me);
    const today = isoDate(todayBkk().y, todayBkk().m, todayBkk().d);
    for (const s of COLS) {
      const list = rows.filter((x) => x.status === s).sort((a, b) => (a.dueDate || '9999').localeCompare(b.dueDate || '9999'));
      const col = h('div', { class: 'col', dataset: { status: s } }, h('h3', null, enumLabel('TaskState', s), h('span', { class: 'muted' }, list.length)));
      col.addEventListener('dragover', (e) => { e.preventDefault(); col.classList.add('drop'); });
      col.addEventListener('dragleave', () => col.classList.remove('drop'));
      col.addEventListener('drop', async (e) => {
        e.preventDefault(); col.classList.remove('drop');
        const id = e.dataTransfer.getData('text/plain');
        const task = rows.find((x) => x.id === id);
        if (!task || task.status === s) return;
        try { await r('tasks').update(id, { status: s, syncStamp: task.syncStamp }); tasksPage(_, query); } catch (err) { toastError(err); }
      });
      for (const x of list) {
        const overdue = x.dueDate && x.dueDate < today && s !== 'Done';
        const card = h('div', { class: 'card', draggable: x._can.edit ? 'true' : 'false', tabIndex: 0, onclick: () => (location.hash = `#/tasks/${x.id}`), onkeydown: (e) => { if (e.key === 'Enter') location.hash = `#/tasks/${x.id}`; } },
          h('div', null, x.title),
          h('div', { class: 'meta' },
            x.priority !== 'Normal' ? h('span', { class: `ui-badge ${tone.status(x.priority)}` }, enumLabel('Priority', x.priority)) : null,
            x.dueDate ? h('span', { class: overdue ? 'ui-badge maroon' : '' }, date(x.dueDate)) : null,
            x.projectId ? h('span', null, nameOf('projects', x.projectId).slice(0, 30)) : null,
            (who.get(x.id) || []).map((p) => h('span', null, '@' + (store.index.people.get(p)?.nickname || store.index.people.get(p)?.firstNameTh || '')))));
        card.addEventListener('dragstart', (e) => e.dataTransfer.setData('text/plain', x.id));
        col.append(card);
      }
      board.append(col);
    }
  } catch (e) { toastError(e); }
}

async function newTask(projectId, extra = {}) {
  const saved = await editRecord('tasks', null, { defaults: { status: 'Todo', priority: 'Normal', projectId, ...extra } });
  if (saved) { await r('task-assignees').create({ taskId: saved.id, personId: store.me.person.id }).catch(() => {}); location.hash = `#/tasks/${saved.id}`; }
}

export async function taskPage({ id }) {
  const el = page('nav.tasks');
  let x;
  try { x = await r('tasks').get(id); } catch (e) { toastError(e); el.append(empty(t('errors.notFound'))); return; }
  const reload = () => taskPage({ id });
  el.append(h('div', { class: 'detail-head' }, h('div', { class: 'grow' }, backLink('#/tasks'), h('h1', null, x.title),
    h('div', { class: 'ui-toolbar' }, h('span', { class: `ui-badge ${tone.status(x.status)}` }, enumLabel('TaskState', x.status)), h('span', { class: `ui-badge ${tone.status(x.priority)}` }, enumLabel('Priority', x.priority)),
      x.sourceMeetingId ? h('a', { class: 'small', href: `#/meetings/${x.sourceMeetingId}` }, t('task.fromMeeting')) : null)),
  h('div', { class: 'ui-toolbar' },
    x._can.edit && x.status !== 'Done' ? h('button', { class: 'ui-btn primary', type: 'button', onclick: (e) => busy(e.target, async () => { await r('tasks').update(id, { status: 'Done', syncStamp: x.syncStamp }); toastOk(); reload(); }) }, icon('check'), t('task.markDone')) : null,
    x._can.edit ? h('button', { class: 'ui-btn', type: 'button', onclick: () => editRecord('tasks', x, { onSaved: reload }) }, icon('edit'), t('common.edit')) : null,
    historyButton('TaskItem', id))));

  const assignees = (await r('task-assignees').list({ taskId: id })).rows;
  const assignBox = multiPicker({
    kind: 'people', values: assignees.map((a) => a.personId),
    onChange: async (ids) => {
      try {
        for (const a of assignees.filter((a) => !ids.includes(a.personId))) await r('task-assignees').remove(a.id);
        for (const p of ids.filter((p) => !assignees.some((a) => a.personId === p))) assignees.push(await r('task-assignees').create({ taskId: id, personId: p }));
        toastOk();
      } catch (e) { toastError(e); }
    },
  });
  const comments = h('div');
  const drawComments = async () => {
    const list = (await r('task-comments').list({ taskId: id, take: 500 })).rows;
    const input = h('textarea', { class: 'ui-textarea', rows: 2, placeholder: t('task.commentPlaceholder') });
    clear(comments).append(...list.map((c) => h('div', { class: 'ui-card', style: { padding: '10px 14px', marginBottom: '8px' } },
      h('div', { class: 'small muted' }, nameOf('people', c.authorPersonId), ' · ', relative(c.createdAt)), h('div', { style: { whiteSpace: 'pre-wrap' } }, c.body))),
    h('div', { class: 'ui-toolbar' }, input, h('button', { class: 'ui-btn primary', type: 'button', onclick: async () => {
      if (!input.value.trim()) return;
      try { await r('task-comments').create({ taskId: id, body: input.value.trim() }); drawComments(); } catch (e) { toastError(e); }
    } }, icon('send'), t('task.comment'))));
  };
  drawComments().catch(toastError);
  el.append(h('div', { class: 'ui-grid c2' },
    h('div', null,
      h('section', { class: 'ui-card section' }, h('header', null, h('h3', null, t('common.details'))), h('div', { class: 'body' },
        detailList(x, DEFS.tasks.fields.filter((f) => !['title', 'description', 'status', 'priority'].includes(f.key))),
        x.description ? h('p', { style: { whiteSpace: 'pre-wrap', marginTop: '12px' } }, x.description) : null,
        h('p', { class: 'small muted', style: { marginTop: '8px' } }, t('task.createdBy', { name: nameOf('people', x.creatorPersonId) })))),
      h('section', { class: 'ui-card section' }, h('header', null, h('h3', null, t('f.assignees'))), h('div', { class: 'body' }, x._can.edit ? assignBox.el : assignees.map((a) => nameOf('people', a.personId)).join(', ')))),
    h('section', null, h('h3', null, t('task.comments')), comments)));
}

export function meetingsPage(_, query) {
  const el = page('nav.meetings');
  if (!canAny('meetings', 'View')) { el.append(empty(t('common.notAllowed'), 'shield')); return; }
  const grid = dataGrid({
    key: 'meetings', columns: DEFS.meetings.columns,
    canCreate: canAny('meetings', 'Create'), createText: t('mtg.new'),
    onCreate: () => newMeeting(),
    onRowClick: (x) => (location.hash = `#/meetings/${x.id}`),
  });
  el.append(pageHead(t('nav.meetings')), grid.el);
  if (query.new) newMeeting();
}

async function newMeeting() {
  const now = new Date();
  const start = new Date(Math.ceil(now.getTime() / 1800000) * 1800000 + 86400000);
  const saved = await editRecord('meetings', null, { defaults: { kind: 'Meeting', status: 'Scheduled', visibility: 'Attendees', startAt: start.toISOString(), endAt: new Date(start.getTime() + 3600000).toISOString() } });
  if (saved) location.hash = `#/meetings/${saved.id}`;
}

export async function meetingPage({ id }) {
  const el = page('nav.meetings');
  let m;
  try { m = await r('meetings').get(id); } catch (e) { toastError(e); el.append(empty(t('errors.notFound'))); return; }
  const reload = () => meetingPage({ id });
  el.append(h('div', { class: 'detail-head' }, h('div', { class: 'grow' }, backLink('#/meetings'), h('h1', null, m.title),
    h('div', { class: 'muted' }, `${dateTime(m.startAt)} – ${dateTime(m.endAt)}`, m.location ? ` · ${m.location}` : ''),
    h('div', { class: 'ui-toolbar', style: { marginTop: '8px' } }, h('span', { class: 'ui-badge navy' }, enumLabel('MeetingKind', m.kind)), h('span', { class: `ui-badge ${tone.status(m.status)}` }, enumLabel('MeetingStatus', m.status)),
      m.onlineUrl ? h('a', { class: 'ui-btn sm', href: m.onlineUrl, target: '_blank', rel: 'noopener' }, icon('link'), t('mtg.join')) : null)),
  h('div', { class: 'ui-toolbar' },
    m._can.edit ? h('button', { class: 'ui-btn', type: 'button', onclick: () => editRecord('meetings', m, { onSaved: reload }) }, icon('edit'), t('common.edit')) : null, historyButton('Meeting', id))));

  const minutesBox = h('div');
  const drawMinutes = async () => {
    const list = (await r('meeting-minutes').list({ meetingId: id })).rows;
    const cur = list[0];
    const ta = h('textarea', { class: 'ui-textarea', rows: 12 });
    ta.value = cur?.body || '';
    clear(minutesBox).append(
      m._can.edit || !cur ? ta : h('div', { style: { whiteSpace: 'pre-wrap' } }, cur.body),
      cur ? h('p', { class: 'small muted' }, t('mtg.recordedBy', { name: nameOf('people', cur.recordedByPersonId) }), ' · ', relative(cur.modifiedAt)) : null,
      m._can.edit ? h('div', { class: 'ui-toolbar' }, h('button', { class: 'ui-btn primary', type: 'button', onclick: async () => {
        try { cur ? await r('meeting-minutes').update(cur.id, { body: ta.value, syncStamp: cur.syncStamp }) : await r('meeting-minutes').create({ meetingId: id, body: ta.value }); toastOk(); drawMinutes(); } catch (e) { toastError(e); }
      } }, t('mtg.saveMinutes'))) : null);
  };
  drawMinutes().catch(toastError);

  const actions = h('div');
  const drawActions = async () => {
    const list = (await r('tasks').list({ sourceMeetingId: id })).rows;
    clear(actions).append(staticTable([{ key: 'title', label: 'f.title' }, { key: 'dueDate', type: 'date' }, { key: 'status', type: 'enum', enum: 'TaskState', tone: tone.status }], list, { onRowClick: (x) => (location.hash = `#/tasks/${x.id}`) }),
      canAny('tasks', 'Create') ? h('button', { class: 'ui-btn sm', type: 'button', style: { marginTop: '8px' }, onclick: async () => {
        const saved = await editRecord('tasks', null, { defaults: { status: 'Todo', priority: 'Normal', projectId: m.projectId, sourceMeetingId: id } });
        if (saved) drawActions();
      } }, icon('plus'), t('mtg.addAction')) : null);
  };
  drawActions().catch(toastError);

  el.append(h('div', { class: 'ui-grid c2' },
    h('div', null,
      m.agenda ? h('section', { class: 'ui-card section' }, h('header', null, h('h3', null, t('f.agenda'))), h('div', { class: 'body', style: { whiteSpace: 'pre-wrap' } }, m.agenda)) : null,
      childGrid('meeting-attendees', 'meetingId', id, {
        canAdd: m._can.edit, title: 'f.attendees',
        columns: [{ key: 'personId', label: 'f.name', render: (a) => (a.personId ? nameOf('people', a.personId) : a.externalName) }, { key: 'response', type: 'enum', enum: 'AttendeeResponse' }, { key: 'attended', type: 'bool' }],
        fields: [{ key: 'personId', type: 'ref', ref: 'people' }, { key: 'externalName' }, { key: 'response', type: 'enum', enum: 'AttendeeResponse', required: true }, { key: 'attended', type: 'bool' }],
        defaults: { response: 'Pending' },
      }),
      h('section', { class: 'section' }, h('h3', null, t('mtg.actions')), actions)),
    h('section', { class: 'ui-card' }, h('header', null, h('h3', null, t('mtg.minutes'))), h('div', { class: 'body' }, minutesBox))));
}
