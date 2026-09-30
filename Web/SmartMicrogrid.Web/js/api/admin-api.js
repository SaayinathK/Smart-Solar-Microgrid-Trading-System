/* ==========================================================================
   Smart Microgrid Energy System - M4 Platform Administration API Service
   ========================================================================== */

const AdminApi = {
  // ── Dashboard ──────────────────────────────────────────────────────────
  getDashboard() {
    return ApiClient.get('/admin/dashboard');
  },

  // ── System health ──────────────────────────────────────────────────────
  getSystemHealth() {
    return ApiClient.get('/admin/system/health');
  },

  // ── Configuration ─────────────────────────────────────────────────────
  getConfiguration() {
    return ApiClient.get('/admin/system/configuration');
  },

  updateConfiguration(config) {
    return ApiClient.put('/admin/system/configuration', config);
  },

  setMaintenanceMode(enabled, message) {
    return ApiClient.patch('/admin/system/configuration/maintenance', {
      maintenanceMode: enabled,
      maintenanceMessage: message || null
    });
  },

  setRegistrationMode(enabled) {
    return ApiClient.patch('/admin/system/configuration/registration', {
      allowRegistration: enabled
    });
  },

  // ── Audit trail ───────────────────────────────────────────────────────
  getActivity(params = {}) {
    return ApiClient.get('/admin/activity', params);
  },

  getActivityById(id) {
    return ApiClient.get(`/admin/activity/${id}`);
  },

  // ── Reports ───────────────────────────────────────────────────────────
  getUserReport(params = {}) {
    return ApiClient.get('/admin/reports/users', params);
  },

  getRoleReport() {
    return ApiClient.get('/admin/reports/roles');
  },

  getActivityReport(params = {}) {
    return ApiClient.get('/admin/reports/activity', params);
  },

  getPlatformReport(params = {}) {
    return ApiClient.get('/admin/reports/platform', params);
  }
};
