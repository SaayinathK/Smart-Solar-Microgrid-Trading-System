// ===========================================================================================================
// File: IComponentMonitorServices.cs
// Project: Smart Solar Microgrid Trading System
// Module: M4 – Platform Administration & System Operations
// Section Owned: M4 – Platform Administration & System Operations
// Author: S. Sriramana (IT23136724)
// Description: Defines IComponentMonitorServices components for the Smart Microgrid system.
// ===========================================================================================================
using SmartMicrogrid.API.DTOs.M4;

namespace SmartMicrogrid.API.Services.Interfaces.M4
{
    /// <summary>
    /// M4 read-only view of M1 (Microgrid &amp; Energy Resource Management).
    /// The dashboard consumes this instead of IMicrogridRepository directly, so a
    /// future change to M1's storage only touches the implementation, not M4.
    /// </summary>
    public interface IMicrogridMonitorService
    {
        /// <summary>
        /// Retrieves count async details.
        /// </summary>
        Task<ComponentStatDto> GetCountAsync();
        /// <summary>
        /// Retrieves capacity summary async details.
        /// </summary>
        Task<ComponentStatDto> GetCapacitySummaryAsync();
    }

    /// <summary>
    /// M4 read-only view of M2 (Energy Marketplace &amp; Reservation Management).
    /// </summary>
    public interface IReservationMonitorService
    {
        /// <summary>
        /// Retrieves count async details.
        /// </summary>
        Task<ComponentStatDto> GetCountAsync();
        /// <summary>
        /// Retrieves pending count async details.
        /// </summary>
        Task<ComponentStatDto> GetPendingCountAsync();
    }

    /// <summary>
    /// M4 read-only view of M3 (Energy Transaction &amp; Verification Management).
    /// </summary>
    public interface ITransactionMonitorService
    {
        /// <summary>
        /// Retrieves count async details.
        /// </summary>
        Task<ComponentStatDto> GetCountAsync();
        /// <summary>
        /// Retrieves completed count async details.
        /// </summary>
        Task<ComponentStatDto> GetCompletedCountAsync();
    }
}
