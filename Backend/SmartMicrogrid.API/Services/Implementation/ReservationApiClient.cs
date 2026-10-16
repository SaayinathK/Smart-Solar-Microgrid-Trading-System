// ===========================================================================================================
// File: ReservationApiClient.cs
// Project: Smart Solar Microgrid Trading System
// Module: M2 – Marketplace & Reservation Management
// Section Owned: M2 – Marketplace & Reservation Management
// Author: J. Shathursini (IT23164062)
// Description: Defines ReservationApiClient components for the Smart Microgrid system.
// ===========================================================================================================
using System.Net;
using System.Net.Http.Json;
using SmartMicrogrid.API.DTOs.Transactions;
using SmartMicrogrid.API.Services.Interfaces;

namespace SmartMicrogrid.API.Services.Implementation
{
    public class ReservationApiClient : IReservationApiClient
    {
        private readonly HttpClient _httpClient;
        /// <summary>
        /// Initializes a new instance of the ReservationApiClient class.
        /// </summary>

        public ReservationApiClient(HttpClient httpClient)
        {
            // Initialize dependencies and state
            _httpClient = httpClient;
        }
        /// <summary>
        /// Retrieves reservation async details.
        /// </summary>

        public async Task<ReservationDto?> GetReservationAsync(
            string reservationId)
        {
            // Execute get reservation async operations
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