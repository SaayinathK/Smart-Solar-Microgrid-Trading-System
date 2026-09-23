using System;

namespace SmartMicrogrid.API.DTOs.M1
{
    public class EnergyAvailabilityQueryDto
    {
        public string? MicrogridId { get; set; }
        public string? Location { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double? MinimumEnergy { get; set; }
        public string? Status { get; set; } = "Available";
    }

    public class EnergyAvailabilityResponseDto
    {
        public string EnergySlotId { get; set; } = string.Empty;
        public string MicrogridNodeId { get; set; } = string.Empty;
        public string MicrogridName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double EnergyAmount { get; set; }
        public double AvailableAmount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal PricePerUnit { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
