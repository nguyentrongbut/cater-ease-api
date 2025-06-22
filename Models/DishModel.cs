using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace cater_ease_api.Models;

public class DishModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Slug is required")]
    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be positive")]
    public decimal Price { get; set; }

    public string? Image { get; set; }
    
    public List<string>? SubImage { get; set; }

    [Required(ErrorMessage = "CuisineId is required")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CuisineId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? EventId { get; set; }
}