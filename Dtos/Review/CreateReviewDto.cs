using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Review;

public class CreateReviewDto
{
    [Required]
    public string DishId { get; set; } = null!;

    [Required]
    public string AuthId { get; set; } = null!;

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }
}