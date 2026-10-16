// ===========================================================================================================
// File: ITransactionRepository.cs
// Project: Smart Solar Microgrid Trading System
// Module: M3 – Transaction & Verification Management
// Section Owned: M3 – Transaction & Verification Management
// Author: J. Jathusan (IT23297418)
// Description: Repository interface defining data access contracts for Transaction.
// ===========================================================================================================
using SmartMicrogrid.API.Models.Transactions;

namespace SmartMicrogrid.API.Repositories.Interfaces
{
    public interface ITransactionRepository
    {
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>
        Task<Transaction?> GetByIdAsync(string id);
        /// <summary>
        /// Retrieves by reservation id async details.
        /// </summary>

        Task<Transaction?> GetByReservationIdAsync(string reservationId);
        /// <summary>
        /// Retrieves by qr code data async details.
        /// </summary>

        Task<Transaction?> GetByQrCodeDataAsync(string qrCodeData);
        /// <summary>
        /// Retrieves all async details.
        /// </summary>

        Task<List<Transaction>> GetAllAsync();
        /// <summary>
        /// Retrieves by prosumer id async details.
        /// </summary>

        Task<List<Transaction>> GetByProsumerIdAsync(string prosumerId);
        /// <summary>
        /// Retrieves by verifier id async details.
        /// </summary>

        Task<List<Transaction>> GetByVerifierIdAsync(string verifierId);
        /// <summary>
        /// Creates or registers a new async record.
        /// </summary>

        Task<Transaction> CreateAsync(Transaction transaction);
        /// <summary>
        /// Updates the specified async record.
        /// </summary>

        Task<Transaction?> UpdateAsync(Transaction transaction);
    }
}