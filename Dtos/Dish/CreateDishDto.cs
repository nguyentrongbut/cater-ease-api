namespace cater_ease_api.Dtos.Dish;

public class CreateDishDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string CuisineId { get; set; } = null!;
    public string? EventId { get; set; }
    public IFormFile? Image { get; set; }
    public List<IFormFile>? SubImage { get; set; }
}