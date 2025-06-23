namespace cater_ease_api.Dtos.Review;

public class ReviewDetailDto
{
    public string Id { get; set; } = null!;
    public string MenuId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string? Comment { get; set; }
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}