// ===========================================================================================================
// File: CompleteTransactionRequest.cs
// Project: Smart Solar Microgrid Trading System
// Module: M3 – Transaction & Verification Management
// Section Owned: M3 – Transaction & Verification Management
// Author: J. Jathusan (IT23297418)
// Description: Data transfer object (DTO) representing CompleteTransactionRequest communication payload.
// ===========================================================================================================
namespace SmartMicrogrid.API.DTOs.Transactions
{
    public class CompleteTransactionRequest
    {
        public string Confirmation { get; set; } = string.Empty;
    }
}