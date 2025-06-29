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
    
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> DishIds { get; set; } = new();

    [Required(ErrorMessage = "Price is required")]
    public decimal Price { get; set; }

    [BsonDefaultValue(false)]
    public bool Deleted { get; set; } = false;
}