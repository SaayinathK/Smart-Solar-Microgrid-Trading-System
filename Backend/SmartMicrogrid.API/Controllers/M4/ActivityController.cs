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
    /// M4 - Audit trail of administrative and authentication events.
    /// </summary>
    [ApiController]
    [Route("api/admin/activity")]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public class ActivityController : ControllerBase
    {
        private readonly IActivityService _activityService;

        public ActivityController(IActivityService activityService)
        {
            _activityService = activityService;
        }

        /// <summary>
        /// Paginated audit records. Filter by userId, module, action, status or date range.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResultDto<SystemActivityDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetActivity([FromQuery] ActivityQueryDto query)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.FailureResponse("Validation failed.", errors));
            }

            try
            {
                var result = await _activityService.GetPagedAsync(query);
                return Ok(ApiResponse<PagedResultDto<SystemActivityDto>>.SuccessResponse(
                    result,
                    "Activity records retrieved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(ex.Message));
            }
        }

        /// <summary>
        /// A single audit record by id.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<SystemActivityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetActivityById(string id)
        {
            var activity = await _activityService.GetByIdAsync(id);

            if (activity == null)
            {
                return NotFound(ApiResponse<object>.FailureResponse("Activity record not found."));
            }

            return Ok(ApiResponse<SystemActivityDto>.SuccessResponse(
                activity,
                "Activity record retrieved successfully."));
        }
    }
}
