using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace cater_ease_api.Models;

public class VenueModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [Required(ErrorMessage = "Venue name is required")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Slug is required")]
    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Area is required")]
    public string Area { get; set; } = null!;

    [Required(ErrorMessage = "People is required")]
    [Range(1, int.MaxValue, ErrorMessage = "People must be greater than 0")]
    public int People { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> RoomIds { get; set; } = new();

    public List<string> HeroBanners { get; set; } = new();
    public List<string> ThumbnailImages { get; set; } = new();
    public List<string> GalleryImages { get; set; } = new();

    [Required(ErrorMessage = "Address is required")]
    public string Address { get; set; } = null!;

    [Required(ErrorMessage = "Open time is required")]
    public string Open { get; set; } = null!;

    [Required(ErrorMessage = "Close time is required")]
    public string Close { get; set; } = null!;

    [Required(ErrorMessage = "Days are required")]
    public List<string> Days { get; set; } = new();

    [Required(ErrorMessage = "Image is required")]
    public string Image { get; set; } = null!;
    
    public bool Deleted { get; set; } = false;
}
