namespace cater_ease_api.Dtos.Dish;

public class DishDetailDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Image { get; set; }
    public List<string>? SubImage { get; set; }
    public string CuisineName { get; set; } = null!;
    
    public double AverageRating { get; set; }
    
    public int ReviewCount { get; set; }
    public string? EventName { get; set; }
}