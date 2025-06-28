using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace cater_ease_api.Models;

public class RoomModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [Required(ErrorMessage = "Room name is required")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Area is required")]
    public string Area { get; set; } = null!;

    [Required(ErrorMessage = "People capacity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "People must be greater than 0")]
    public int People { get; set; }

    [Required(ErrorMessage = "Table number is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Table must be greater than 0")]
    public int Table { get; set; }

    [Required(ErrorMessage = "Image is required")]
    public string Image { get; set; } = null!;
}