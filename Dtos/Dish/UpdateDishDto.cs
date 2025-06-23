namespace cater_ease_api.Dtos.Dish;

public class UpdateDishDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Image { get; set; }
    public string? CategoryId { get; set; }
}