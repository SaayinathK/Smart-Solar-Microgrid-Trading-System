namespace SmartMicrogrid.API.DTOs.M1;

public class GeocodingResponseDto
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string FormattedAddress { get; set; } = string.Empty;
    public bool IsApproximate { get; set; }
}
