/* Compatibility route for bookmarks to the former transaction operations page. */
document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireRole('MicrogridOperator')) return;
  window.location.replace(new URL('transactions.html?view=attention', window.location.href).href);
});
