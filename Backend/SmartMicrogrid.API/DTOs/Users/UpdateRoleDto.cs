// ===========================================================================================================
// File: UpdateRoleDto.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Data transfer object (DTO) representing UpdateRoleDto communication payload.
// ===========================================================================================================
using System.ComponentModel.DataAnnotations;
using SmartMicrogrid.API.Models.Common;

namespace SmartMicrogrid.API.DTOs.Users
{
    public class UpdateRoleDto
    {
        [Required]
        public Role Role { get; set; }
    }
}
