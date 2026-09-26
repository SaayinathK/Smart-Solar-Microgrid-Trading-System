/* ==========================================================================
   Smart Microgrid Energy System - Route Guard & Authentication Controller
   ========================================================================== */

const AuthGuard = {
  requireAuth(requiredRole = null) {
    if (!SessionManager.isAuthenticated()) {
      window.location.href = '/login.html';
      return false;
    }

    if (requiredRole && SessionManager.getUserRole() !== requiredRole) {
      ApiClient.showToast('Access denied: You do not have permission to view this page.', 'error');
      setTimeout(() => {
        window.location.href = '/dashboard.html';
      }, 1500);
      return false;
    }

    return true;
  },

  requireRole(requiredRoles) {
    const roles = Array.isArray(requiredRoles) ? requiredRoles : [requiredRoles];
    if (!this.requireAuth()) return false;
    if (!roles.includes(SessionManager.getUserRole())) {
      ApiClient.showToast('Access denied: You do not have permission to view this page.', 'error');
      setTimeout(() => {
        window.location.href = '/dashboard.html';
      }, 1500);
      return false;
    }
    return true;
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
