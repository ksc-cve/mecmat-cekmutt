import { t } from './i18n.js';

let csrf = null;
export function setCsrf(v) { csrf = v; }

export class ApiError extends Error {
  constructor(status, body) {
    super(body?.error || `HTTP ${status}`);
    this.status = status;
    this.key = body?.error;
    this.detail = body?.detail;
    this.reference = body?.reference;
  }
  get message() { return describe(this); }
}

export function describe(e) {
  if (!(e instanceof ApiError)) return String(e?.message || e);
  let msg = t(e.key || 'errors.server', e.detail || {});
  if (e.detail?.field) msg += ` (${t('f.' + e.detail.field)})`;
  if (e.reference) msg += ` — ${t('errors.reference')}: ${e.reference}`;
  return msg;
}

const listeners = new Set();
/** Lets main.js send the user back to sign-in when a session ends mid-use. */
export function onAuthLost(fn) { listeners.add(fn); }

async function request(method, url, body, { raw = false } = {}) {
  const opts = { method, headers: {}, credentials: 'same-origin' };
  if (csrf && method !== 'GET') opts.headers['X-CSRF'] = csrf;
  if (body instanceof FormData) opts.body = body;
  else if (body !== undefined) { opts.headers['Content-Type'] = 'application/json'; opts.body = JSON.stringify(body); }
  const res = await fetch(url, opts);
  if (raw) return res;
  const text = await res.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { data = { error: 'errors.server' }; }
  if (!res.ok) {
    const err = new ApiError(res.status, data);
    if (res.status === 401 || ['auth.mustChangePassword', 'auth.totpRequired', 'auth.mustSetupTotp'].includes(err.key)) listeners.forEach((f) => f(err));
    throw err;
  }
  if (data && data.csrf) csrf = data.csrf;
  return data;
}

export const api = {
  get: (u) => request('GET', u),
  post: (u, b) => request('POST', u, b ?? {}),
  put: (u, b) => request('PUT', u, b),
  del: (u) => request('DELETE', u),
  upload: (file, extra = {}) => {
    const fd = new FormData();
    fd.append('file', file);
    for (const [k, v] of Object.entries(extra)) if (v != null) fd.append(k, v);
    return request('POST', '/api/files', fd);
  },
  uploadTo: (url, file, extra = {}) => {
    const fd = new FormData();
    fd.append('file', file);
    for (const [k, v] of Object.entries(extra)) if (v != null) fd.append(k, v);
    return request('POST', url, fd);
  },
};

/** Generic collection client for /api/r/{key}. */
export const r = (key) => ({
  list: (params = {}) => api.get(`/api/r/${key}?` + new URLSearchParams(Object.entries(params).filter(([, v]) => v != null && v !== ''))),
  get: (id) => api.get(`/api/r/${key}/${id}`),
  create: (body) => api.post(`/api/r/${key}`, body),
  update: (id, body) => api.put(`/api/r/${key}/${id}`, body),
  remove: (id) => api.del(`/api/r/${key}/${id}`),
  restore: (id) => api.post(`/api/r/${key}/${id}/restore`),
});
