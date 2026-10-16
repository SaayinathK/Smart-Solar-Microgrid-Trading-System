// ===========================================================================================================
// File: EnergyAvailabilityController.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: API Controller exposing REST endpoints for EnergyAvailability management.
// ===========================================================================================================
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Services.Interfaces;

namespace SmartMicrogrid.API.Controllers
{
    [ApiController]
    [Route("api/energy-availability")]
    public class EnergyAvailabilityController : ControllerBase
    {
        private readonly IEnergySlotService _slotService;
        /// <summary>
        /// Initializes a new instance of the EnergyAvailabilityController class.
        /// </summary>

        public EnergyAvailabilityController(IEnergySlotService slotService)
        {
            // Initialize dependencies and state
            _slotService = slotService;
        }

        /// <summary>
        /// Retrieve currently available energy slots for trading & reservations.
        /// Integration point for Component 2 (Marketplace).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEnergyAvailability([FromQuery] EnergyAvailabilityQueryDto query)
        {
            // Execute get energy availability operations
            var data = await _slotService.GetEnergyAvailabilityAsync(query);
            return Ok(ApiResponse<object>.SuccessResponse(data, "Available energy slots retrieved successfully."));
        }
    }
}
