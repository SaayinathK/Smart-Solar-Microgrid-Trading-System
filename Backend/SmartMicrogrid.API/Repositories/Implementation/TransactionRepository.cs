// ===========================================================================================================
// File: TransactionRepository.cs
// Project: Smart Solar Microgrid Trading System
// Module: M3 – Transaction & Verification Management
// Section Owned: M3 – Transaction & Verification Management
// Author: J. Jathusan (IT23297418)
// Description: Repository implementation handling MongoDB operations for Transaction.
// ===========================================================================================================
using MongoDB.Driver;
using SmartMicrogrid.API.Data;
using SmartMicrogrid.API.Models.Transactions;
using SmartMicrogrid.API.Repositories.Interfaces;

namespace SmartMicrogrid.API.Repositories.Implementation
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly IMongoCollection<Transaction> _transactions;
        /// <summary>
        /// Initializes a new instance of the TransactionRepository class.
        /// </summary>

        public TransactionRepository(MongoDbContext mongoDbContext)
        {
            // Initialize dependencies and state
            _transactions = mongoDbContext.Transactions;
        }
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>

        public async Task<Transaction?> GetByIdAsync(string id)
        {
            // Execute get by id async operations
            return await _transactions
                .Find(x => x.Id == id)
                .FirstOrDefaultAsync();
        }
        /// <summary>
        /// Retrieves by reservation id async details.
        /// </summary>

        public async Task<Transaction?> GetByReservationIdAsync(
            string reservationId)
        {
            // Execute get by reservation id async operations
            return await _transactions
                .Find(x => x.ReservationId == reservationId)
                .FirstOrDefaultAsync();
        }
        /// <summary>
        /// Retrieves by qr code data async details.
        /// </summary>

        public async Task<Transaction?> GetByQrCodeDataAsync(
            string qrCodeData)
        {
            // Execute get by qr code data async operations
            return await _transactions
                .Find(x => x.QrCodeData == qrCodeData)
                .FirstOrDefaultAsync();
        }
        /// <summary>
        /// Retrieves all async details.
        /// </summary>

        public async Task<List<Transaction>> GetAllAsync()
        {
            // Execute get all async operations
            return await _transactions
                .Find(_ => true)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
        /// <summary>
        /// Retrieves by prosumer id async details.
        /// </summary>

        public async Task<List<Transaction>> GetByProsumerIdAsync(
            string prosumerId)
        {
            // Execute get by prosumer id async operations
            return await _transactions
                .Find(x => x.ProsumerId == prosumerId)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
        /// <summary>
        /// Retrieves by verifier id async details.
        /// </summary>

        public async Task<List<Transaction>> GetByVerifierIdAsync(
            string verifierId)
        {
            // Execute get by verifier id async operations
            return await _transactions
                .Find(x => x.VerifiedBy == verifierId)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
        /// <summary>
        /// Creates or registers a new async record.
        /// </summary>

        public async Task<Transaction> CreateAsync(
            Transaction transaction)
        {
            // Execute create async operations
            await _transactions.InsertOneAsync(transaction);

            return transaction;
        }
        /// <summary>
        /// Updates the specified async record.
        /// </summary>

        public async Task<Transaction?> UpdateAsync(
            Transaction transaction)
        {
            // Execute update async operations
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
