using System.ComponentModel.DataAnnotations;

namespace SmartMicrogrid.API.DTOs.M1
{
    public class UpdateBatteryDto
    {
        [Range(0.0, 1000000.0, ErrorMessage = "Battery capacity cannot be negative.")]
        public double BatteryCapacity { get; set; }

        [Range(0.0, 1000000.0, ErrorMessage = "Current battery level cannot be negative.")]
        public double CurrentBatteryLevel { get; set; }
    }

    public class BatteryResponseDto
    {
        public string MicrogridId { get; set; } = string.Empty;
        public double BatteryCapacity { get; set; }
        public double CurrentBatteryLevel { get; set; }
        public double BatteryPercentage { get; set; }
        public string Status { get; set; } = string.Empty; // Charging, Discharging, Full, Low, Normal, Unavailable
    }
}
