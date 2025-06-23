namespace cater_ease_api.Dtos.Menu;

public class UpdateMenuDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? DishIds { get; set; }
    public string? Image { get; set; } 
    public decimal? Price { get; set; }
}