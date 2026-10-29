import { h, clear } from '../dom.js';
import { icon } from '../icons.js';
import { t } from '../i18n.js';
import { store, nameOf } from '../store.js';

/** Searchable single picker over a lookup list (people, projects, accounts …). */
export function refPicker({ kind, value = null, filter, placeholder, onChange, allowEmpty = true }) {
  let current = value;
  const input = h('input', { class: 'ui-input', type: 'text', placeholder: placeholder || t('common.search'), autocomplete: 'off', role: 'combobox', 'aria-expanded': 'false' });
  const list = h('div', { class: 'ui-picker-list hidden', role: 'listbox' });
  const wrap = h('div', { class: 'ui-picker' }, input, list);
  let active = -1;
  let shown = [];
  const label = (id) => (id ? nameOf(kind, id) : '');
  const items = () => (store.lookup?.[kind] || []).filter((x) => !filter || filter(x));
  input.value = label(current);

  const render = () => {
    const q = input.value.trim().toLowerCase();
    shown = items().filter((x) => !q || nameOf(kind, x.id).toLowerCase().includes(q) || (x.nickname || '').toLowerCase().includes(q)).slice(0, 50);
    clear(list);
    if (allowEmpty) list.append(h('button', { type: 'button', onmousedown: (e) => { e.preventDefault(); choose(null); } }, h('span', { class: 'muted' }, t('common.none'))));
    shown.forEach((x, i) => list.append(h('button', { type: 'button', role: 'option', 'aria-selected': String(i === active), onmousedown: (e) => { e.preventDefault(); choose(x.id); } }, nameOf(kind, x.id))));
    list.classList.remove('hidden');
    input.setAttribute('aria-expanded', 'true');
  };
  const hide = () => { list.classList.add('hidden'); input.setAttribute('aria-expanded', 'false'); active = -1; };
  const choose = (id) => { current = id; input.value = label(id); hide(); onChange?.(id); };

  input.addEventListener('focus', () => { input.select(); render(); });
  input.addEventListener('input', () => { active = -1; render(); });
  input.addEventListener('blur', () => { hide(); input.value = label(current); });
  input.addEventListener('keydown', (e) => {
    if (e.key === 'ArrowDown') { active = Math.min(active + 1, shown.length - 1); render(); e.preventDefault(); }
    else if (e.key === 'ArrowUp') { active = Math.max(active - 1, 0); render(); e.preventDefault(); }
    else if (e.key === 'Enter' && active >= 0) { choose(shown[active].id); e.preventDefault(); }
    else if (e.key === 'Escape') hide();
  });
  return { el: wrap, input, get: () => current, set: (v) => { current = v; input.value = label(v); } };
}

/** Multi picker shown as chips (meeting attendees, task assignees …). */
export function multiPicker({ kind, values = [], filter, onChange }) {
  let current = [...values];
  const chips = h('div', { class: 'ui-chips' });
  const p = refPicker({
    kind, filter: (x) => !current.includes(x.id) && (!filter || filter(x)), allowEmpty: false, placeholder: t('common.addPerson'),
    onChange: (id) => { if (id && !current.includes(id)) { current.push(id); draw(); onChange?.(current); } p.set(null); },
  });
  const draw = () => {
    clear(chips);
    current.forEach((id) => chips.append(h('span', { class: 'ui-chip' }, nameOf(kind, id),
      h('button', { type: 'button', 'aria-label': t('common.remove'), onclick: () => { current = current.filter((x) => x !== id); draw(); onChange?.(current); } }, icon('x')))));
  };
  draw();
  return { el: h('div', null, p.el, chips), get: () => current, set: (v) => { current = [...v]; draw(); } };
}
