// ===========================================================================================================
// File: UpdateStatusDto.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Data transfer object (DTO) representing UpdateStatusDto communication payload.
// ===========================================================================================================
using System.ComponentModel.DataAnnotations;

namespace SmartMicrogrid.API.DTOs.Users
{
    public class UpdateStatusDto
    {
        [Required]
        public bool IsActive { get; set; }
    }
}
