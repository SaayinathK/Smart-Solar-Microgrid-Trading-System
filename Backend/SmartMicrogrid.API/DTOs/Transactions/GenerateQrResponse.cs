// ===========================================================================================================
// File: GenerateQrResponse.cs
// Project: Smart Solar Microgrid Trading System
// Module: M3 – Transaction & Verification Management
// Section Owned: M3 – Transaction & Verification Management
// Author: J. Jathusan (IT23297418)
// Description: Data transfer object (DTO) representing GenerateQrResponse communication payload.
// ===========================================================================================================
namespace SmartMicrogrid.API.DTOs.Transactions
{
    public class GenerateQrResponse
    {
        public string TransactionId { get; set; } = string.Empty;

        public string ReservationId { get; set; } = string.Empty;

        public string TransactionCode { get; set; } = string.Empty;

        public string QrCodeData { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}