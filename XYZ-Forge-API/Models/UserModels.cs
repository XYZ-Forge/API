using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace XYZForge.Models
{
    public record User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } // MongoDB's unique identifier

        [BsonElement("Username")]
        public string Username { get; set; } = string.Empty;

        [BsonElement("Password")]
        public string Password { get; set; } = string.Empty;

        [BsonElement("Role")]
        public string Role { get; set; } = "User";
    }

    public record UserRegistration(string Username, string Password, string? Role = "User");
    public record UserLogin(string Username, string Password);
    public record UserUpdate(string IssuerJWT, string Username, string TargetRole, string TargetUsername, string TargetPassword);
    public record UserDelete(string IssuerJWT, string Username);
}
