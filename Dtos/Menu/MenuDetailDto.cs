using cater_ease_api.Models;

namespace cater_ease_api.Dtos.Menu;

public class MenuDetailDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public List<DishModel> Dishes { get; set; } = new(); 
    public string? Image { get; set; }
    public decimal Price { get; set; }
}