using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Category;

public class UpdateCategoryDto
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = null!;
}