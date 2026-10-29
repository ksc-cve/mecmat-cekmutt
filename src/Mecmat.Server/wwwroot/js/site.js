// Public site: only the mobile menu toggle. Pages work fully without JavaScript.
document.addEventListener('DOMContentLoaded', () => {
  const btn = document.querySelector('.menutoggle');
  const nav = document.getElementById('sitenav');
  if (!btn || !nav) return;
  btn.addEventListener('click', () => {
    const open = nav.classList.toggle('open');
    btn.setAttribute('aria-expanded', String(open));
  });
});
