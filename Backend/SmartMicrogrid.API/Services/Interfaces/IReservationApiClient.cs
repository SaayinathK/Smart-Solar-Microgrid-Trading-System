// ===========================================================================================================
// File: IReservationApiClient.cs
// Project: Smart Solar Microgrid Trading System
// Module: M2 – Marketplace & Reservation Management
// Section Owned: M2 – Marketplace & Reservation Management
// Author: J. Shathursini (IT23164062)
// Description: Defines IReservationApiClient components for the Smart Microgrid system.
// ===========================================================================================================
using SmartMicrogrid.API.DTOs.Transactions;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IReservationApiClient
    {
        /// <summary>
        /// Retrieves reservation async details.
        /// </summary>
        Task<ReservationDto?> GetReservationAsync(
            string reservationId);
    }
}