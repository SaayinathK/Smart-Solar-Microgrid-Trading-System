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

        public EnergyAvailabilityController(IEnergySlotService slotService)
        {
            _slotService = slotService;
        }

        /// <summary>
        /// Retrieve currently available energy slots for trading & reservations.
        /// Integration point for Component 2 (Marketplace).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEnergyAvailability([FromQuery] EnergyAvailabilityQueryDto query)
        {
            var data = await _slotService.GetEnergyAvailabilityAsync(query);
            return Ok(ApiResponse<object>.SuccessResponse(data, "Available energy slots retrieved successfully."));
        }
    }
}
