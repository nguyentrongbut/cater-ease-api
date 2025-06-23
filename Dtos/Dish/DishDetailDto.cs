namespace cater_ease_api.Dtos.Dish;

public class DishDetailDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Image { get; set; }
    public string CategoryName { get; set; } = null!;
}