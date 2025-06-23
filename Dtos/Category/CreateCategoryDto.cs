using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Category;

public class CreateCategoryDto
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = null!;
}