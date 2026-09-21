/* ==========================================================================
   Smart Microgrid Energy System - Authentication API Service
   ========================================================================== */

const AuthApi = {
  register(registerData) {
    return ApiClient.post('/auth/register', registerData);
  },

  login(loginData) {
    return ApiClient.post('/auth/login', loginData);
  },

  changePassword(passwordData) {
    return ApiClient.post('/auth/change-password', passwordData);
  }
};
