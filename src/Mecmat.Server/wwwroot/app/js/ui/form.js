import { h, clear } from '../dom.js';
import { icon } from '../icons.js';
import { t, enumLabel } from '../i18n.js';
import { api, ApiError } from '../api.js';
import { money, parseMoney, bkkParts, bkkToIso, fileSize } from '../fmt.js';
import { refPicker, multiPicker } from './picker.js';
import { modal, toastError } from './feedback.js';
import { ENUMS } from '../enums.js';

/**
 * Field descriptor: { key, type, label?, required, enum, ref, span2, hint, readonly, show(values), min, max, filter }
 * Types: text | textarea | html | number | int | money | date | datetime | bool | enum | ref | refs | year | image | file | hours | password | section
 */
export function buildForm(fields, values = {}) {
  const el = h('div', { class: 'ui-form' });
  const controls = {};
  const wraps = {};
  const state = { ...values };

  for (const f of fields) {
    if (f.type === 'section') { el.append(h('h4', { class: 'span2' }, t(f.label))); continue; }
    const id = 'f_' + f.key + '_' + Math.random().toString(36).slice(2, 7);
    const c = control(f, state[f.key], id, (v) => { state[f.key] = v; refreshVisibility(); });
    controls[f.key] = c;
    const label = f.type === 'bool' ? null
      : h('label', { for: id }, t(f.label || 'f.' + f.key), f.required ? h('span', { class: 'req' }, ' *') : null);
    const wrap = h('div', { class: `ui-field ${f.span2 || ['textarea', 'html', 'hours', 'refs'].includes(f.type) ? 'span2' : ''}` },
      label, c.el, f.hint ? h('div', { class: 'hint' }, t(f.hint)) : null, h('div', { class: 'err', 'aria-live': 'polite' }));
    wraps[f.key] = wrap;
    el.append(wrap);
  }
  const refreshVisibility = () => {
    for (const f of fields) if (f.show && wraps[f.key]) wraps[f.key].classList.toggle('hidden', !f.show(collect(true)));
  };
  const collect = (raw = false) => {
    const out = {};
    for (const f of fields) {
      if (f.type === 'section' || !controls[f.key]) continue;
      if (!raw && f.show && !f.show(collect(true))) continue;
      out[f.key] = controls[f.key].get();
    }
    return out;
  };
  refreshVisibility();

  return {
    el, controls,
    values: () => collect(),
    validate() {
      let ok = true;
      for (const f of fields) {
        const w = wraps[f.key];
        if (!w || w.classList.contains('hidden')) continue;
        const v = controls[f.key].get();
        const missing = f.required && (v === null || v === undefined || v === '' || (Array.isArray(v) && !v.length));
        w.classList.toggle('invalid', missing);
        w.querySelector('.err').textContent = missing ? t('form.required') : '';
        if (missing && ok) { ok = false; w.querySelector('input, select, textarea')?.focus(); }
      }
      return ok;
    },
    showError(field) {
      const key = Object.keys(wraps).find((k) => k.toLowerCase() === String(field).toLowerCase());
      if (key) { wraps[key].classList.add('invalid'); wraps[key].querySelector('.err').textContent = t('errors.invalidField'); }
    },
  };
}

function control(f, value, id, onChange) {
  const ro = !!f.readonly;
  const change = (get) => (e) => onChange(get(e));
  switch (f.type) {
    case 'textarea':
    case 'html': {
      // Rich text is kept as simple paragraphs; the server and the public pages sanitise again.
      const el = h('textarea', { class: 'ui-textarea', id, readOnly: ro, rows: f.type === 'html' ? 8 : 4 });
      el.value = f.type === 'html' ? htmlToText(value) : value ?? '';
      el.addEventListener('input', change(() => el.value));
      return { el, get: () => (el.value.trim() === '' ? null : f.type === 'html' ? textToHtml(el.value) : el.value) };
    }
    case 'number':
    case 'int':
    case 'year': {
      const el = h('input', { class: 'ui-input', id, type: 'number', step: f.type === 'number' ? 'any' : '1', min: f.min, max: f.max, readOnly: ro, inputMode: 'decimal' });
      // Years are typed the way people read them: พ.ศ. in Thai is converted to ค.ศ. for storage.
      const be = f.type === 'year' && document.documentElement.lang === 'th';
      el.value = value == null ? '' : be ? value + 543 : value;
      if (be) el.placeholder = 'พ.ศ.';
      el.addEventListener('input', change(() => el.value));
      return {
        el, get: () => {
          if (el.value === '') return null;
          const n = f.type === 'number' ? Number(el.value) : parseInt(el.value, 10);
          return be && n > 2400 ? n - 543 : n;
        },
      };
    }
    case 'money': {
      const el = h('input', { class: 'ui-input num', id, type: 'text', inputMode: 'decimal', readOnly: ro, placeholder: '0.00' });
      el.value = value == null ? '' : money(value);
      el.addEventListener('blur', () => { const v = parseMoney(el.value); if (v != null && el.value) el.value = money(v); });
      el.addEventListener('input', change(() => parseMoney(el.value)));
      return { el, get: () => (el.value.trim() === '' ? null : parseMoney(el.value)) };
    }
    case 'date': {
      const el = h('input', { class: 'ui-input', id, type: 'date', readOnly: ro });
      el.value = value ?? '';
      el.addEventListener('change', change(() => el.value || null));
      return { el, get: () => el.value || null };
    }
    case 'datetime': {
      // Entered and shown in Bangkok time; sent as UTC.
      const el = h('input', { class: 'ui-input', id, type: 'datetime-local', readOnly: ro, step: 300 });
      if (value) { const p = bkkParts(value); el.value = `${p.y}-${String(p.m + 1).padStart(2, '0')}-${String(p.d).padStart(2, '0')}T${p.hh}:${p.mm}`; }
      const get = () => {
        if (!el.value) return null;
        const [d, tm] = el.value.split('T');
        const [y, m, dd] = d.split('-').map(Number);
        const [hh, mm] = tm.split(':').map(Number);
        return bkkToIso(y, m - 1, dd, hh, mm);
      };
      el.addEventListener('change', change(get));
      return { el, get };
    }
    case 'bool': {
      const input = h('input', { type: 'checkbox', id, disabled: ro });
      input.checked = !!value;
      input.addEventListener('change', change(() => input.checked));
      return { el: h('label', { class: 'ui-check', for: id }, input, t(f.label || 'f.' + f.key)), get: () => input.checked };
    }
    case 'enum': {
      const el = h('select', { class: 'ui-select', id, disabled: ro });
      if (!f.required) el.append(h('option', { value: '' }, '—'));
      for (const v of f.values || ENUMS[f.enum] || []) el.append(h('option', { value: v }, enumLabel(f.enum, v)));
      el.value = value ?? (f.required ? (f.values || ENUMS[f.enum])?.[0] ?? '' : '');
      el.addEventListener('change', change(() => el.value || null));
      return { el, get: () => el.value || null };
    }
    case 'ref': {
      const p = refPicker({ kind: f.ref, value, filter: f.filter, onChange, allowEmpty: !f.required });
      p.input.id = id;
      if (ro) p.input.readOnly = true;
      return { el: p.el, get: p.get };
    }
    case 'refs': {
      const p = multiPicker({ kind: f.ref, values: value || [], filter: f.filter, onChange });
      return { el: p.el, get: p.get };
    }
    case 'image':
    case 'file': {
      let current = value ?? null;
      const preview = h('div', { class: 'small muted' });
      const input = h('input', { type: 'file', id, class: 'sr-only', accept: f.type === 'image' ? 'image/png,image/jpeg,image/webp' : undefined });
      const btn = h('label', { class: 'ui-btn sm', for: id }, icon('upload'), t('common.chooseFile'));
      const removeBtn = h('button', { class: 'ui-btn sm ghost', type: 'button', onclick: () => { current = null; draw(); onChange(null); } }, t('common.remove'));
      const draw = () => {
        clear(preview);
        if (!current) { preview.append(t('common.noFile')); removeBtn.classList.add('hidden'); return; }
        removeBtn.classList.remove('hidden');
        if (f.type === 'image') preview.append(h('img', { src: `/api/files/${current}?inline=1`, alt: '', style: { maxHeight: '80px', borderRadius: '8px' } }));
        else preview.append(h('a', { href: `/api/files/${current}`, target: '_blank' }, current));
      };
      input.addEventListener('change', async () => {
        const file = input.files[0];
        if (!file) return;
        btn.classList.add('disabled');
        try {
          const res = await api.upload(file, { isPublic: f.public ? 'true' : 'false' });
          current = res.id;
          draw();
          onChange(current);
        } catch (e) { toastError(e); } finally { input.value = ''; }
      });
      draw();
      return { el: h('div', { class: 'ui-toolbar' }, btn, removeBtn, input, preview), get: () => current };
    }
    case 'hours': return hoursControl(value, onChange);
    case 'password': {
      const el = h('input', { class: 'ui-input', id, type: 'password', autocomplete: f.autocomplete || 'new-password' });
      el.addEventListener('input', change(() => el.value));
      return { el, get: () => el.value };
    }
    default: {
      const el = h('input', { class: 'ui-input', id, type: f.inputType || 'text', readOnly: ro, maxLength: f.max || 500, autocomplete: 'off' });
      el.value = value ?? '';
      el.addEventListener('input', change(() => el.value));
      return { el, get: () => (el.value.trim() === '' ? null : el.value.trim()) };
    }
  }
}

/** Opening hours per weekday, stored as JSON [["08:00","17:00"] | null] × 7 (Mon..Sun). */
function hoursControl(value, onChange) {
  let rows;
  try { rows = value ? JSON.parse(value) : null; } catch { rows = null; }
  if (!Array.isArray(rows) || rows.length !== 7) rows = [...Array(5).fill(['08:00', '17:00']), null, null];
  const grid = h('div', { class: 'ui-grid', style: { gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '8px' } });
  const inputs = rows.map((r, i) => {
    const on = h('input', { type: 'checkbox' }); on.checked = !!r;
    const a = h('input', { class: 'ui-input', type: 'time', step: 900, value: r?.[0] || '08:00' });
    const b = h('input', { class: 'ui-input', type: 'time', step: 900, value: r?.[1] || '17:00' });
    const sync = () => { a.disabled = b.disabled = !on.checked; onChange(get()); };
    [on, a, b].forEach((x) => x.addEventListener('change', sync));
    a.disabled = b.disabled = !on.checked;
    grid.append(h('div', { class: 'ui-card', style: { padding: '8px 10px' } },
      h('label', { class: 'ui-check' }, on, t(`dow.${i}`)), h('div', { class: 'ui-toolbar', style: { margin: '6px 0 0' } }, a, '–', b)));
    return { on, a, b };
  });
  const get = () => JSON.stringify(inputs.map((x) => (x.on.checked ? [x.a.value, x.b.value] : null)));
  return { el: grid, get };
}

function htmlToText(html) {
  if (!html) return '';
  const d = new DOMParser().parseFromString(html, 'text/html');
  return [...d.body.querySelectorAll('p, li, h3, h4, blockquote')].map((p) => p.textContent).join('\n\n') || d.body.textContent;
}
function textToHtml(text) {
  const esc = (s) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  return text.split(/\n{2,}/).map((p) => `<p>${esc(p.trim()).replace(/\n/g, '<br>')}</p>`).join('');
}

/** Standard create/edit dialog. onSubmit(values) may throw an ApiError; field errors are marked. */
export function openForm({ title, fields, values = {}, onSubmit, size = '', submitText, extra }) {
  return new Promise((resolve) => {
    const m = modal({ title, size, onClose: (r) => resolve(r) });
    const form = buildForm(fields, values);
    m.body.append(form.el);
    if (extra) m.body.append(extra);
    const save = h('button', { class: 'ui-btn primary', type: 'submit' }, submitText || t('common.save'));
    const wrapper = h('form', { onsubmit: async (e) => {
      e.preventDefault();
      if (!form.validate()) return;
      save.disabled = true;
      try { const r = await onSubmit(form.values(), form); m.close(r ?? true); }
      catch (err) { if (err instanceof ApiError && err.detail?.field) form.showError(err.detail.field); toastError(err); }
      finally { save.disabled = false; }
    } });
    // The form element wraps body+footer so Enter submits.
    m.el.insertBefore(wrapper, m.body);
    wrapper.append(m.body, m.footer);
    m.footer.append(h('button', { class: 'ui-btn', type: 'button', onclick: () => m.close(null) }, t('common.cancel')), save);
  });
}
