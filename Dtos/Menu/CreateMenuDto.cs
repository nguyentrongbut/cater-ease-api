using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Menu;

public class CreateMenuDto
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "DishIds are required")]
    public List<string> DishIds { get; set; } = new();
}