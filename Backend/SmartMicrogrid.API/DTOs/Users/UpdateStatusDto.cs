using System.ComponentModel.DataAnnotations;

namespace SmartMicrogrid.API.DTOs.Users
{
    public class UpdateStatusDto
    {
        [Required]
        public bool IsActive { get; set; }
    }
}
