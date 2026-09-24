using System.ComponentModel.DataAnnotations;
using SmartMicrogrid.API.Models.M2;

namespace SmartMicrogrid.API.DTOs.M2;

public class CreateReservationDto
{
    [RegularExpression(@"^(?:[0-9]{9}[VvXx]|[0-9]{12})$", ErrorMessage = "Enter a valid prosumer NIC.")]
    public string? ProsumerId { get; set; }
    [Required] public string EnergySlotId { get; set; } = string.Empty;
    [Range(0.1, 1000000)] public double EnergyAmount { get; set; }
    public DateTime? ReservationDate { get; set; }
}

public class ReservationResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string ProsumerId { get; set; } = string.Empty;
    public string MicrogridNodeId { get; set; } = string.Empty;
    public string EnergySlotId { get; set; } = string.Empty;
    public double EnergyAmount { get; set; }
    public DateTime ReservationDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ReservationStatusEvent> StatusHistory { get; set; } = new();
}

public class ReservationSummaryDto
{
    public long PendingCount { get; set; }
    public long ApprovedFutureCount { get; set; }
    public long CompletedThisMonthCount { get; set; }
    public double TotalEnergyReserved { get; set; }
}

public class RejectReservationDto { public string? Reason { get; set; } }
public class UpdateReservationDto
{
    public string? EnergySlotId { get; set; }
    [Range(0.1, 1000000)] public double EnergyAmount { get; set; }
}
public class ReservationStatusUpdateDto
{
    [Required] public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
