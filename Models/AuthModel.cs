using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace cater_ease_api.Models;

public class AuthModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Role { get; set; } = "customer";
    public string? Avatar { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = "active";
}