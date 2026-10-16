// ===========================================================================================================
// File: BatteryController.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: API Controller exposing REST endpoints for Battery management.
// ===========================================================================================================
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Services.Interfaces;

namespace SmartMicrogrid.API.Controllers
{
    [ApiController]
    [Route("api/microgrids/{id}/battery")]
    public class BatteryController : ControllerBase
    {
        private readonly IBatteryService _batteryService;
        /// <summary>
        /// Initializes a new instance of the BatteryController class.
        /// </summary>

        public BatteryController(IBatteryService batteryService)
        {
            // Initialize dependencies and state
            _batteryService = batteryService;
        }

        /// <summary>
        /// Get battery storage status for a microgrid
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetBattery(string id)
        {
            // Execute get battery operations
            var battery = await _batteryService.GetBatteryAsync(id);
            if (battery == null)
            {
                return NotFound(ApiResponse<object>.FailureResponse("Microgrid not found."));
            }
            return Ok(ApiResponse<object>.SuccessResponse(battery, "Battery storage status retrieved."));
        }

        /// <summary>
        /// Update battery level / capacity for a microgrid
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "MicrogridOperator,Admin")]
        public async Task<IActionResult> UpdateBattery(string id, [FromBody] UpdateBatteryDto dto)
        {
            // Execute update battery operations
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.FailureResponse("Invalid battery payload."));
            }

            try
            {
                var updated = await _batteryService.UpdateBatteryAsync(id, dto);
                if (updated == null)
                {
                    return NotFound(ApiResponse<object>.FailureResponse("Microgrid not found."));
                }
                return Ok(ApiResponse<object>.SuccessResponse(updated, "Battery status updated successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(ex.Message));
            }
        }
    }
}
