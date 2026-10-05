// ===========================================================================================================
// File: IEnergySlotRepository.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Repository interface defining data access contracts for EnergySlot.
// ===========================================================================================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartMicrogrid.API.Models.M1;

namespace SmartMicrogrid.API.Repositories.Interfaces
{
    public interface IEnergySlotRepository
    {
        /// <summary>
        /// Retrieves all async details.
        /// </summary>
        Task<IEnumerable<EnergySlot>> GetAllAsync(string? microgridId = null, string? status = null, DateTime? startTime = null, DateTime? endTime = null, double? minEnergy = null);
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>
        Task<EnergySlot?> GetByIdAsync(string id);
        /// <summary>
        /// Creates or registers a new async record.
        /// </summary>
        Task<EnergySlot> CreateAsync(EnergySlot slot);
        /// <summary>
        /// Updates the specified async record.
        /// </summary>
        Task<bool> UpdateAsync(string id, EnergySlot slot);
        /// <summary>
        /// Deletes or removes the designated async record.
        /// </summary>
        Task<bool> DeleteAsync(string id);
        /// <summary>
        /// Updates the specified status async record.
        /// </summary>
        Task<bool> UpdateStatusAsync(string id, string status);
        /// <summary>
        /// Retrieves count async details.
        /// </summary>
        Task<long> GetCountAsync(string? status = null);
        /// <summary>
        /// Retrieves total available energy async details.
        /// </summary>
        Task<double> GetTotalAvailableEnergyAsync();
    }
}
