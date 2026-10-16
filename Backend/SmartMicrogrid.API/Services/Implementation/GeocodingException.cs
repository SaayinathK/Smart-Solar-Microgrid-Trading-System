// ===========================================================================================================
// File: GeocodingException.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Defines GeocodingException components for the Smart Microgrid system.
// ===========================================================================================================
namespace SmartMicrogrid.API.Services.Implementation;

// Only controlled, public messages are returned; Google's raw response/key stays private.
public sealed class GeocodingException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
