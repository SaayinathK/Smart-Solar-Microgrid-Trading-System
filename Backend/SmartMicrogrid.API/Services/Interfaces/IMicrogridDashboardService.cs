// ===========================================================================================================
// File: IMicrogridDashboardService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Service interface defining contract for MicrogridDashboard operations.
// ===========================================================================================================
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IMicrogridDashboardService
    {
        /// <summary>
        /// Retrieves dashboard stats async details.
        /// </summary>
        Task<MicrogridDashboardDto> GetDashboardStatsAsync();
    }
}
