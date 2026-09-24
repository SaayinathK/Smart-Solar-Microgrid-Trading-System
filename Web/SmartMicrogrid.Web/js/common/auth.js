/* ==========================================================================
   Smart Microgrid Energy System - Route Guard & Authentication Controller
   ========================================================================== */

function getAuthAppUrl(path) {
  const authScript = Array.from(document.scripts).find(script =>
    new URL(script.src, document.baseURI).pathname.endsWith('/js/common/auth.js')
  );
  const appBase = authScript
    ? new URL('../../', authScript.src)
    : new URL('.', window.location.href);

  return new URL(path, appBase).href;
}

const AuthGuard = {
  requireAuth(requiredRole = null) {
    if (!SessionManager.isAuthenticated()) {
      window.location.href = getAuthAppUrl('login.html');
      return false;
    }

    if (requiredRole && SessionManager.getUserRole() !== requiredRole) {
      ApiClient.showToast('Access denied: You do not have permission to view this page.', 'error');
      setTimeout(() => {
        window.location.href = getAuthAppUrl('dashboard.html');
      }, 1500);
      return false;
    }

    return true;
  },

  redirectIfAuthenticated() {
    if (SessionManager.isAuthenticated()) {
      const landingPage = SessionManager.getUserRole() === 'TransactionVerifier'
        ? 'pages/M3/dashboard.html'
        : 'dashboard.html';
      window.location.href = getAuthAppUrl(landingPage);
    }
  },

  logout() {
    SessionManager.clearSession();
    ApiClient.showToast('You have been logged out successfully.', 'info');
    setTimeout(() => {
      window.location.href = getAuthAppUrl('login.html');
    }, 1000);
  }
};
