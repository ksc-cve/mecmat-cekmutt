import { h, clear } from './dom.js';
import { t, setLang, getLang } from './i18n.js';
import { api, setCsrf, onAuthLost } from './api.js';
import { store, loadLookup } from './store.js';
import { route, fallback, dispatch } from './router.js';
import { signInScreen, forgotScreen, registerScreen, changePasswordScreen, totpVerifyScreen, totpSetupScreen } from './auth.js';
import { mountShell, page } from './shell.js';
import { empty } from './ui/feedback.js';
import { registerPages } from './pages/index.js';

const root = document.getElementById('root');

function applyTheme(theme) {
  if (theme === 'light' || theme === 'dark') document.documentElement.dataset.theme = theme;
  else delete document.documentElement.dataset.theme;
}

// Before sign-in the language comes from the browser (or the last choice on this device).
function initialLang() {
  try { return localStorage.getItem('mecmat.lang') || (navigator.language.startsWith('th') ? 'th' : 'en'); } catch { return 'th'; }
}

async function boot() {
  setLang(initialLang());
  let me;
  try { me = await api.get('/api/auth/me'); } catch { me = { authenticated: false }; }
  await enter(me);
}

async function enter(me) {
  if (!me?.authenticated) return showAnonymous();
  setCsrf(me.csrf);
  store.me = me;
  setLang(me.user.language);
  try { localStorage.setItem('mecmat.lang', me.user.language); } catch { /* private mode */ }
  applyTheme(me.user.theme);
  // A session can be live yet fenced: the server refuses everything else until these are done.
  if (me.user.mustChangePassword) return changePasswordScreen(root, { onDone: enter });
  if (me.totpPending) return totpVerifyScreen(root, { onDone: enter });
  if (me.mustSetupTotp) return totpSetupScreen(root, { onDone: enter });
  await loadLookup();
  mountShell(root, {
    onLang: async () => { await api.put('/api/auth/prefs', { language: getLang() === 'th' ? 'en' : 'th' }); location.reload(); },
    onTheme: async () => {
      const dark = document.documentElement.dataset.theme === 'dark' || (!document.documentElement.dataset.theme && matchMedia('(prefers-color-scheme: dark)').matches);
      const next = dark ? 'light' : 'dark';
      applyTheme(next);
      await api.put('/api/auth/prefs', { theme: next });
      dispatch();
    },
    onSignOut: async () => { await api.post('/api/auth/logout'); location.hash = '#/'; location.reload(); },
  });
  registerPages(route);
  fallback(() => page('common.notFound').append(empty(t('common.notFound'), 'search')));
  window.addEventListener('hashchange', dispatch);
  dispatch();
}

function showAnonymous() {
  const onLang = () => { const l = getLang() === 'th' ? 'en' : 'th'; try { localStorage.setItem('mecmat.lang', l); } catch { /* ignore */ } setLang(l); showAnonymous(); };
  const hash = location.hash;
  if (hash.startsWith('#/forgot')) return forgotScreen(root);
  if (hash.startsWith('#/register')) return registerScreen(root);
  signInScreen(root, { onSignedIn: (me) => { location.hash = hash.startsWith('#/forgot') || hash.startsWith('#/register') ? '#/' : hash; location.reload(); }, onLang });
}

window.addEventListener('hashchange', () => { if (!store.me) showAnonymous(); });
onAuthLost((err) => {
  // The session ended or got fenced while the app was open: start over cleanly.
  // Only for a signed-in app: a 401 from the sign-in form itself is just a wrong password.
  if (store.me) location.reload();
});
boot();
