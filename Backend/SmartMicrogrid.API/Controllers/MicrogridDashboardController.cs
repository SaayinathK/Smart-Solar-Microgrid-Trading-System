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

        public MicrogridDashboardController(IMicrogridDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Get M1 infrastructure statistics dashboard summary.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = await _dashboardService.GetDashboardStatsAsync();
            return Ok(ApiResponse<object>.SuccessResponse(stats, "Infrastructure dashboard statistics retrieved successfully."));
        }
    }
}
