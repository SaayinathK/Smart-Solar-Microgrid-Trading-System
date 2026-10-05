// ===========================================================================================================
// File: TransactionResponse.cs
// Project: Smart Solar Microgrid Trading System
// Module: M3 – Transaction & Verification Management
// Section Owned: M3 – Transaction & Verification Management
// Author: J. Jathusan (IT23297418)
// Description: Data transfer object (DTO) representing TransactionResponse communication payload.
// ===========================================================================================================
namespace SmartMicrogrid.API.DTOs.Transactions
{
    public class TransactionResponse
    {
        public string Id { get; set; } = string.Empty;

        public string ReservationId { get; set; } = string.Empty;

        public string ProsumerId { get; set; } = string.Empty;

        public string MicrogridNodeId { get; set; } = string.Empty;

        public string EnergySlotId { get; set; } = string.Empty;

        public double EnergyAmount { get; set; }

        public string TransactionCode { get; set; } = string.Empty;

        public string QrCodeData { get; set; } = string.Empty;

        public string? VerifiedBy { get; set; }

        public DateTime? VerificationTime { get; set; }

        public DateTime? EnergyTransferTime { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}