using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace cater_ease_api.Models;

public class AuthModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = null!;

    public string Role { get; set; } = "customer";

    public string? Avatar { get; set; }

    [Required(ErrorMessage = "Phone is required")]
    public string Phone { get; set; } = null!;

    public string? Address { get; set; }
    public string Status { get; set; } = "active";
    
    public bool Deleted { get; set; } = false;
}