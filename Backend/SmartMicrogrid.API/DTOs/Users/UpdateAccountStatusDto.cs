// ===========================================================================================================
// File: UpdateAccountStatusDto.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Data transfer object (DTO) representing UpdateAccountStatusDto communication payload.
// ===========================================================================================================
using System.ComponentModel.DataAnnotations;
using SmartMicrogrid.API.Models.Common;

namespace SmartMicrogrid.API.DTOs.Users
{
    /// <summary>
    /// Administrative account lifecycle transition. Unlike
    /// <see cref="UpdateStatusDto"/>, which can only flip Active/Inactive, this
    /// exposes the full <see cref="AccountStatus"/> set so the backoffice officer
    /// can suspend an account or place it in pending review.
    /// </summary>
    public class UpdateAccountStatusDto
    {
        [Required(ErrorMessage = "Account status is required.")]
        public AccountStatus AccountStatus { get; set; }
    }
}
