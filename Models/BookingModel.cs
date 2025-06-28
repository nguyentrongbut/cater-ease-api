using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace cater_ease_api.Models;

public class BookingModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string AuthId { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Phone { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string EventName { get; set; } = null!;

    [Required]
    public string LocationType { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int People { get; set; }

    public string Address { get; set; } = null!;
    public string EventTime { get; set; } = null!;
    public string EventDate { get; set; } = null!;

    public List<string> MenuIds { get; set; } = new();
    public List<string> ServiceIds { get; set; } = new();

    public string? Note { get; set; }

    public string Status { get; set; } = "pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}