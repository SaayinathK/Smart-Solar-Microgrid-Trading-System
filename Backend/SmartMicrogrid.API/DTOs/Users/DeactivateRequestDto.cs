// ===========================================================================================================
// File: DeactivateRequestDto.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Data transfer object (DTO) representing an account deactivation request.
// ===========================================================================================================
namespace SmartMicrogrid.API.DTOs.Users
{
    public class DeactivateRequestDto
    {
        public string? Reason { get; set; }
    }
}
