namespace SmartMicrogrid.API.DTOs.Transactions
{
    public class GenerateQrResponse
    {
        public string TransactionId { get; set; } = string.Empty;

        public string ReservationId { get; set; } = string.Empty;

        public string TransactionCode { get; set; } = string.Empty;

        public string QrCodeData { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}