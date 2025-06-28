namespace cater_ease_api.Dtos.Review;

public class ReviewDetailDto
{
    public string Id { get; set; } = null!;
    public string EventId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}