using System;
using System.ComponentModel.DataAnnotations;

namespace SmartMicrogrid.API.DTOs.M1
{
    public class CreateEnergySlotDto
    {
        [Required(ErrorMessage = "MicrogridNodeId is required.")]
        public string MicrogridNodeId { get; set; } = string.Empty;

        [Range(0.01, 100000.0, ErrorMessage = "Energy amount must be greater than zero.")]
        public double EnergyAmount { get; set; }

        public double? AvailableAmount { get; set; }

        [Required(ErrorMessage = "StartTime is required.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "EndTime is required.")]
        public DateTime EndTime { get; set; }

        [Range(0.0, 10000.0, ErrorMessage = "Price per unit cannot be negative.")]
        public decimal PricePerUnit { get; set; }

        public string Status { get; set; } = "Available";
    }

    public class UpdateEnergySlotDto
    {
        [Range(0.01, 100000.0, ErrorMessage = "Energy amount must be greater than zero.")]
        public double EnergyAmount { get; set; }

        [Range(0.0, 100000.0, ErrorMessage = "Available amount cannot be negative.")]
        public double AvailableAmount { get; set; }

        [Required(ErrorMessage = "StartTime is required.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "EndTime is required.")]
        public DateTime EndTime { get; set; }

        [Range(0.0, 10000.0, ErrorMessage = "Price per unit cannot be negative.")]
        public decimal PricePerUnit { get; set; }

        public string Status { get; set; } = "Available";
    }

    public class EnergySlotResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string MicrogridNodeId { get; set; } = string.Empty;
        public string MicrogridName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double EnergyAmount { get; set; }
        public double AvailableAmount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal PricePerUnit { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
