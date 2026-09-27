using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartMicrogrid.API.Models.M2;

public class Reservation
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)] public string? Id { get; set; }
    [BsonElement("prosumerId")] public string ProsumerId { get; set; } = string.Empty;
    [BsonElement("microgridNodeId"), BsonRepresentation(BsonType.ObjectId)] public string MicrogridNodeId { get; set; } = string.Empty;
    [BsonElement("energySlotId"), BsonRepresentation(BsonType.ObjectId)] public string EnergySlotId { get; set; } = string.Empty;
    [BsonElement("energyAmount")] public double EnergyAmount { get; set; }
    [BsonElement("reservationDate")] public DateTime ReservationDate { get; set; }
    [BsonElement("startTime")] public DateTime StartTime { get; set; }
    [BsonElement("endTime")] public DateTime EndTime { get; set; }
    [BsonElement("status")] public string Status { get; set; } = "Pending";
    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("updatedAt")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("statusHistory")] public List<ReservationStatusEvent> StatusHistory { get; set; } = new();
}

public class ReservationStatusEvent
{
    [BsonElement("from")] public string? From { get; set; }
    [BsonElement("to")] public string To { get; set; } = string.Empty;
    [BsonElement("changedBy")] public string ChangedBy { get; set; } = string.Empty;
    [BsonElement("changedAt")] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("reason")] public string? Reason { get; set; }
}
