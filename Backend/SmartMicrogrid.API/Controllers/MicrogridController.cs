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
    [Route("api/microgrids")]
    public class MicrogridController : ControllerBase
    {
        private readonly IMicrogridService _service;

        public MicrogridController(IMicrogridService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get all microgrids with optional filters
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] bool? isActive, [FromQuery] string? location, [FromQuery] string? search)
        {
            var data = await _service.GetAllAsync(status, isActive, location, search);
            return Ok(ApiResponse<object>.SuccessResponse(data, "Microgrids retrieved successfully."));
        }

        /// <summary>
        /// Get microgrid details by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                return NotFound(ApiResponse<object>.FailureResponse("Microgrid not found."));
            }
            return Ok(ApiResponse<object>.SuccessResponse(data, "Microgrid details retrieved."));
        }

        /// <summary>
        /// Create a new microgrid
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "MicrogridOperator,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateMicrogridDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.FailureResponse("Invalid payload data."));
            }

            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Operator-System";
                var created = await _service.CreateAsync(dto, currentUserId);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<object>.SuccessResponse(created, "Microgrid created successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(ex.Message));
            }
        }

        /// <summary>
        /// Update an existing microgrid
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "MicrogridOperator,Admin")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateMicrogridDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.FailureResponse("Invalid payload data."));
            }

            try
            {
                var updated = await _service.UpdateAsync(id, dto);
                if (updated == null)
                {
                    return NotFound(ApiResponse<object>.FailureResponse("Microgrid not found."));
                }
                return Ok(ApiResponse<object>.SuccessResponse(updated, "Microgrid updated successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(ex.Message));
            }
        }

        /// <summary>
        /// Delete a microgrid by ID
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "MicrogridOperator,Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(ApiResponse<object>.FailureResponse("Microgrid not found or could not be deleted."));
            }
            return Ok(ApiResponse<object>.SuccessResponse(null, "Microgrid deleted successfully."));
        }

        /// <summary>
        /// Update operational status of a microgrid
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "MicrogridOperator,Admin")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] StatusUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Status))
            {
                return BadRequest(ApiResponse<object>.FailureResponse("Status is required."));
            }

            try
            {
                var updated = await _service.UpdateStatusAsync(id, dto.Status);
                if (!updated)
                {
                    return NotFound(ApiResponse<object>.FailureResponse("Microgrid not found."));
                }
                return Ok(ApiResponse<object>.SuccessResponse(null, $"Microgrid status updated to '{dto.Status}'."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(ex.Message));
            }
        }
    }
}
