// Tiny element builder. Styles are set through el.style (CSSOM), never as a style attribute,
// because the CSP (style-src 'self') blocks inline style attributes but not CSSOM writes.
export function h(tag, attrs, ...children) {
  const el = document.createElement(tag);
  if (attrs) {
    for (const [k, v] of Object.entries(attrs)) {
      if (v === undefined || v === null || v === false) continue;
      if (k === 'class') el.className = v;
      else if (k === 'style') Object.assign(el.style, v);
      else if (k === 'dataset') Object.assign(el.dataset, v);
      else if (k.startsWith('on') && typeof v === 'function') el.addEventListener(k.slice(2).toLowerCase(), v);
      else if (k === 'html') throw new Error('h(): raw html is not allowed; use sanitizeHtml + setHtml');
      else if (k in el && typeof v !== 'string' && k !== 'list') el[k] = v;
      else el.setAttribute(k, v === true ? '' : String(v));
    }
  }
  append(el, children);
  return el;
}

export function append(el, children) {
  for (const c of children.flat(Infinity)) {
    if (c === null || c === undefined || c === false) continue;
    el.append(c instanceof Node ? c : document.createTextNode(String(c)));
  }
  return el;
}

export function clear(el) { while (el.firstChild) el.firstChild.remove(); return el; }

export const $ = (sel, root = document) => root.querySelector(sel);

// Rich text from the editor is reduced to a small allow-list before it touches innerHTML.
const ALLOWED = new Set(['P', 'BR', 'B', 'STRONG', 'I', 'EM', 'U', 'UL', 'OL', 'LI', 'H3', 'H4', 'A', 'BLOCKQUOTE', 'SUB', 'SUP']);
export function sanitizeHtml(html) {
  const doc = new DOMParser().parseFromString(`<div>${html || ''}</div>`, 'text/html');
  const walk = (node) => {
    for (const child of [...node.childNodes]) {
      if (child.nodeType === 1) {
        if (!ALLOWED.has(child.tagName)) { child.replaceWith(...child.childNodes); walk(node); return; }
        for (const a of [...child.attributes]) {
          const ok = child.tagName === 'A' && a.name === 'href' && /^(https?:|mailto:|#|\/)/i.test(a.value);
          if (!ok) child.removeAttribute(a.name);
        }
        if (child.tagName === 'A') { child.setAttribute('rel', 'noopener noreferrer'); child.setAttribute('target', '_blank'); }
        walk(child);
      } else if (child.nodeType !== 3) child.remove();
    }
  };
  const root = doc.body.firstChild;
  walk(root);
  return root.innerHTML;
}

export function setHtml(el, html) { el.innerHTML = sanitizeHtml(html); return el; }
