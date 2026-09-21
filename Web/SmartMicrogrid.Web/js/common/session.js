/* ==========================================================================
   Smart Microgrid Energy System - Session Manager
   ========================================================================== */

const SessionManager = {
  getToken() {
    return localStorage.getItem(API_CONFIG.TOKEN_KEY);
  },

  getUser() {
    const raw = localStorage.getItem(API_CONFIG.USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  },

  setSession(token, user) {
    localStorage.setItem(API_CONFIG.TOKEN_KEY, token);
    localStorage.setItem(API_CONFIG.USER_KEY, JSON.stringify(user));
  },

  updateUser(user) {
    localStorage.setItem(API_CONFIG.USER_KEY, JSON.stringify(user));
  },

  clearSession() {
    localStorage.removeItem(API_CONFIG.TOKEN_KEY);
    localStorage.removeItem(API_CONFIG.USER_KEY);
  },

  isAuthenticated() {
    return !!this.getToken();
  },

  getUserRole() {
    const user = this.getUser();
    return user ? user.role : null;
  },

  isAdmin() {
    return this.getUserRole() === 'Admin';
  }
};
