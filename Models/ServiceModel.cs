using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Models;

public class ServiceModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    [Range(0, double.MaxValue)]
    public double Price { get; set; }

    public string? Description { get; set; }

    public List<string> Images { get; set; } = new();

    public string? Icon { get; set; } 

    public bool Deleted { get; set; } = false; 
}