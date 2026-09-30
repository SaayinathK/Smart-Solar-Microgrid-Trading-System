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
