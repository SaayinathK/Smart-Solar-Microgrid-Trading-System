// ===========================================================================================================
// File: GeocodingResponseDto.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Data transfer object (DTO) representing GeocodingResponseDto communication payload.
// ===========================================================================================================
namespace SmartMicrogrid.API.DTOs.M1;

public class GeocodingResponseDto
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string FormattedAddress { get; set; } = string.Empty;
    public bool IsApproximate { get; set; }
}
