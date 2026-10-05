// ===========================================================================================================
// File: ISystemActivityRepository.cs
// Project: Smart Solar Microgrid Trading System
// Module: M4 – Platform Administration & System Operations
// Section Owned: M4 – Platform Administration & System Operations
// Author: S. Sriramana (IT23136724)
// Description: Repository interface defining data access contracts for SystemActivity.
// ===========================================================================================================
using SmartMicrogrid.API.Models.M4;

namespace SmartMicrogrid.API.Repositories.Interfaces.M4
{
    public interface ISystemActivityRepository
    {
        /// <summary>
        /// Performs record async operation.
        /// </summary>
        Task<SystemActivity> RecordAsync(SystemActivity activity);
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>
        Task<SystemActivity?> GetByIdAsync(string id);
        /// <summary>
        /// Retrieves recent async details.
        /// </summary>
        Task<List<SystemActivity>> GetRecentAsync(int count);
        /// <summary>
        /// Retrieves paged async details.
        /// </summary>
        Task<(List<SystemActivity> Items, long TotalItems)> GetPagedAsync(
            string? userId,
            string? module,
            string? action,
            string? status,
            DateTime? from,
            DateTime? to,
            int page,
            int pageSize);
        /// <summary>
        /// Performs count async operation.
        /// </summary>
        Task<long> CountAsync(
            string? userId = null,
            string? module = null,
            string? action = null,
            string? status = null,
            DateTime? from = null,
            DateTime? to = null);
        /// <summary>
        /// Retrieves action counts async details.
        /// </summary>
        Task<Dictionary<string, long>> GetActionCountsAsync(DateTime? from, DateTime? to);
        /// <summary>
        /// Retrieves module counts async details.
        /// </summary>
        Task<Dictionary<string, long>> GetModuleCountsAsync(DateTime? from, DateTime? to);
    }
}
