namespace SmartMicrogrid.API.DTOs.Transactions
{
    public class VerifyTransactionResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? TransactionId { get; set; }

        public string? ReservationId { get; set; }

        public string? Status { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}