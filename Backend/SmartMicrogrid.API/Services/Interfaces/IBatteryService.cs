// ===========================================================================================================
// File: IBatteryService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Service interface defining contract for Battery operations.
// ===========================================================================================================
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IBatteryService
    {
        /// <summary>
        /// Retrieves battery async details.
        /// </summary>
        Task<BatteryResponseDto?> GetBatteryAsync(string microgridId);
        /// <summary>
        /// Updates the specified battery async record.
        /// </summary>
        Task<BatteryResponseDto?> UpdateBatteryAsync(string microgridId, UpdateBatteryDto dto);
    }
}
