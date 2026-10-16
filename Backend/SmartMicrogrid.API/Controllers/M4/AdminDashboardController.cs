// ===========================================================================================================
// File: AdminDashboardController.cs
// Project: Smart Solar Microgrid Trading System
// Module: M4 – Platform Administration & System Operations
// Section Owned: M4 – Platform Administration & System Operations
// Author: S. Sriramana (IT23136724)
// Description: API Controller exposing REST endpoints for AdminDashboard management.
// ===========================================================================================================
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMicrogrid.API.DTOs.M4;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Services.Interfaces.M4;

namespace SmartMicrogrid.API.Controllers.M4
{
    /// <summary>
    /// M4 - Platform Administration dashboard.
    /// </summary>
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        /// <summary>
        /// Initializes a new instance of the AdminDashboardController class.
        /// </summary>

        public AdminDashboardController(IDashboardService dashboardService)
        {
            // Initialize dependencies and state
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Aggregate platform statistics: user counts, role distribution,
        /// read-only M1/M2/M3 overview, recent activity and system health.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<AdminDashboardDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDashboard()
        {
            // Execute get dashboard operations
            var dashboard = await _dashboardService.BuildAsync();
            return Ok(ApiResponse<AdminDashboardDto>.SuccessResponse(
                dashboard,
                "Dashboard retrieved successfully."));
        }
    }
}
