using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Services.Interfaces;

public interface IGeocodingService
{
    Task<GeocodingResponseDto> GetCoordinatesAsync(string address, CancellationToken cancellationToken = default);
    Task<GeocodingResponseDto> GetAddressAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
