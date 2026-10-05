// ===========================================================================================================
// File: ITransactionService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M3 – Transaction & Verification Management
// Section Owned: M3 – Transaction & Verification Management
// Author: J. Jathusan (IT23297418)
// Description: Service interface defining contract for Transaction operations.
// ===========================================================================================================
using SmartMicrogrid.API.DTOs.Transactions;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface ITransactionService
    {
        /// <summary>
        /// Creates or registers a new async record.
        /// </summary>
        // Create transaction
        Task<TransactionResponse> CreateAsync(
            CreateTransactionRequest request,
            string currentUserId,
            string? operatorId = null);
        /// <summary>
        /// Retrieves all async details.
        /// </summary>


        // Get all transactions
        Task<List<TransactionResponse>> GetAllAsync(
            string currentUserId,
            string currentRole);
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>


        // Get transaction by ID
        Task<TransactionResponse?> GetByIdAsync(
            string transactionId,
            string currentUserId,
            string currentRole);
        /// <summary>
        /// Performs generate qr async operation.
        /// </summary>


        // Generate QR
        Task<GenerateQrResponse?> GenerateQrAsync(
            string transactionId,
            string currentUserId);
        /// <summary>
        /// Verifies and validates async criteria.
        /// </summary>


        // Verify transaction
        Task<TransactionResponse?> VerifyAsync(
            string transactionId,
            VerifyTransactionRequest request,
            string currentUserId,
            string? operatorId = null);
        /// <summary>
        /// Performs complete async operation.
        /// </summary>


        // Complete transaction
        Task<TransactionResponse?> CompleteAsync(
            string transactionId,
            CompleteTransactionRequest request,
            string currentUserId);
        /// <summary>
        /// Updates the specified status async record.
        /// </summary>


        // Update transaction status
        Task<TransactionResponse?> UpdateStatusAsync(
            string transactionId,
            string status,
            string currentUserId);
    }
}