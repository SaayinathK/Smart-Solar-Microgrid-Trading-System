using System.ComponentModel.DataAnnotations;

namespace SmartMicrogrid.API.DTOs.M1
{
    public class CreateMicrogridDto
    {
        [Required(ErrorMessage = "Microgrid name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required.")]
        public string Location { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
        public double Latitude { get; set; }

        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
        public double Longitude { get; set; }

        [Range(0.01, 1000000.0, ErrorMessage = "Capacity must be greater than zero.")]
        public double Capacity { get; set; }

        [Range(0.0, 1000000.0, ErrorMessage = "Battery capacity cannot be negative.")]
        public double BatteryCapacity { get; set; }

        [Range(0, 100000, ErrorMessage = "Battery storage slots cannot be negative.")]
        public int BatteryStorageSlots { get; set; }

        public double CurrentBatteryLevel { get; set; }

        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        public string? OperatorId { get; set; }
    }
}
