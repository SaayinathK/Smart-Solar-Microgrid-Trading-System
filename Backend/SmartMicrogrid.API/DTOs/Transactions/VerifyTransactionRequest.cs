// ===========================================================================================================
// File: VerifyTransactionRequest.cs
// Project: Smart Solar Microgrid Trading System
// Module: M3 – Transaction & Verification Management
// Section Owned: M3 – Transaction & Verification Management
// Author: J. Jathusan (IT23297418)
// Description: Data transfer object (DTO) representing VerifyTransactionRequest communication payload.
// ===========================================================================================================
namespace SmartMicrogrid.API.DTOs.Transactions
{
    public class VerifyTransactionRequest
    {
        public string QrCodeData { get; set; } = string.Empty;
    }
}