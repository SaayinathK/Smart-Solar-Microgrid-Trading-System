// ===========================================================================================================
// File: MicrogridDashboardController.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: API Controller exposing REST endpoints for MicrogridDashboard management.
// ===========================================================================================================
using System.Security.Claims;
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
        public async Task<IActionResult> GetDashboardStats([FromQuery] string? operatorId = null)
        {
            // Execute get dashboard stats operations
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var isOperator = User.IsInRole("MicrogridOperator");
            var effectiveOperatorId = isOperator ? currentUserId : operatorId;

            var stats = await _dashboardService.GetDashboardStatsAsync(effectiveOperatorId);
            return Ok(ApiResponse<object>.SuccessResponse(stats, "Infrastructure dashboard statistics retrieved successfully."));
        }
    }
}
