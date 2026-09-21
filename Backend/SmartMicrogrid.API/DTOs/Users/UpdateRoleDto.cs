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
