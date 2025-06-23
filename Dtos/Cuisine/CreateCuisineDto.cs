using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Cuisine;

public class CreateCuisineDto
{
    [Required(ErrorMessage = "Cuisine title is required")]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }
}