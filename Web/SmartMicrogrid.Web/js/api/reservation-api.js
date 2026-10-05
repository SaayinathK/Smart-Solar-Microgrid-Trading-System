/* M2 reservation endpoints. The API remains the source of truth for lifecycle rules. */
const ReservationApi = {
  list(params = {}) { return ApiClient.get('/reservations', params); },
  summary() { return ApiClient.get('/reservations/summary'); },
  get(id) { return ApiClient.get(`/reservations/${encodeURIComponent(id)}`); },
  create(data) { return ApiClient.post('/reservations', data); },
  update(id, data) { return ApiClient.put(`/reservations/${encodeURIComponent(id)}`, data); },
  approve(id) { return ApiClient.patch(`/reservations/${encodeURIComponent(id)}/approve`, {}); },
  reject(id, reason) { return ApiClient.patch(`/reservations/${encodeURIComponent(id)}/reject`, { reason }); },
  cancel(id) { return ApiClient.patch(`/reservations/${encodeURIComponent(id)}/cancel`, {}); },
  complete(id) { return ApiClient.patch(`/reservations/${encodeURIComponent(id)}/complete`, {}); }
};
