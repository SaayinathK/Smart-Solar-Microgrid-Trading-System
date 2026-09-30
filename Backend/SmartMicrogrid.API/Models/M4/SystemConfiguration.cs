using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartMicrogrid.API.Models.M4
{
    /// <summary>
    /// M4 platform configuration. A single document lives in the
    /// systemConfiguration collection under the fixed id "platform".
    /// </summary>
    public class SystemConfiguration
    {
        public const string SingletonId = "platform";

        [BsonId]
        public string Id { get; set; } = SingletonId;

        [BsonElement("platformName")]
        public string PlatformName { get; set; } = "Smart Microgrid Energy Management & Trading Platform";

        [BsonElement("platformDescription")]
        public string PlatformDescription { get; set; } =
            "Client-server platform for microgrid energy management, reservation and transaction administration.";

        [BsonElement("maintenanceMode")]
        public bool MaintenanceMode { get; set; }

        [BsonElement("maintenanceMessage")]
        public string? MaintenanceMessage { get; set; }

        [BsonElement("allowRegistration")]
        public bool AllowRegistration { get; set; } = true;

        [BsonElement("sessionTimeoutMinutes")]
        public int SessionTimeoutMinutes { get; set; } = 480;

        [BsonElement("maxLoginAttempts")]
        public int MaxLoginAttempts { get; set; } = 5;

        [BsonElement("defaultPageSize")]
        public int DefaultPageSize { get; set; } = 20;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedBy")]
        public string? UpdatedBy { get; set; }
    }
}
