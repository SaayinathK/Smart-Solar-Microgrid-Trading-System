// ===========================================================================================================
// File: EnergySlotController.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: API Controller exposing REST endpoints for EnergySlot management.
// ===========================================================================================================
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Services.Interfaces;

namespace SmartMicrogrid.API.Controllers
{
    [ApiController]
    [Route("api/energy-slots")]
    public class EnergySlotController : ControllerBase
    {
        private readonly IEnergySlotService _slotService;
        /// <summary>
        /// Initializes a new instance of the EnergySlotController class.
        /// </summary>

        public EnergySlotController(IEnergySlotService slotService)
        {
            // Initialize dependencies and state
            _slotService = slotService;
        }

        /// <summary>
        /// Get all energy slots with optional filters
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? microgridId, [FromQuery] string? status, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] double? minEnergy)
        {
            // Execute get all operations
            var data = await _slotService.GetAllAsync(microgridId, status, startTime, endTime, minEnergy);
            return Ok(ApiResponse<object>.SuccessResponse(data, "Energy slots retrieved successfully."));
        }

        /// <summary>
        /// Get energy slot details by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            // Execute get by id operations
            var slot = await _slotService.GetByIdAsync(id);
            if (slot == null)
            {
                return NotFound(ApiResponse<object>.FailureResponse("Energy slot not found."));
            }
            return Ok(ApiResponse<object>.SuccessResponse(slot, "Energy slot retrieved."));
        }

        /// <summary>
        /// Create a new energy slot
        /// Authorized Roles: MicrogridOperator, Admin
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "MicrogridOperator,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateEnergySlotDto dto)
        {
            // Execute create operations
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.FailureResponse("Invalid energy slot payload."));
            }

            try
            {
                var createdBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Operator-System";
                var created = await _slotService.CreateAsync(dto, createdBy);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<object>.SuccessResponse(created, "Energy slot published successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(ex.Message));
            }
        }

        /// <summary>
        /// Update an energy slot
        /// Authorized Roles: MicrogridOperator, Admin
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "MicrogridOperator,Admin")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateEnergySlotDto dto)
        {
            // Execute update operations
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.FailureResponse("Invalid energy slot payload."));
            }

            try
            {
                var updated = await _slotService.UpdateAsync(id, dto);
                if (updated == null)
                {
                    return NotFound(ApiResponse<object>.FailureResponse("Energy slot not found."));
                }
                return Ok(ApiResponse<object>.SuccessResponse(updated, "Energy slot updated successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(ex.Message));
            }
        }

        /// <summary>
        /// Delete an energy slot
        /// Authorized Roles: MicrogridOperator, Admin
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "MicrogridOperator,Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            // Execute delete operations
            var deleted = await _slotService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(ApiResponse<object>.FailureResponse("Energy slot not found or could not be deleted."));
            }
            return Ok(ApiResponse<object>.SuccessResponse(null, "Energy slot deleted successfully."));
        }

        /// <summary>
        /// Update status of an energy slot
        /// Authorized Roles: MicrogridOperator, Admin
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "MicrogridOperator,Admin")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] SlotStatusUpdateDto dto)
        {
            // Execute update status operations
            if (string.IsNullOrWhiteSpace(dto.Status))
            {
                return BadRequest(ApiResponse<object>.FailureResponse("Status is required."));
            }

            try
            {
                var updated = await _slotService.UpdateStatusAsync(id, dto.Status);
                if (!updated)
                {
                    return NotFound(ApiResponse<object>.FailureResponse("Energy slot not found."));
                }
                return Ok(ApiResponse<object>.SuccessResponse(null, $"Energy slot status updated to '{dto.Status}'."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(ex.Message));
            }
        }
    }
}
