// ===========================================================================================================
// File: CreateTransactionRequest.cs
// Project: Smart Solar Microgrid Trading System
// Module: M3 – Transaction & Verification Management
// Section Owned: M3 – Transaction & Verification Management
// Author: J. Jathusan (IT23297418)
// Description: Data transfer object (DTO) representing CreateTransactionRequest communication payload.
// ===========================================================================================================
namespace SmartMicrogrid.API.DTOs.Transactions
{
    public class CreateTransactionRequest
    {
        public string ReservationId { get; set; } = string.Empty;
    }
}