namespace SmartMicrogrid.API.DTOs.Transactions
{
    public class ReservationDto
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
    }
}