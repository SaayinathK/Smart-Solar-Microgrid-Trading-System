// ===========================================================================================================
// File: GeocodingTests.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Unit/Integration test suite verifying Geocoding operations and validations.
// ===========================================================================================================
using System.Net;
using Microsoft.Extensions.Configuration;
using SmartMicrogrid.API.Controllers;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Services.Implementation;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace SmartMicrogrid.API.Tests;

public class GeocodingTests
{
    private const string Place = """
        {"lat":"6.851234567","lon":"79.861234567","display_name":"Dehiwala, Sri Lanka","address":{"house_number":"25/3"}}
        """;
    private const string Search = "[" + Place + "]";

    private sealed class FakeHandler(string json, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public Uri? RequestedUri { get; private set; }
        public string? UserAgent { get; private set; }
        public List<DateTimeOffset> Starts { get; } = [];
        public Exception? Error { get; init; }
        public bool ReverseUsesPlace { get; init; }
        /// <summary>
        /// Performs send async operation.
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Execute send async operations
            RequestedUri = request.RequestUri;
            UserAgent = request.Headers.UserAgent.ToString();
            Starts.Add(DateTimeOffset.UtcNow);
            if (Error != null) return Task.FromException<HttpResponseMessage>(Error);
            var content = ReverseUsesPlace && request.RequestUri!.AbsolutePath.EndsWith("/reverse") ? Place : json;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(content) });
        }
    }
    /// <summary>
    /// Performs service operation.
    /// </summary>

    private static OpenStreetMapGeocodingService Service(FakeHandler handler, string endpoint = "https://nominatim.openstreetmap.org/") =>
        new(new HttpClient(handler), new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["OpenStreetMap:GeocodingBaseUrl"] = endpoint }).Build());
    /// <summary>
    /// Performs encodes address and returns exact coordinates without akey operation.
    /// </summary>

    [Fact]
    public async Task EncodesAddressAndReturnsExactCoordinatesWithoutAKey()
    {
        // Execute encodes address and returns exact coordinates without akey operations
        var handler = new FakeHandler(Search);
        using var service = Service(handler);
        const string address = "25/3, Sri mahabodhi road & lane, Dehiwala";
        var result = await service.GetCoordinatesAsync("  " + address + "  ");
        Assert.Contains("q=" + Uri.EscapeDataString(address), handler.RequestedUri!.AbsoluteUri);
        Assert.DoesNotContain("key=", handler.RequestedUri.Query);
        Assert.Equal("nominatim.openstreetmap.org", handler.RequestedUri.Host);
        Assert.Contains("SmartMicrogrid", handler.UserAgent);
        Assert.Equal(6.851234567, result.Lat);
        Assert.Equal(79.861234567, result.Lng);
        Assert.Equal("Dehiwala, Sri Lanka", result.FormattedAddress);
        Assert.False(result.IsApproximate);
    }
    /// <summary>
    /// Performs reverse lookup uses invariant coordinates and can switch provider operation.
    /// </summary>

    [Fact]
    public async Task ReverseLookupUsesInvariantCoordinatesAndCanSwitchProvider()
    {
        // Execute reverse lookup uses invariant coordinates and can switch provider operations
        var handler = new FakeHandler(Place);
        using var service = Service(handler, "https://maps.example.test/nominatim/");
        var result = await service.GetAddressAsync(6.851234567, 79.861234567);
        Assert.Equal("maps.example.test", handler.RequestedUri!.Host);
        Assert.Equal("/nominatim/reverse", handler.RequestedUri.AbsolutePath);
        Assert.Contains("lat=6.851234567&lon=79.861234567", handler.RequestedUri.Query);
        Assert.Equal("Dehiwala, Sri Lanka", result.FormattedAddress);
    }
    /// <summary>
    /// Performs repeated addresses use cache and concurrent requests are spaced apart operation.
    /// </summary>

    [Fact]
    public async Task RepeatedAddressesUseCacheAndConcurrentRequestsAreSpacedApart()
    {
        // Execute repeated addresses use cache and concurrent requests are spaced apart operations
        var handler = new FakeHandler(Search) { ReverseUsesPlace = true };
        using var service = Service(handler);
        await Task.WhenAll(service.GetCoordinatesAsync("Dehiwala"), service.GetCoordinatesAsync("Dehiwala"));
        Assert.Single(handler.Starts);
        await Task.WhenAll(service.GetCoordinatesAsync("Colombo"), service.GetAddressAsync(7, 80));
        Assert.Equal(3, handler.Starts.Count);
        Assert.True(handler.Starts[1] - handler.Starts[0] >= TimeSpan.FromSeconds(1));
        Assert.True(handler.Starts[2] - handler.Starts[1] >= TimeSpan.FromSeconds(1));
    }
    /// <summary>
    /// Performs rate limited provider triggers cooldown without another provider call operation.
    /// </summary>

    [Fact]
    public async Task RateLimitedProviderTriggersCooldownWithoutAnotherProviderCall()
    {
        // Execute rate limited provider triggers cooldown without another provider call operations
        var handler = new FakeHandler("", HttpStatusCode.TooManyRequests);
        using var service = Service(handler);
        Assert.Equal(503, (await Assert.ThrowsAsync<GeocodingException>(() => service.GetCoordinatesAsync("Dehiwala"))).StatusCode);
        Assert.Equal(503, (await Assert.ThrowsAsync<GeocodingException>(() => service.GetCoordinatesAsync("Colombo"))).StatusCode);
        Assert.Single(handler.Starts);
    }
    /// <summary>
    /// Performs invalid address does not call provider operation.
    /// </summary>

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    public async Task InvalidAddressDoesNotCallProvider(string address)
    {
        // Execute invalid address does not call provider operations
        var handler = new FakeHandler(Search);
        using var service = Service(handler);
        Assert.Equal(400, (await Assert.ThrowsAsync<GeocodingException>(() => service.GetCoordinatesAsync(address))).StatusCode);
        Assert.Empty(handler.Starts);
    }
    /// <summary>
    /// Performs invalid coordinates do not call provider operation.
    /// </summary>

    [Theory]
    [InlineData(91, 80)]
    [InlineData(7, -181)]
    [InlineData(double.NaN, 80)]
    public async Task InvalidCoordinatesDoNotCallProvider(double latitude, double longitude)
    {
        // Execute invalid coordinates do not call provider operations
        var handler = new FakeHandler(Place);
        using var service = Service(handler);
        Assert.Equal(400, (await Assert.ThrowsAsync<GeocodingException>(() => service.GetAddressAsync(latitude, longitude))).StatusCode);
        Assert.Empty(handler.Starts);
    }
    /// <summary>
    /// Performs rejects malformed and missing matches operation.
    /// </summary>

    [Theory]
    [InlineData("not json", 502)]
    [InlineData("[]", 404)]
    [InlineData("[{}]", 502)]
    [InlineData("{\"error\":\"internal information\"}", 502)]
    [InlineData("[{\"lat\":\"91\",\"lon\":\"80\",\"display_name\":\"Bad\"}]", 502)]
    [InlineData("[{\"lat\":\"NaN\",\"lon\":\"80\",\"display_name\":\"Bad\"}]", 502)]
    public async Task RejectsMalformedAndMissingMatches(string json, int status)
    {
        // Execute rejects malformed and missing matches operations
        using var service = Service(new FakeHandler(json));
        var error = await Assert.ThrowsAsync<GeocodingException>(() => service.GetCoordinatesAsync("Dehiwala"));
        Assert.Equal(status, error.StatusCode);
        Assert.DoesNotContain("internal information", error.Message);
    }
    /// <summary>
    /// Performs not found results are cached operation.
    /// </summary>

    [Fact]
    public async Task NotFoundResultsAreCached()
    {
        // Execute not found results are cached operations
        var handler = new FakeHandler("[]");
        using var service = Service(handler);
        for (var i = 0; i < 2; i++)
            Assert.Equal(404, (await Assert.ThrowsAsync<GeocodingException>(() => service.GetCoordinatesAsync("Unknown place"))).StatusCode);
        Assert.Single(handler.Starts);
    }
    /// <summary>
    /// Performs city matches are marked approximate operation.
    /// </summary>

    [Fact]
    public async Task CityMatchesAreMarkedApproximate()
    {
        // Execute city matches are marked approximate operations
        using var service = Service(new FakeHandler("[{\"lat\":\"7\",\"lon\":\"80\",\"display_name\":\"Town\"}]"));
        Assert.True((await service.GetCoordinatesAsync("Town")).IsApproximate);
    }
    /// <summary>
    /// Performs timeout network errors and cancellation have correct behavior operation.
    /// </summary>

    [Fact]
    public async Task TimeoutNetworkErrorsAndCancellationHaveCorrectBehavior()
    {
        // Execute timeout network errors and cancellation have correct behavior operations
        foreach (var exception in new Exception[] { new TaskCanceledException(), new HttpRequestException("private URL") })
        {
            using var service = Service(new FakeHandler("") { Error = exception });
            var error = await Assert.ThrowsAsync<GeocodingException>(() => service.GetCoordinatesAsync("Dehiwala"));
            Assert.Equal(exception is TaskCanceledException ? 504 : 502, error.StatusCode);
            Assert.DoesNotContain("private URL", error.ToString());
        }
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        using var service2 = Service(new FakeHandler(Search));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service2.GetCoordinatesAsync("Dehiwala", canceled.Token));
    }
    /// <summary>
    /// Performs controllers return the existing envelope operation.
    /// </summary>

    [Fact]
    public async Task ControllersReturnTheExistingEnvelope()
    {
        // Execute controllers return the existing envelope operations
        using var service = Service(new FakeHandler(Search));
        var controller = new GeocodingController(service);
        var response = Assert.IsType<OkObjectResult>(await controller.GetCoordinates("Dehiwala", default));
        var body = Assert.IsType<ApiResponse<GeocodingResponseDto>>(response.Value);
        Assert.True(body.Success);
        Assert.Equal(6.851234567, body.Data!.Lat);
        using var reverse = Service(new FakeHandler(Place));
        var result = Assert.IsType<OkObjectResult>(await new GeocodingController(reverse).GetAddress(6.9, 79.8, default));
        Assert.True(Assert.IsType<ApiResponse<GeocodingResponseDto>>(result.Value).Success);
        Assert.IsType<BadRequestObjectResult>(await controller.GetAddress(null, null, default));
    }
}
