using System;

namespace SmartMicrogrid.API.DTOs.M1
{
    public class MicrogridResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Capacity { get; set; }
        public double AvailableCapacity { get; set; }
        public double ReservedCapacity { get; set; }
        public double UsedCapacity { get; set; }
        public double BatteryCapacity { get; set; }
        public int BatteryStorageSlots { get; set; }
        public double CurrentBatteryLevel { get; set; }
        public double BatteryPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string OperatorId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
