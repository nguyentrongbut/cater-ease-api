using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace cater_ease_api.Models;

public class CuisineModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [Required(ErrorMessage = "Cuisine title is required")]
    public string Title { get; set; } = null!;
    
    public string? Description { get; set; }
}