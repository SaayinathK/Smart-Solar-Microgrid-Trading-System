// ===========================================================================================================
// File: MicrogridDashboardController.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: API Controller exposing REST endpoints for MicrogridDashboard management.
// ===========================================================================================================
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Services.Interfaces;

namespace SmartMicrogrid.API.Controllers
{
    [ApiController]
    [Route("api/microgrid-dashboard")]
    public class MicrogridDashboardController : ControllerBase
    {
        private readonly IMicrogridDashboardService _dashboardService;
        /// <summary>
        /// Initializes a new instance of the MicrogridDashboardController class.
        /// </summary>

        public MicrogridDashboardController(IMicrogridDashboardService dashboardService)
        {
            // Initialize dependencies and state
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Get M1 infrastructure statistics dashboard summary.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            // Execute get dashboard stats operations
            var stats = await _dashboardService.GetDashboardStatsAsync();
            return Ok(ApiResponse<object>.SuccessResponse(stats, "Infrastructure dashboard statistics retrieved successfully."));
        }
    }
}
