using SmartMicrogrid.API.Models.Transactions;

namespace SmartMicrogrid.API.Repositories.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetByIdAsync(string id);

        Task<Transaction?> GetByReservationIdAsync(string reservationId);

        Task<Transaction?> GetByQrCodeDataAsync(string qrCodeData);

        Task<List<Transaction>> GetAllAsync();

        Task<List<Transaction>> GetByProsumerIdAsync(string prosumerId);

        Task<List<Transaction>> GetByVerifierIdAsync(string verifierId);

        Task<Transaction> CreateAsync(Transaction transaction);

        Task<Transaction?> UpdateAsync(Transaction transaction);
    }
}