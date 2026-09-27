using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartMicrogrid.API.Models.M1
{
    public class MicrogridNode
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("location")]
        public string Location { get; set; } = string.Empty;

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("latitude")]
        public double Latitude { get; set; }

        [BsonElement("longitude")]
        public double Longitude { get; set; }

        [BsonElement("capacity")]
        public double Capacity { get; set; }

        [BsonElement("availableCapacity")]
        public double AvailableCapacity { get; set; }

        [BsonElement("reservedCapacity")]
        public double ReservedCapacity { get; set; }

        [BsonElement("usedCapacity")]
        public double UsedCapacity { get; set; }

        [BsonElement("batteryCapacity")]
        public double BatteryCapacity { get; set; }

        [BsonElement("batteryStorageSlots")]
        public int BatteryStorageSlots { get; set; }

        [BsonElement("currentBatteryLevel")]
        public double CurrentBatteryLevel { get; set; }

        [BsonElement("batteryPercentage")]
        public double BatteryPercentage { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "Active"; // Active, Inactive, Maintenance, Offline

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("operatorId")]
        public string OperatorId { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
