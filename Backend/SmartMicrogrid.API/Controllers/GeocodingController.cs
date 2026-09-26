using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Services.Implementation;
using SmartMicrogrid.API.Services.Interfaces;

namespace SmartMicrogrid.API.Controllers;

[ApiController]
[Route("api/geocoding")]
[Authorize(Roles = "MicrogridOperator,Admin")]
public class GeocodingController(IGeocodingService geocoding) : ControllerBase
{
    [HttpGet("reverse")]
    public async Task<IActionResult> GetAddress(
        [FromQuery, Required, Range(-90d, 90d)] double? latitude,
        [FromQuery, Required, Range(-180d, 180d)] double? longitude,
        CancellationToken cancellationToken)
    {
        if (!latitude.HasValue || !longitude.HasValue)
            return BadRequest(ApiResponse<GeocodingResponseDto>.FailureResponse("Latitude and longitude are required."));
        try
        {
            var result = await geocoding.GetAddressAsync(latitude.Value, longitude.Value, cancellationToken);
            return Ok(ApiResponse<GeocodingResponseDto>.SuccessResponse(result, "Location found."));
        }
        catch (GeocodingException error)
        {
            return StatusCode(error.StatusCode, ApiResponse<GeocodingResponseDto>.FailureResponse(error.Message));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCoordinates(
        [FromQuery, Required, StringLength(500, MinimumLength = 3)] string address,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await geocoding.GetCoordinatesAsync(address, cancellationToken);
            return Ok(ApiResponse<GeocodingResponseDto>.SuccessResponse(result, "Address located."));
        }
        catch (GeocodingException error)
        {
            return StatusCode(error.StatusCode, ApiResponse<GeocodingResponseDto>.FailureResponse(error.Message));
        }
    }
}
