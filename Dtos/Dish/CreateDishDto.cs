using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Dish;

public class CreateDishDto
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = null!;
}