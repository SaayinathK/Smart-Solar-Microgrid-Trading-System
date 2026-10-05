// ===========================================================================================================
// File: SystemController.cs
// Project: Smart Solar Microgrid Trading System
// Module: M4 – Platform Administration & System Operations
// Section Owned: M4 – Platform Administration & System Operations
// Author: S. Sriramana (IT23136724)
// Description: API Controller exposing REST endpoints for System management.
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
    /// M4 - System health and platform configuration.
    /// </summary>
    [ApiController]
    [Route("api/admin/system")]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public class SystemController : ControllerBase
    {
        private readonly ISystemHealthService _healthService;
        private readonly ISystemConfigurationService _configurationService;
        private readonly IAuditService _auditService;
        /// <summary>
        /// Initializes a new instance of the SystemController class.
        /// </summary>

        public SystemController(
            ISystemHealthService healthService,
            ISystemConfigurationService configurationService,
            IAuditService auditService)
        {
            // Initialize dependencies and state
            _healthService = healthService;
            _configurationService = configurationService;
            _auditService = auditService;
        }

        /// <summary>
        /// Live health of the API, MongoDB, authentication and server.
        /// The database status comes from an actual ping command.
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(typeof(ApiResponse<SystemHealthDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetHealth()
        {
            // Execute get health operations
            var health = await _healthService.GetHealthAsync();
            return Ok(ApiResponse<SystemHealthDto>.SuccessResponse(
                health,
                "System health retrieved successfully."));
        }

        /// <summary>
        /// Current platform configuration.
        /// </summary>
        [HttpGet("configuration")]
        [ProducesResponseType(typeof(ApiResponse<ConfigurationResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetConfiguration()
        {
            // Execute get configuration operations
            var configuration = await _configurationService.GetAsync();
            return Ok(ApiResponse<ConfigurationResponseDto>.SuccessResponse(
                configuration,
                "Configuration retrieved successfully."));
        }

        /// <summary>
        /// Update platform configuration. Records an audit entry.
        /// </summary>
        [HttpPut("configuration")]
        [ProducesResponseType(typeof(ApiResponse<ConfigurationResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateConfiguration([FromBody] UpdateConfigurationDto dto)
        {
            // Execute update configuration operations
            if (!ModelState.IsValid)
            {
                return BadRequest(ValidationFailure());
            }

            var updatedBy = User.Identity?.Name ?? "Admin";
            var configuration = await _configurationService.UpdateAsync(dto, updatedBy);

            return Ok(ApiResponse<ConfigurationResponseDto>.SuccessResponse(
                configuration,
                "Configuration updated successfully."));
        }

        /// <summary>
        /// Toggle maintenance mode.
        /// </summary>
        [HttpPatch("configuration/maintenance")]
        [ProducesResponseType(typeof(ApiResponse<ConfigurationResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateMaintenanceMode([FromBody] MaintenanceModeDto dto)
        {
            // Execute update maintenance mode operations
            if (!ModelState.IsValid)
            {
                return BadRequest(ValidationFailure());
            }

            var updatedBy = User.Identity?.Name ?? "Admin";
            var configuration = await _configurationService.SetMaintenanceModeAsync(dto, updatedBy);

            return Ok(ApiResponse<ConfigurationResponseDto>.SuccessResponse(
                configuration,
                configuration.MaintenanceMode
                    ? "Maintenance mode enabled."
                    : "Maintenance mode disabled."));
        }

        /// <summary>
        /// Toggle public self-registration.
        /// </summary>
        [HttpPatch("configuration/registration")]
        [ProducesResponseType(typeof(ApiResponse<ConfigurationResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateRegistrationMode([FromBody] RegistrationModeDto dto)
        {
            // Execute update registration mode operations
            if (!ModelState.IsValid)
            {
                return BadRequest(ValidationFailure());
            }

            var updatedBy = User.Identity?.Name ?? "Admin";
            var configuration = await _configurationService.SetRegistrationModeAsync(dto, updatedBy);

            return Ok(ApiResponse<ConfigurationResponseDto>.SuccessResponse(
                configuration,
                configuration.AllowRegistration
                    ? "Public registration enabled."
                    : "Public registration disabled."));
        }
        /// <summary>
        /// Reseeds all database collections with interrelated test datasets across all models.
        /// </summary>
        [HttpPost("reseed")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ReseedDatabase([FromServices] SmartMicrogrid.API.Data.MongoDbContext context)
        {
            // Execute database reseed operation
            await SmartMicrogrid.API.Data.DbSeeder.ReseedAllAsync(context);
            return Ok(ApiResponse<string>.SuccessResponse(
                "Database successfully wiped and reseeded with interrelated datasets for all models.",
                "Database reseeded successfully."));
        }

        /// <summary>
        /// Performs validation failure operation.
        /// </summary>
        private ApiResponse<object> ValidationFailure()
        {
            // Execute validation failure operations
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return ApiResponse<object>.FailureResponse("Validation failed.", errors);
        }
    }
}
