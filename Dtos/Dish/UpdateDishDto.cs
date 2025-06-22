namespace cater_ease_api.Dtos.Dish;

public class UpdateDishDto
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Image { get; set; }
    public List<string>? SubImage { get; set; }
    public string? CuisineId { get; set; }
    public string? EventId { get; set; }
}