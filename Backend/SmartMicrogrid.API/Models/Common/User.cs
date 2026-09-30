using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartMicrogrid.API.Models.Common
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [BsonElement("lastName")]
        public string LastName { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [BsonElement("nic")]
        public string? Nic { get; set; }

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [BsonElement("role")]
        [BsonRepresentation(BsonType.String)]
        public Role Role { get; set; } = Role.Prosumer;

        // M4: richer lifecycle state. IsActive remains the authentication gate
        // (AuthService.LoginAsync and the M3 transaction flow read it) and is kept
        // in sync with AccountStatus by UserService.
        [BsonElement("accountStatus")]
        [BsonRepresentation(BsonType.String)]
        public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("statusChangedAt")]
        public DateTime? StatusChangedAt { get; set; }

        [BsonElement("statusChangedBy")]
        public string? StatusChangedBy { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
