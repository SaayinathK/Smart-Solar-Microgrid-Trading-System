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

  getAllUsers(params = {}) {
    return ApiClient.get('/users', params);
  },

  getUserById(id) {
    return ApiClient.get(`/users/${id}`);
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

  updateRole(id, role) {
    return ApiClient.patch(`/users/${id}/role`, { role });
  },

  deleteUser(id) {
    return ApiClient.delete(`/users/${id}`);
  }
};
