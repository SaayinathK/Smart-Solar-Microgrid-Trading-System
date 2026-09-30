/* ==========================================================================
   Smart Microgrid Energy System - Route Guard & Authentication Controller
   ========================================================================== */

const AuthGuard = {
  requireAuth(requiredRole = null) {
    if (!SessionManager.isAuthenticated()) {
      window.location.href = '/login.html';
      return false;
    }

    const allowedRoles = requiredRole == null ? [] : (Array.isArray(requiredRole) ? requiredRole : [requiredRole]);
    if (allowedRoles.length && !allowedRoles.includes(SessionManager.getUserRole())) {
      ApiClient.showToast('Access denied: You do not have permission to view this page.', 'error');
      setTimeout(() => {
        window.location.href = '/dashboard.html';
      }, 1500);
      return false;
    }

    return true;
  },

  requireRole(roles) {
    return this.requireAuth(roles);
  },

  redirectIfAuthenticated() {
    if (SessionManager.isAuthenticated()) {
      window.location.href = '/dashboard.html';
    }
  },

  logout() {
    SessionManager.clearSession();
    ApiClient.showToast('You have been logged out successfully.', 'info');
    setTimeout(() => {
      window.location.href = '/login.html';
    }, 1000);
  }
};
