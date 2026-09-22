namespace SmartMicrogrid.API.DTOs.Transactions
{
    public class ReservationApiResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public ReservationDto? Data { get; set; }
    }
}