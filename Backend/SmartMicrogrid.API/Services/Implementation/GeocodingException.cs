namespace SmartMicrogrid.API.Services.Implementation;

// Only controlled, public messages are returned; Google's raw response/key stays private.
public sealed class GeocodingException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
