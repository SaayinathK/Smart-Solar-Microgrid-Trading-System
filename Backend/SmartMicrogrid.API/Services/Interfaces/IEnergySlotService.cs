// ===========================================================================================================
// File: IEnergySlotService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Service interface defining contract for EnergySlot operations.
// ===========================================================================================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IEnergySlotService
    {
        /// <summary>
        /// Retrieves all async details.
        /// </summary>
        Task<IEnumerable<EnergySlotResponseDto>> GetAllAsync(string? microgridId = null, string? status = null, DateTime? startTime = null, DateTime? endTime = null, double? minEnergy = null);
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>
        Task<EnergySlotResponseDto?> GetByIdAsync(string id);
        /// <summary>
        /// Creates or registers a new async record.
        /// </summary>
        Task<EnergySlotResponseDto> CreateAsync(CreateEnergySlotDto dto, string createdBy);
        /// <summary>
        /// Updates the specified async record.
        /// </summary>
        Task<EnergySlotResponseDto?> UpdateAsync(string id, UpdateEnergySlotDto dto);
        /// <summary>
        /// Deletes or removes the designated async record.
        /// </summary>
        Task<bool> DeleteAsync(string id);
        /// <summary>
        /// Updates the specified status async record.
        /// </summary>
        Task<bool> UpdateStatusAsync(string id, string status);
        /// <summary>
        /// Retrieves energy availability async details.
        /// </summary>
        Task<IEnumerable<EnergyAvailabilityResponseDto>> GetEnergyAvailabilityAsync(EnergyAvailabilityQueryDto query);
    }
}
