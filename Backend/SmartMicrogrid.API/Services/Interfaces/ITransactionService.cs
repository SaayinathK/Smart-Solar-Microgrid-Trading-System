using SmartMicrogrid.API.DTOs.Transactions;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface ITransactionService
    {
        // ============================================================
        // CREATE TRANSACTION
        // ============================================================
        Task<TransactionResponse> CreateAsync(
            CreateTransactionRequest request,
            string currentUserId);


        // ============================================================
        // GET ALL TRANSACTIONS
        // ============================================================
        Task<List<TransactionResponse>> GetAllAsync(
            string currentUserId,
            string currentRole);


        // ============================================================
        // GET TRANSACTION BY ID
        // ============================================================
        Task<TransactionResponse?> GetByIdAsync(
            string transactionId,
            string currentUserId,
            string currentRole);


        // ============================================================
        // GENERATE QR
        // ============================================================
        Task<GenerateQrResponse?> GenerateQrAsync(
            string transactionId,
            string currentUserId);


        // ============================================================
        // VERIFY TRANSACTION
        // ============================================================
        Task<TransactionResponse?> VerifyAsync(
            string transactionId,
            VerifyTransactionRequest request,
            string currentUserId);


        // ============================================================
        // COMPLETE TRANSACTION
        // ============================================================
        Task<TransactionResponse?> CompleteAsync(
            string transactionId,
            CompleteTransactionRequest request,
            string currentUserId);


        // ============================================================
        // UPDATE TRANSACTION STATUS
        // ============================================================
        Task<TransactionResponse?> UpdateStatusAsync(
            string transactionId,
            string status,
            string currentUserId);
    }
}