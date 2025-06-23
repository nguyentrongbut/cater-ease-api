namespace cater_ease_api.Dtos.Dish;

public class CreateDishDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string CategoryId { get; set; } = null!;
    public IFormFile? Image { get; set; }
}