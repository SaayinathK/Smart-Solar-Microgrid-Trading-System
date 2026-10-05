// ===========================================================================================================
// File: IUserRepository.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Repository interface defining data access contracts for User.
// ===========================================================================================================
using SmartMicrogrid.API.Models.Common;

namespace SmartMicrogrid.API.Repositories.Interfaces
{
    public interface IUserRepository
    {
        /// <summary>
        /// Retrieves all async details.
        /// </summary>
        Task<IEnumerable<User>> GetAllAsync(string? searchTerm = null, Role? roleFilter = null, bool? activeOnly = null, AccountStatus? accountStatus = null);
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>
        Task<User?> GetByIdAsync(string id);
        /// <summary>
        /// Retrieves by email async details.
        /// </summary>
        Task<User?> GetByEmailAsync(string email);
        /// <summary>
        /// Creates or registers a new async record.
        /// </summary>
        Task<User> CreateAsync(User user);
        /// <summary>
        /// Updates the specified async record.
        /// </summary>
        Task<bool> UpdateAsync(User user);
        /// <summary>
        /// Deletes or removes the designated async record.
        /// </summary>
        Task<bool> DeleteAsync(string id);
        /// <summary>
        /// Performs exists by email async operation.
        /// </summary>
        Task<bool> ExistsByEmailAsync(string email, string? excludeUserId = null);
        /// <summary>
        /// Performs exists by nic async operation.
        /// </summary>
        Task<bool> ExistsByNicAsync(string nic, string? excludeUserId = null);
        /// <summary>
        /// Retrieves by nic async details.
        /// </summary>
        Task<User?> GetByNicAsync(string nic);
    }
}
