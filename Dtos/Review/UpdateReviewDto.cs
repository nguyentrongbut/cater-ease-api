using System.ComponentModel.DataAnnotations;
namespace cater_ease_api.Dtos.Review;

public class UpdateReviewDto
{
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int? Rating { get; set; }

    [StringLength(500, ErrorMessage = "Comment is too long")]
    public string? Comment { get; set; }
}