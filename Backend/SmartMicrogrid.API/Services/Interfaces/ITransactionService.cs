using SmartMicrogrid.API.DTOs.Transactions;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResponse> CreateAsync(
            CreateTransactionRequest request,
            string currentUserId);

        Task<List<TransactionResponse>> GetAllAsync(
            string currentUserId,
            string currentRole);

        Task<TransactionResponse?> GetByIdAsync(
            string transactionId,
            string currentUserId,
            string currentRole);

        Task<GenerateQrResponse?> GenerateQrAsync(
            string transactionId,
            string currentUserId);

        Task<TransactionResponse?> VerifyAsync(
            VerifyTransactionRequest request,
            string currentUserId);

        Task<TransactionResponse?> CompleteAsync(
            string transactionId,
            CompleteTransactionRequest request,
            string currentUserId);

        Task<TransactionResponse?> UpdateStatusAsync(
            string transactionId,
            string status,
            string currentUserId);
    }
}