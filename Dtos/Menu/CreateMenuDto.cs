using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Menu;

public class CreateMenuDto
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Required(ErrorMessage = "At least one dish is required")]
    public List<string> DishIds { get; set; } = new();

    public IFormFile? Image { get; set; } = null!;

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be positive")]
    public decimal Price { get; set; }
}