/* ==========================================================================
   Smart Microgrid Energy System - User Management API Service
   ========================================================================== */

const UserApi = {
  getCurrentUser() {
    return ApiClient.get('/users/me');
  },

  updateProfile(profileData) {
    return ApiClient.put('/users/me', profileData);
  },

  deactivateSelf(reason = null) {
    return ApiClient.post('/users/me/deactivate', reason ? { reason } : {});
  },

  requestDeactivation(reason = null) {
    return ApiClient.post('/users/me/request-deactivation', reason ? { reason } : {});
  },

  getAllUsers(params = {}) {
    return ApiClient.get('/users', params);
  },

  getUserById(id) {
    return ApiClient.get(`/users/${id}`);
  },

  getUserByNic(nic) {
    return ApiClient.get(`/users/nic/${encodeURIComponent(nic)}`);
  },

  createUser(userData) {
    return ApiClient.post('/users', userData);
  },

  updateUser(id, userData) {
    return ApiClient.put(`/users/${id}`, userData);
  },

  updateStatus(id, isActive) {
    return ApiClient.patch(`/users/${id}/status`, { isActive });
  },

  updateAccountStatus(id, accountStatus) {
    return ApiClient.patch(`/users/${id}/account-status`, { accountStatus });
  },

  updateRole(id, role) {
    return ApiClient.patch(`/users/${id}/role`, { role });
  },

  deleteUser(id) {
    return ApiClient.delete(`/users/${id}`);
  }
};
