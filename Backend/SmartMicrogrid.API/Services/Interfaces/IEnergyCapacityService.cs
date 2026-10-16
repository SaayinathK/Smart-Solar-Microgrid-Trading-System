// ===========================================================================================================
// File: IEnergyCapacityService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Service interface defining contract for EnergyCapacity operations.
// ===========================================================================================================
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IEnergyCapacityService
    {
        /// <summary>
        /// Retrieves capacity async details.
        /// </summary>
        Task<CapacityResponseDto?> GetCapacityAsync(string microgridId);
        /// <summary>
        /// Updates the specified capacity async record.
        /// </summary>
        Task<CapacityResponseDto?> UpdateCapacityAsync(string microgridId, UpdateCapacityDto dto);
    }
}
