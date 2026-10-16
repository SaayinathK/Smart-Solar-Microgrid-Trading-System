// ===========================================================================================================
// File: UpdateUserDto.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Data transfer object (DTO) representing UpdateUserDto communication payload.
// ===========================================================================================================
using System.ComponentModel.DataAnnotations;
using SmartMicrogrid.API.Models.Common;

namespace SmartMicrogrid.API.DTOs.Users
{
    public class UpdateUserDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone format.")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? Nic { get; set; }

        public Role? Role { get; set; }
        public bool? IsActive { get; set; }
    }
}
