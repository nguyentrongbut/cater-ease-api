using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace cater_ease_api.Models;

public class EventModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = null!;

    public string? SubName { get; set; }

    public string? Icon { get; set; }

    [Required]
    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> MenuIds { get; set; } = new();
    
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> ServiceIds { get; set; } = new();
    
    public List<string> Images { get; set; } = new();

    public bool Hot { get; set; } = false;
}