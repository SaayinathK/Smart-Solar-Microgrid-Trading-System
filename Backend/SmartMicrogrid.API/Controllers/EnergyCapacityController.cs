// ===========================================================================================================
// File: EnergyCapacityController.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: API Controller exposing REST endpoints for EnergyCapacity management.
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
    [Route("api/microgrids/{id}/capacity")]
    public class EnergyCapacityController : ControllerBase
    {
        private readonly IEnergyCapacityService _capacityService;
        /// <summary>
        /// Initializes a new instance of the EnergyCapacityController class.
        /// </summary>

        public EnergyCapacityController(IEnergyCapacityService capacityService)
        {
            // Initialize dependencies and state
            _capacityService = capacityService;
        }

        /// <summary>
        /// Get capacity management details for a microgrid
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCapacity(string id)
        {
            // Execute get capacity operations
            var capacity = await _capacityService.GetCapacityAsync(id);
            if (capacity == null)
            {
                return NotFound(ApiResponse<object>.FailureResponse("Microgrid not found."));
            }
            return Ok(ApiResponse<object>.SuccessResponse(capacity, "Microgrid capacity details retrieved."));
        }

        /// <summary>
        /// Update capacity allocation for a microgrid
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "MicrogridOperator,Admin")]
        public async Task<IActionResult> UpdateCapacity(string id, [FromBody] UpdateCapacityDto dto)
        {
            // Execute update capacity operations
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.FailureResponse("Invalid capacity payload."));
            }

            try
            {
                var updated = await _capacityService.UpdateCapacityAsync(id, dto);
                if (updated == null)
                {
                    return NotFound(ApiResponse<object>.FailureResponse("Microgrid not found."));
                }
                return Ok(ApiResponse<object>.SuccessResponse(updated, "Microgrid capacity updated successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(ex.Message));
            }
        }
    }
}
