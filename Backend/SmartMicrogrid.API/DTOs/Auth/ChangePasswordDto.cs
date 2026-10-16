// ===========================================================================================================
// File: ChangePasswordDto.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Data transfer object (DTO) representing ChangePasswordDto communication payload.
// ===========================================================================================================
using System.ComponentModel.DataAnnotations;

namespace SmartMicrogrid.API.DTOs.Auth
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm new password is required.")]
        [Compare("NewPassword", ErrorMessage = "New password and confirm password do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
