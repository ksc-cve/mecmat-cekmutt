// Hash router: '#/projects/PRJ-…?tab=team'. Routes are patterns with :params.
const routes = [];
let notFound = null;
let current = null;

export function route(pattern, handler) {
  const keys = [];
  const re = new RegExp('^' + pattern.replace(/:(\w+)/g, (_, k) => { keys.push(k); return '([^/]+)'; }) + '$');
  routes.push({ re, keys, handler });
}
export function fallback(handler) { notFound = handler; }

export function parse() {
  const raw = location.hash.slice(1) || '/';
  const [path, qs] = raw.split('?');
  return { path, query: Object.fromEntries(new URLSearchParams(qs || '')) };
}

export async function dispatch() {
  const { path, query } = parse();
  for (const r of routes) {
    const m = path.match(r.re);
    if (m) {
      const params = Object.fromEntries(r.keys.map((k, i) => [k, decodeURIComponent(m[i + 1])]));
      current = { path, params, query };
      return r.handler(params, query);
    }
  }
  current = { path, params: {}, query };
  return notFound?.();
}

export const go = (hash) => { if (location.hash === hash) dispatch(); else location.hash = hash; };
export const currentRoute = () => current;

/** Update the query string without re-rendering the page (e.g. switching tabs). */
export function setQuery(patch) {
  const { path, query } = parse();
  const q = new URLSearchParams({ ...query, ...patch });
  for (const [k, v] of [...q]) if (v === '' || v === 'null' || v === 'undefined') q.delete(k);
  history.replaceState(null, '', '#' + path + (q.toString() ? '?' + q : ''));
}
