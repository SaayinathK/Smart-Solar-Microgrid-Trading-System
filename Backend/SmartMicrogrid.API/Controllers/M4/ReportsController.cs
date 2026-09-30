using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMicrogrid.API.DTOs.M4;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Services.Interfaces.M4;

namespace SmartMicrogrid.API.Controllers.M4
{
    /// <summary>
    /// M4 - Administrative reports. User and role sections read the shared users
    /// collection; M1/M2/M3 sections are read-only projections supplied by the
    /// component monitor services.
    /// </summary>
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// User summary report. Optionally filtered by role, status and date range.
        /// </summary>
        [HttpGet("users")]
        [ProducesResponseType(typeof(ApiResponse<UserReportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUserReport(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? role,
            [FromQuery] string? status)
        {
            try
            {
                var report = await _reportService.GetUserReportAsync(from, to, role, status);
                return Ok(ApiResponse<UserReportDto>.SuccessResponse(
                    report,
                    "User report generated successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(ex.Message));
            }
        }

        /// <summary>
        /// Role distribution report derived from the shared users collection.
        /// </summary>
        [HttpGet("roles")]
        [ProducesResponseType(typeof(ApiResponse<System.Collections.Generic.List<RoleDistributionDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRoleReport()
        {
            var report = await _reportService.GetRoleReportAsync();
            return Ok(ApiResponse<System.Collections.Generic.List<RoleDistributionDto>>.SuccessResponse(
                report,
                "Role report generated successfully."));
        }

        /// <summary>
        /// Audit activity report for a date range.
        /// </summary>
        [HttpGet("activity")]
        [ProducesResponseType(typeof(ApiResponse<ActivityReportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetActivityReport(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            try
            {
                var report = await _reportService.GetActivityReportAsync(from, to);
                return Ok(ApiResponse<ActivityReportDto>.SuccessResponse(
                    report,
                    "Activity report generated successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(ex.Message));
            }
        }

        /// <summary>
        /// Platform statistics report combining M4 data with read-only M1/M2/M3 figures.
        /// </summary>
        [HttpGet("platform")]
        [ProducesResponseType(typeof(ApiResponse<PlatformReportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPlatformReport(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            try
            {
                var report = await _reportService.GetPlatformReportAsync(from, to);
                return Ok(ApiResponse<PlatformReportDto>.SuccessResponse(
                    report,
                    "Platform report generated successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(ex.Message));
            }
        }
    }
}
