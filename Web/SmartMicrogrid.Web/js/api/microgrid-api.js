/* ==========================================================================
   Smart Microgrid Energy System - Component 1 API Client
   ========================================================================== */

const MicrogridApi = {
  async reverseGeocode(latitude, longitude) {
    return ApiClient.get('/geocoding/reverse', { latitude, longitude });
  },
  async geocodeAddress(address) {
    return ApiClient.get('/geocoding', { address });
  },

  // Microgrid Node APIs
  async getMicrogrids(params = {}) {
    return ApiClient.get('/microgrids', params);
  },

  async getMicrogridById(id) {
    return ApiClient.get(`/microgrids/${id}`);
  },

  async createMicrogrid(data) {
    return ApiClient.post('/microgrids', data);
  },

  async updateMicrogrid(id, data) {
    return ApiClient.put(`/microgrids/${id}`, data);
  },

  async deleteMicrogrid(id) {
    return ApiClient.delete(`/microgrids/${id}`);
  },

  async updateStatus(id, status) {
    return ApiClient.patch(`/microgrids/${id}/status`, { status });
  },

  // Capacity APIs
  async getCapacity(microgridId) {
    return ApiClient.get(`/microgrids/${microgridId}/capacity`);
  },

  async updateCapacity(microgridId, data) {
    return ApiClient.put(`/microgrids/${microgridId}/capacity`, data);
  },

  // Battery APIs
  async getBattery(microgridId) {
    return ApiClient.get(`/microgrids/${microgridId}/battery`);
  },

  async updateBattery(microgridId, data) {
    return ApiClient.put(`/microgrids/${microgridId}/battery`, data);
  },

  // Energy Slot APIs
  async getEnergySlots(params = {}) {
    return ApiClient.get('/energy-slots', params);
  },

  async getEnergySlotById(id) {
    return ApiClient.get(`/energy-slots/${id}`);
  },

  async createEnergySlot(data) {
    return ApiClient.post('/energy-slots', data);
  },

  async updateEnergySlot(id, data) {
    return ApiClient.put(`/energy-slots/${id}`, data);
  },

  async deleteEnergySlot(id) {
    return ApiClient.delete(`/energy-slots/${id}`);
  },

  async updateSlotStatus(id, status) {
    return ApiClient.patch(`/energy-slots/${id}/status`, { status });
  },

  // Energy Availability API
  async getEnergyAvailability(params = {}) {
    return ApiClient.get('/energy-availability', params);
  },

  // Infrastructure Dashboard API
  async getDashboardStats() {
    return ApiClient.get('/microgrid-dashboard');
  }
};
