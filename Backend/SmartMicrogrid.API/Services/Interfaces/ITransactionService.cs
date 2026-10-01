using SmartMicrogrid.API.DTOs.Transactions;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface ITransactionService
    {
        // Create transaction
        Task<TransactionResponse> CreateAsync(
            CreateTransactionRequest request,
            string currentUserId,
            string? operatorId = null);


        // Get all transactions
        Task<List<TransactionResponse>> GetAllAsync(
            string currentUserId,
            string currentRole);


        // Get transaction by ID
        Task<TransactionResponse?> GetByIdAsync(
            string transactionId,
            string currentUserId,
            string currentRole);


        // Generate QR
        Task<GenerateQrResponse?> GenerateQrAsync(
            string transactionId,
            string currentUserId);


        // Verify transaction
        Task<TransactionResponse?> VerifyAsync(
            string transactionId,
            VerifyTransactionRequest request,
            string currentUserId,
            string? operatorId = null);


        // Complete transaction
        Task<TransactionResponse?> CompleteAsync(
            string transactionId,
            CompleteTransactionRequest request,
            string currentUserId);


        // Update transaction status
        Task<TransactionResponse?> UpdateStatusAsync(
            string transactionId,
            string status,
            string currentUserId);
    }
}