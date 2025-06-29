using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Menu;

public class CreateMenuDto
{
    [Required]
    public string Name { get; set; } = null!;

    public List<string> DishIds { get; set; } = new();

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number")]
    public decimal Price { get; set; }
}
