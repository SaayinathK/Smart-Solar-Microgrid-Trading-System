using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Services.Interfaces;

namespace SmartMicrogrid.API.Services.Implementation;

// One instance per API process: forward and reverse lookups share the same rate limit/cache.
public sealed class OpenStreetMapGeocodingService(HttpClient httpClient, IConfiguration configuration)
    : IGeocodingService, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 1000 });
    private DateTimeOffset nextRequest = DateTimeOffset.MinValue;

    public Task<GeocodingResponseDto> GetCoordinatesAsync(string address, CancellationToken cancellationToken = default)
    {
        address = (address ?? "").Trim();
        if (address.Length is < 3 or > 500)
            throw new GeocodingException(400, "Enter an address between 3 and 500 characters.");
        return LookupAsync("search?format=jsonv2&limit=1&addressdetails=1&q=" + Uri.EscapeDataString(address), true, cancellationToken);
    }

    public Task<GeocodingResponseDto> GetAddressAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        if (!double.IsFinite(latitude) || !double.IsFinite(longitude) || Math.Abs(latitude) > 90 || Math.Abs(longitude) > 180)
            throw new GeocodingException(400, "Enter valid latitude and longitude.");
        return LookupAsync("reverse?format=jsonv2&addressdetails=1&lat=" + latitude.ToString("R", CultureInfo.InvariantCulture)
            + "&lon=" + longitude.ToString("R", CultureInfo.InvariantCulture), false, cancellationToken);
    }

    private async Task<GeocodingResponseDto> LookupAsync(string query, bool search, CancellationToken cancellationToken)
    {
        var endpoint = configuration["OpenStreetMap:GeocodingBaseUrl"] ?? "https://nominatim.openstreetmap.org/";
        if (!Uri.TryCreate(endpoint.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) || baseUri.Scheme != "https")
            throw new GeocodingException(503, "Address lookup is not configured correctly. Enter coordinates manually.");
        var uri = new Uri(baseUri, query);
        var key = uri.AbsoluteUri;
        if (cache.TryGetValue(key, out GeocodingResponseDto? cached)) return cached!;
        if (cache.TryGetValue(key + ":missing", out bool _)) throw NotFound();

        // Reject excess simultaneous work instead of allowing an unbounded request queue.
        if (!await gate.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken))
            throw new GeocodingException(429, "Address lookup is busy. Please try again shortly.");
        try
        {
            if (cache.TryGetValue(key, out cached)) return cached!;
            if (cache.TryGetValue(key + ":missing", out bool _)) throw NotFound();
            var wait = nextRequest - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.FromSeconds(2))
                throw new GeocodingException(503, "Address lookup is temporarily busy. Try later or enter coordinates manually.");
            if (wait > TimeSpan.Zero) await Task.Delay(wait, cancellationToken);
            nextRequest = DateTimeOffset.UtcNow.AddMilliseconds(1100);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("SmartMicrogrid/1.0");
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.AcceptLanguage.ParseAdd("en");
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if ((int)response.StatusCode == 429)
            {
                var retry = response.Headers.RetryAfter;
                nextRequest = retry?.Date ?? DateTimeOffset.UtcNow.Add(retry?.Delta ?? TimeSpan.FromSeconds(60));
                throw new GeocodingException(503, "Address lookup is temporarily busy. Try later or enter coordinates manually.");
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) throw NotFound();
            if (!response.IsSuccessStatusCode)
                throw new GeocodingException(502, "The address lookup provider is unavailable. Try again or enter coordinates manually.");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var item = json.RootElement;
            if (search)
            {
                if (item.ValueKind != JsonValueKind.Array) throw InvalidResponse();
                if (item.GetArrayLength() == 0) throw NotFound();
                item = item[0];
            }
            if (item.ValueKind != JsonValueKind.Object) throw InvalidResponse();
            if (item.TryGetProperty("error", out _)) throw NotFound();
            if (!TryCoordinate(item, "lat", out var lat) || !TryCoordinate(item, "lon", out var lng)
                || Math.Abs(lat) > 90 || Math.Abs(lng) > 180
                || !item.TryGetProperty("display_name", out var name) || name.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(name.GetString())) throw InvalidResponse();
            var result = new GeocodingResponseDto
            {
                Lat = lat, Lng = lng, FormattedAddress = name.GetString()!,
                // Street/city centroids need the operator to confirm the exact site on the map.
                IsApproximate = !item.TryGetProperty("address", out var address)
                    || address.ValueKind != JsonValueKind.Object || !address.TryGetProperty("house_number", out _)
            };
            cache.Set(key, result, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1) });
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new GeocodingException(504, "Address lookup timed out. Try again or enter coordinates manually."); }
        catch (HttpRequestException)
        { throw new GeocodingException(502, "Unable to reach the address lookup provider. Try again or enter coordinates manually."); }
        catch (JsonException) { throw InvalidResponse(); }
        catch (GeocodingException error) when (error.StatusCode == 404)
        {
            cache.Set(key + ":missing", true, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });
            throw;
        }
        finally { gate.Release(); }
    }

    private static bool TryCoordinate(JsonElement item, string property, out double value)
    {
        value = double.NaN;
        return item.TryGetProperty(property, out var field)
            && double.TryParse(field.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);
    }

    private static GeocodingException NotFound() => new(404, "Address not found. Include the city and country or enter coordinates manually.");
    private static GeocodingException InvalidResponse() => new(502, "The address lookup provider returned an invalid response. Try another address or enter coordinates manually.");
    public void Dispose() { cache.Dispose(); gate.Dispose(); }
}
