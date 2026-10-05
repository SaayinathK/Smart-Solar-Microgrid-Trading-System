// ===========================================================================================================
// File: StatusUpdateDtos.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Data transfer object (DTO) representing StatusUpdateDtos communication payload.
// ===========================================================================================================
namespace SmartMicrogrid.API.DTOs.M1
{
    public class StatusUpdateDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class SlotStatusUpdateDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
