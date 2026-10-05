// ===========================================================================================================
// File: VerifyTransactionResponse.cs
// Project: Smart Solar Microgrid Trading System
// Module: M3 – Transaction & Verification Management
// Section Owned: M3 – Transaction & Verification Management
// Author: J. Jathusan (IT23297418)
// Description: Data transfer object (DTO) representing VerifyTransactionResponse communication payload.
// ===========================================================================================================
namespace SmartMicrogrid.API.DTOs.Transactions
{
    public class VerifyTransactionResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? TransactionId { get; set; }

        public string? ReservationId { get; set; }

        public string? Status { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}