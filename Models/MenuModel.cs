using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace cater_ease_api.Models;

public class MenuModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    // List of Dish Ids
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> DishIds { get; set; } = new();

    public string? Image { get; set; }
    
    [BsonRepresentation(BsonType.ObjectId)]
    public string? EventId { get; set; }
    public string Slug { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}