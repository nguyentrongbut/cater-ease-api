using cater_ease_api.Dtos.Dish;

namespace cater_ease_api.Dtos.Menu;

public class MenuDetailDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public List<DishDetailDto> Dishes { get; set; } = new();
    public string? Image { get; set; }
    public decimal Price { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public string Slug { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}