using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Venue;

public class UpdateVenueDto
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Area { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative")]
    public double? Price { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "People must be greater than 0")]
    public int? People { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Table must be greater than 0")]
    public int? Table { get; set; }

    public string? Address { get; set; }

    public string? Open { get; set; }

    public string? Close { get; set; }

    public List<string>? Days { get; set; }

    // Hình ảnh
    public List<IFormFile>? AddHeroBanners { get; set; }
    public List<string>? RemoveHeroBanners { get; set; }

    public List<IFormFile>? AddThumbnailImages { get; set; }
    public List<string>? RemoveThumbnailImages { get; set; }

    public List<IFormFile>? AddGalleryImages { get; set; }
    public List<string>? RemoveGalleryImages { get; set; }

    // Phòng
    public List<string>? AddRoomIds { get; set; }
    public List<string>? RemoveRoomIds { get; set; }
}