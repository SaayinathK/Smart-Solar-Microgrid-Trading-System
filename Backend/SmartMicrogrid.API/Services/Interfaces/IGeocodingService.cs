// ===========================================================================================================
// File: IGeocodingService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Service interface defining contract for Geocoding operations.
// ===========================================================================================================
using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Services.Interfaces;

public interface IGeocodingService
{
    /// <summary>
    /// Retrieves coordinates async details.
    /// </summary>
    Task<GeocodingResponseDto> GetCoordinatesAsync(string address, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves address async details.
    /// </summary>
    Task<GeocodingResponseDto> GetAddressAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
