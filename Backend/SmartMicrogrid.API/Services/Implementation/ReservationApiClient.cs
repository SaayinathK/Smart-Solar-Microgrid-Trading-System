using System.Net;
using System.Net.Http.Json;
using SmartMicrogrid.API.DTOs.Transactions;
using SmartMicrogrid.API.Services.Interfaces;

namespace SmartMicrogrid.API.Services.Implementation
{
    public class ReservationApiClient : IReservationApiClient
    {
        private readonly HttpClient _httpClient;

        public ReservationApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ReservationDto?> GetReservationAsync(
            string reservationId)
        {
            if (string.IsNullOrWhiteSpace(reservationId))
            {
                return null;
            }

            var response = await _httpClient.GetAsync(
                $"api/reservations/{reservationId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content
                    .ReadFromJsonAsync<ReservationApiResponse>();

            return result?.Success == true
                ? result.Data
                : null;
        }
    }
}