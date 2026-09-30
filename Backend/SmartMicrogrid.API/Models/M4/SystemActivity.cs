using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartMicrogrid.API.Models.M4
{
    /// <summary>
    /// M4 audit record. One document per administrative or authentication event,
    /// stored in the systemActivity collection.
    /// </summary>
    public class SystemActivity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("userId")]
        public string? UserId { get; set; }

        [BsonElement("userName")]
        public string UserName { get; set; } = "System";

        [BsonElement("role")]
        public string? Role { get; set; }

        [BsonElement("action")]
        public string Action { get; set; } = string.Empty;

        [BsonElement("module")]
        public string Module { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("entityType")]
        public string? EntityType { get; set; }

        [BsonElement("entityId")]
        public string? EntityId { get; set; }

        [BsonElement("ipAddress")]
        public string? IpAddress { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [BsonElement("status")]
        public string Status { get; set; } = AuditStatus.Success;
    }
}
