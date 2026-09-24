/* ============================================================================
   Smart Microgrid Energy System - Component 3 API Client
   ============================================================================ */

const TransactionApi = {
  async getTransactions() {
    return ApiClient.get('/transactions');
  },

  async getTransaction(id) {
    return ApiClient.get(`/transactions/${id}`);
  },

  async createTransaction(reservationId) {
    return ApiClient.post('/transactions', { reservationId });
  },

  async generateQr(transactionId) {
    return ApiClient.post(`/transactions/${transactionId}/generate-qr`, {});
  },

  async verifyTransaction(transactionId, qrCodeData) {
    return ApiClient.post(`/transactions/${transactionId}/verify`, { qrCodeData });
  },

  async completeTransaction(transactionId) {
    return ApiClient.post(`/transactions/${transactionId}/complete`, {
      confirmation: 'CONFIRMED'
    });
  }
};
