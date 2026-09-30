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

        public AdminDashboardController(IDashboardService dashboardService)
        {
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
            var dashboard = await _dashboardService.BuildAsync();
            return Ok(ApiResponse<AdminDashboardDto>.SuccessResponse(
                dashboard,
                "Dashboard retrieved successfully."));
        }
    }
}
