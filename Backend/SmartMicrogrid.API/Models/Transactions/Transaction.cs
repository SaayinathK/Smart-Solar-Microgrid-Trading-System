using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartMicrogrid.API.Models.Transactions
{
    public class Transaction
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string ReservationId { get; set; } = string.Empty;

        public string ProsumerId { get; set; } = string.Empty;

        public string MicrogridNodeId { get; set; } = string.Empty;

        public string EnergySlotId { get; set; } = string.Empty;

        public double EnergyAmount { get; set; }

        public string TransactionCode { get; set; } = string.Empty;

        public string QrCodeData { get; set; } = string.Empty;

        public string? VerifiedBy { get; set; }

        public DateTime? VerificationTime { get; set; }

        public DateTime? EnergyTransferTime { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}