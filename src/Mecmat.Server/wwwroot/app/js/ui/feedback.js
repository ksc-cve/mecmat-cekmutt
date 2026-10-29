import { h, clear } from '../dom.js';
import { icon } from '../icons.js';
import { t } from '../i18n.js';
import { describe } from '../api.js';

let toasts;
export function toast(msg, kind = '') {
  if (!toasts) { toasts = h('div', { class: 'ui-toasts', role: 'status', 'aria-live': 'polite' }); document.body.append(toasts); }
  const el = h('div', { class: `ui-toast ${kind}` }, msg);
  toasts.append(el);
  setTimeout(() => el.remove(), kind === 'error' ? 7000 : 3500);
}
export const toastError = (e) => toast(describe(e), 'error');
export const toastOk = (msg) => toast(msg || t('common.saved'), 'ok');

/** Modal with focus kept inside and Escape to close. Returns { el, body, footer, close }. */
export function modal({ title, size = '', onClose } = {}) {
  const prev = document.activeElement;
  const body = h('div', { class: 'body' });
  const footer = h('footer');
  const closeBtn = h('button', { class: 'ui-btn ghost icon', type: 'button', 'aria-label': t('common.close') }, icon('x'));
  const dialog = h('div', { class: `ui-modal ${size}`, role: 'dialog', 'aria-modal': 'true', 'aria-label': title },
    h('header', null, h('h2', null, title), closeBtn), body, footer);
  const backdrop = h('div', { class: 'ui-modal-backdrop' }, dialog);
  let closed = false;
  const close = (result) => {
    if (closed) return;
    closed = true;
    backdrop.remove();
    document.removeEventListener('keydown', onKey);
    prev?.focus?.();
    onClose?.(result);
  };
  const onKey = (e) => {
    if (e.key === 'Escape') { e.preventDefault(); close(); }
    if (e.key === 'Tab') {
      const f = [...dialog.querySelectorAll('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])')].filter((x) => !x.disabled && x.offsetParent);
      if (!f.length) return;
      if (e.shiftKey && document.activeElement === f[0]) { e.preventDefault(); f.at(-1).focus(); }
      else if (!e.shiftKey && document.activeElement === f.at(-1)) { e.preventDefault(); f[0].focus(); }
    }
  };
  closeBtn.addEventListener('click', () => close());
  backdrop.addEventListener('mousedown', (e) => { if (e.target === backdrop) close(); });
  document.addEventListener('keydown', onKey);
  document.body.append(backdrop);
  setTimeout(() => (dialog.querySelector('input, select, textarea') || closeBtn).focus(), 0);
  return { el: dialog, body, footer, close };
}

export function confirmBox(message, { okText, danger = false, title } = {}) {
  return new Promise((resolve) => {
    const m = modal({ title: title || t('common.confirm'), size: 'narrow', onClose: (r) => resolve(!!r) });
    m.body.append(h('p', null, message));
    m.footer.append(
      h('button', { class: 'ui-btn', type: 'button', onclick: () => m.close(false) }, t('common.cancel')),
      h('button', { class: `ui-btn ${danger ? 'danger' : 'primary'}`, type: 'button', onclick: () => m.close(true) }, okText || t('common.ok')));
  });
}

export function promptBox(message, { title, required = true, multiline = true } = {}) {
  return new Promise((resolve) => {
    const m = modal({ title: title || t('common.confirm'), size: 'narrow', onClose: (r) => resolve(r ?? null) });
    const input = multiline ? h('textarea', { class: 'ui-textarea' }) : h('input', { class: 'ui-input' });
    m.body.append(h('div', { class: 'ui-field' }, h('label', null, message), input));
    m.footer.append(
      h('button', { class: 'ui-btn', type: 'button', onclick: () => m.close(null) }, t('common.cancel')),
      h('button', { class: 'ui-btn primary', type: 'button', onclick: () => { if (required && !input.value.trim()) { input.focus(); return; } m.close(input.value.trim()); } }, t('common.ok')));
  });
}

export function empty(text, iconName = 'inbox') {
  return h('div', { class: 'ui-empty' }, icon(iconName), h('p', null, text || t('common.empty')));
}

export function loading(el) { clear(el).append(h('div', { class: 'ui-empty' }, t('common.loading'))); }

/** Runs an async action with the button disabled; reports errors as a toast. */
export async function busy(btn, fn) {
  if (btn) btn.disabled = true;
  try { return await fn(); } catch (e) { toastError(e); throw e; } finally { if (btn) btn.disabled = false; }
}
