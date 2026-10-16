// ===========================================================================================================
// File: UpdateCapacityDto.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Data transfer object (DTO) representing UpdateCapacityDto communication payload.
// ===========================================================================================================
using System.ComponentModel.DataAnnotations;

namespace SmartMicrogrid.API.DTOs.M1
{
    public class UpdateCapacityDto
    {
        [Range(0.01, 1000000.0, ErrorMessage = "Total capacity must be greater than zero.")]
        public double TotalCapacity { get; set; }

        [Range(0.0, 1000000.0, ErrorMessage = "Available capacity cannot be negative.")]
        public double AvailableCapacity { get; set; }

        [Range(0.0, 1000000.0, ErrorMessage = "Reserved capacity cannot be negative.")]
        public double ReservedCapacity { get; set; }

        [Range(0.0, 1000000.0, ErrorMessage = "Used capacity cannot be negative.")]
        public double UsedCapacity { get; set; }
    }

    public class CapacityResponseDto
    {
        public string MicrogridId { get; set; } = string.Empty;
        public double TotalCapacity { get; set; }
        public double AvailableCapacity { get; set; }
        public double ReservedCapacity { get; set; }
        public double UsedCapacity { get; set; }
    }
}
