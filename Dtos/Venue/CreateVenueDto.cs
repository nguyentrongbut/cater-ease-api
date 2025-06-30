using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Venue;

public class CreateVenueDto
{
    [Required(ErrorMessage = "Venue name is required")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Area is required")]
    public string Area { get; set; } = null!;

    [Required(ErrorMessage = "People is required")]
    [Range(1, int.MaxValue, ErrorMessage = "People must be greater than 0")]
    public int People { get; set; }

    public string? Description { get; set; }

    public List<string>? RoomIds { get; set; }

    public List<IFormFile>? HeroBanners { get; set; }

    public List<IFormFile>? ThumbnailImages { get; set; }

    public List<IFormFile>? GalleryImages { get; set; }

    [Required(ErrorMessage = "Address is required")]
    public string Address { get; set; } = null!;

    [Required(ErrorMessage = "Open time is required")]
    public string Open { get; set; } = null!;

    [Required(ErrorMessage = "Close time is required")]
    public string Close { get; set; } = null!;

    [Required(ErrorMessage = "Days are required")]
    public List<string> Days { get; set; } = new();
    public IFormFile? Image { get; set; }
}