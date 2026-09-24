using MongoDB.Driver;
using SmartMicrogrid.API.Data;
using SmartMicrogrid.API.Models.Transactions;
using SmartMicrogrid.API.Repositories.Interfaces;

namespace SmartMicrogrid.API.Repositories.Implementation
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly IMongoCollection<Transaction> _transactions;

        public TransactionRepository(MongoDbContext mongoDbContext)
        {
            _transactions = mongoDbContext.Transactions;
        }

        public async Task<Transaction?> GetByIdAsync(string id)
        {
            return await _transactions
                .Find(x => x.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<Transaction?> GetByReservationIdAsync(
            string reservationId)
        {
            return await _transactions
                .Find(x => x.ReservationId == reservationId)
                .FirstOrDefaultAsync();
        }

        public async Task<Transaction?> GetByQrCodeDataAsync(
            string qrCodeData)
        {
            return await _transactions
                .Find(x => x.QrCodeData == qrCodeData)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Transaction>> GetAllAsync()
        {
            return await _transactions
                .Find(_ => true)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetByProsumerIdAsync(
            string prosumerId)
        {
            return await _transactions
                .Find(x => x.ProsumerId == prosumerId)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetByVerifierIdAsync(
            string verifierId)
        {
            return await _transactions
                .Find(x => x.VerifiedBy == verifierId)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<Transaction> CreateAsync(
            Transaction transaction)
        {
            await _transactions.InsertOneAsync(transaction);

            return transaction;
        }

        public async Task<Transaction?> UpdateAsync(
            Transaction transaction)
        {
            var result = await _transactions.ReplaceOneAsync(
                x => x.Id == transaction.Id,
                transaction);

            if (!result.IsAcknowledged ||
                result.MatchedCount == 0)
            {
                return null;
            }

            return transaction;
        }
    }
}
