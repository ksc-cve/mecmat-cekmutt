import { h, clear } from './dom.js';
import { t, getLang } from './i18n.js';
import { api } from './api.js';
import { toast, toastError } from './ui/feedback.js';
import { ENUMS } from './enums.js';
import { enumLabel } from './i18n.js';

function card(title, ...content) {
  return h('div', { class: 'auth' }, h('div', { class: 'ui-card' }, h('div', { class: 'body' },
    h('div', { class: 'brand-line' }, h('img', { src: '/assets/img/favicon.svg', alt: '', width: 40, height: 40 }),
      h('div', null, h('strong', null, t('app.name')), h('div', { class: 'small muted' }, t('app.fullName')))),
    h('h1', null, title), ...content)));
}

function field(labelKey, input) {
  const id = 'a_' + Math.random().toString(36).slice(2, 8);
  input.id = id;
  return h('div', { class: 'ui-field' }, h('label', { for: id }, t(labelKey)), input);
}

const langSwitch = (onSwitch) => h('button', { class: 'ui-btn ghost sm', type: 'button', onclick: onSwitch }, t('nav.language'));

export function signInScreen(root, { onSignedIn, onLang }) {
  const user = h('input', { class: 'ui-input', autocomplete: 'username', required: true, autocapitalize: 'none' });
  const pass = h('input', { class: 'ui-input', type: 'password', autocomplete: 'current-password', required: true });
  const btn = h('button', { class: 'ui-btn primary', type: 'submit' }, t('auth.signIn'));
  const form = h('form', { onsubmit: async (e) => {
    e.preventDefault();
    btn.disabled = true;
    try { onSignedIn(await api.post('/api/auth/login', { username: user.value.trim(), password: pass.value })); }
    catch (err) { toastError(err); pass.select(); }
    finally { btn.disabled = false; }
  } }, field('auth.username', user), field('auth.password', pass), btn,
  h('div', { class: 'links' }, h('a', { href: '#/forgot' }, t('auth.forgot')), h('a', { href: '#/register' }, t('auth.register')), langSwitch(onLang)));
  clear(root).append(card(t('auth.signInTitle'), form));
  user.focus();
}

export function forgotScreen(root) {
  const user = h('input', { class: 'ui-input', autocomplete: 'username', required: true, autocapitalize: 'none' });
  const note = h('textarea', { class: 'ui-textarea', rows: 2 });
  const btn = h('button', { class: 'ui-btn primary', type: 'submit' }, t('common.submit'));
  const form = h('form', { onsubmit: async (e) => {
    e.preventDefault(); btn.disabled = true;
    try { await api.post('/api/auth/forgot', { username: user.value.trim(), note: note.value.trim() || null }); clear(root).append(card(t('auth.forgot'), h('p', null, t('auth.forgotSent')), h('a', { href: '#/' }, t('auth.backToSignIn')))); }
    catch (err) { toastError(err); } finally { btn.disabled = false; }
  } }, h('p', { class: 'small muted' }, t('auth.forgotIntro')), field('auth.username', user), field('auth.forgotNote', note), btn,
  h('div', { class: 'links' }, h('a', { href: '#/' }, t('auth.backToSignIn'))));
  clear(root).append(card(t('auth.forgot'), form));
}

export function registerScreen(root) {
  const i = (attrs = {}) => h('input', { class: 'ui-input', ...attrs });
  const f = {
    firstNameTh: i({ required: true }), lastNameTh: i({ required: true }), firstNameEn: i(), lastNameEn: i(),
    email: i({ type: 'email', autocomplete: 'email' }), phone: i({ type: 'tel', autocomplete: 'tel' }),
    username: i({ required: true, autocapitalize: 'none', autocomplete: 'username' }),
    password: i({ type: 'password', required: true, autocomplete: 'new-password' }),
    confirm: i({ type: 'password', required: true, autocomplete: 'new-password' }),
  };
  const cat = h('select', { class: 'ui-select' }, ENUMS.PersonCategory.map((v) => h('option', { value: v }, enumLabel('PersonCategory', v))));
  cat.value = 'MasterStudent';
  const note = h('textarea', { class: 'ui-textarea', rows: 2 });
  const btn = h('button', { class: 'ui-btn primary', type: 'submit' }, t('auth.register'));
  const form = h('form', { onsubmit: async (e) => {
    e.preventDefault();
    if (f.password.value !== f.confirm.value) { toast(t('auth.passwordsDiffer'), 'error'); return; }
    btn.disabled = true;
    try {
      await api.post('/api/auth/register', {
        username: f.username.value.trim().toLowerCase(), password: f.password.value, firstNameTh: f.firstNameTh.value.trim(), lastNameTh: f.lastNameTh.value.trim(),
        firstNameEn: f.firstNameEn.value.trim() || null, lastNameEn: f.lastNameEn.value.trim() || null, email: f.email.value.trim() || null,
        phone: f.phone.value.trim() || null, category: cat.value, note: note.value.trim() || null,
      });
      clear(root).append(card(t('auth.register'), h('p', null, t('auth.registerSent')), h('a', { href: '#/' }, t('auth.backToSignIn'))));
    } catch (err) { toastError(err); } finally { btn.disabled = false; }
  } },
  h('p', { class: 'small muted' }, t('auth.registerIntro')),
  h('div', { class: 'ui-form' },
    field('f.firstNameTh', f.firstNameTh), field('f.lastNameTh', f.lastNameTh), field('f.firstNameEn', f.firstNameEn), field('f.lastNameEn', f.lastNameEn),
    field('f.category', cat), field('f.email', f.email), field('f.phone', f.phone), field('auth.username', f.username),
    field('auth.password', f.password), field('auth.confirmPassword', f.confirm)),
  h('p', { class: 'small muted' }, t('auth.passwordRules')), field('f.note', note), btn,
  h('div', { class: 'links' }, h('a', { href: '#/' }, t('auth.backToSignIn'))));
  clear(root).append(card(t('auth.register'), form));
  f.firstNameTh.focus();
}

export function changePasswordScreen(root, { onDone, intro = true }) {
  const cur = h('input', { class: 'ui-input', type: 'password', autocomplete: 'current-password', required: true });
  const nw = h('input', { class: 'ui-input', type: 'password', autocomplete: 'new-password', required: true });
  const cf = h('input', { class: 'ui-input', type: 'password', autocomplete: 'new-password', required: true });
  const btn = h('button', { class: 'ui-btn primary', type: 'submit' }, t('auth.changeButton'));
  const form = h('form', { onsubmit: async (e) => {
    e.preventDefault();
    if (nw.value !== cf.value) { toast(t('auth.passwordsDiffer'), 'error'); return; }
    btn.disabled = true;
    try { const me = await api.post('/api/auth/password', { current: cur.value, new: nw.value }); toast(t('auth.changed'), 'ok'); onDone(me); }
    catch (err) { toastError(err); } finally { btn.disabled = false; }
  } }, intro ? h('p', { class: 'small muted' }, t('auth.changeIntro')) : null,
  field('auth.currentPassword', cur), field('auth.newPassword', nw), field('auth.confirmPassword', cf), h('p', { class: 'small muted' }, t('auth.passwordRules')), btn,
  h('div', { class: 'links' }, h('a', { href: '#/', onclick: async (e) => { e.preventDefault(); await api.post('/api/auth/logout'); location.reload(); } }, t('nav.signOut'))));
  clear(root).append(card(t('auth.changeTitle'), form));
  cur.focus();
}

export function totpVerifyScreen(root, { onDone }) {
  const code = h('input', { class: 'ui-input', inputMode: 'numeric', autocomplete: 'one-time-code', required: true, maxLength: 8 });
  const btn = h('button', { class: 'ui-btn primary', type: 'submit' }, t('auth.verify'));
  const form = h('form', { onsubmit: async (e) => {
    e.preventDefault(); btn.disabled = true;
    try { onDone(await api.post('/api/auth/totp/verify', { code: code.value.trim() })); } catch (err) { toastError(err); code.select(); } finally { btn.disabled = false; }
  } }, h('p', { class: 'small muted' }, t('auth.totpIntro')), field('auth.totpCode', code), btn,
  h('div', { class: 'links' }, h('a', { href: '#/', onclick: async (e) => { e.preventDefault(); await api.post('/api/auth/logout'); location.reload(); } }, t('nav.signOut'))));
  clear(root).append(card(t('auth.totpTitle'), form));
  code.focus();
}

export async function totpSetupScreen(root, { onDone }) {
  let setup;
  try { setup = await api.post('/api/auth/totp/setup'); } catch (e) { toastError(e); return; }
  const code = h('input', { class: 'ui-input', inputMode: 'numeric', autocomplete: 'one-time-code', required: true, maxLength: 6 });
  const btn = h('button', { class: 'ui-btn primary', type: 'submit' }, t('auth.verify'));
  const form = h('form', { onsubmit: async (e) => {
    e.preventDefault(); btn.disabled = true;
    try {
      const res = await api.post('/api/auth/totp/enable', { code: code.value.trim() });
      const done = h('button', { class: 'ui-btn primary', type: 'button', onclick: async () => onDone(await api.get('/api/auth/me')) }, t('auth.recoveryDone'));
      clear(root).append(card(t('auth.recoveryTitle'), h('p', { class: 'small muted' }, t('auth.recoveryIntro')),
        h('div', { class: 'code-box' }, res.recoveryCodes.join('  ')),
        h('div', { class: 'ui-toolbar', style: { marginTop: '12px' } },
          h('button', { class: 'ui-btn', type: 'button', onclick: () => { navigator.clipboard?.writeText(res.recoveryCodes.join('\n')); toast(t('common.copied'), 'ok'); } }, t('common.copy')), done)));
    } catch (err) { toastError(err); code.select(); } finally { btn.disabled = false; }
  } },
  h('p', { class: 'small muted' }, t('auth.setupIntro')),
  h('p', null, t('auth.setupStep1')),
  h('div', { class: 'code-box' }, setup.secret.replace(/(.{4})/g, '$1 ').trim()),
  h('p', { class: 'small' }, h('a', { href: setup.uri }, t('auth.setupLink'))),
  h('p', null, t('auth.setupStep2')), field('auth.totpCode', code), btn);
  clear(root).append(card(t('auth.setupTitle'), form));
  code.focus();
}
