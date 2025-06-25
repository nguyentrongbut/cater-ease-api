using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Menu;

public class CreateMenuDto
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Required(ErrorMessage = "At least one dish is required")]
    public List<string> DishIds { get; set; } = new();
    public IFormFile? Image { get; set; }
    public string? EventId { get; set; }
    public DateTime CreatedAt { get; set; }
}