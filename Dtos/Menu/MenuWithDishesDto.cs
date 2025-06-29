using cater_ease_api.Dtos.Dish;

namespace cater_ease_api.Dtos.Menu;

public class MenuWithDishesDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public List<DishDetailDto> Dishes { get; set; } = new();
}