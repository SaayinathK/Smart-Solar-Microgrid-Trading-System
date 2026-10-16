// ===========================================================================================================
// File: IMicrogridRepository.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Repository interface defining data access contracts for Microgrid.
// ===========================================================================================================
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartMicrogrid.API.Models.M1;

namespace SmartMicrogrid.API.Repositories.Interfaces
{
    public interface IMicrogridRepository
    {
        /// <summary>
        /// Retrieves all async details.
        /// </summary>
        Task<IEnumerable<MicrogridNode>> GetAllAsync(string? status = null, bool? isActive = null, string? location = null, string? search = null);
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>
        Task<MicrogridNode?> GetByIdAsync(string id);
        /// <summary>
        /// Creates or registers a new async record.
        /// </summary>
        Task<MicrogridNode> CreateAsync(MicrogridNode microgrid);
        /// <summary>
        /// Updates the specified async record.
        /// </summary>
        Task<bool> UpdateAsync(string id, MicrogridNode microgrid);
        /// <summary>
        /// Deletes or removes the designated async record.
        /// </summary>
        Task<bool> DeleteAsync(string id);
        /// <summary>
        /// Updates the specified status async record.
        /// </summary>
        Task<bool> UpdateStatusAsync(string id, string status, bool isActive);
        /// <summary>
        /// Updates the specified capacity async record.
        /// </summary>
        Task<bool> UpdateCapacityAsync(string id, double totalCapacity, double availableCapacity, double reservedCapacity, double usedCapacity);
        /// <summary>
        /// Updates the specified battery async record.
        /// </summary>
        Task<bool> UpdateBatteryAsync(string id, double batteryCapacity, double currentBatteryLevel, double batteryPercentage, string batteryStatus);
        /// <summary>
        /// Retrieves count async details.
        /// </summary>
        Task<long> GetCountAsync(string? status = null);
    }
}
