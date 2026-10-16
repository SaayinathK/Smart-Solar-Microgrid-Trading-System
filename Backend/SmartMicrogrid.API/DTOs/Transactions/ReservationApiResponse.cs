// ===========================================================================================================
// File: ReservationApiResponse.cs
// Project: Smart Solar Microgrid Trading System
// Module: M3 – Transaction & Verification Management
// Section Owned: M3 – Transaction & Verification Management
// Author: J. Jathusan (IT23297418)
// Description: Data transfer object (DTO) representing ReservationApiResponse communication payload.
// ===========================================================================================================
namespace SmartMicrogrid.API.DTOs.Transactions
{
    public class ReservationApiResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public ReservationDto? Data { get; set; }
    }
}