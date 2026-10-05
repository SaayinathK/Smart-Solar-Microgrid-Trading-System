// ===========================================================================================================
// File: EnergySlot.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Domain entity model representing EnergySlot in the database.
// ===========================================================================================================
using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartMicrogrid.API.Models.M1
{
    public class EnergySlot
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("microgridNodeId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string MicrogridNodeId { get; set; } = string.Empty;

        [BsonElement("energyAmount")]
        public double EnergyAmount { get; set; }

        [BsonElement("availableAmount")]
        public double AvailableAmount { get; set; }

        [BsonElement("startTime")]
        public DateTime StartTime { get; set; }

        [BsonElement("endTime")]
        public DateTime EndTime { get; set; }

        [BsonElement("pricePerUnit")]
        public decimal PricePerUnit { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "Available"; // Available, PartiallyReserved, FullyReserved, Expired, Cancelled

        [BsonElement("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
