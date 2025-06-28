using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Room;

public class CreateRoomDto
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Area is required")]
    public string Area { get; set; } = null!;

    [Required(ErrorMessage = "People capacity is required")]
    public int People { get; set; }

    [Required(ErrorMessage = "Table number is required")]
    public int Table { get; set; }

    [Required(ErrorMessage = "Image is required")]
    public IFormFile Image { get; set; } = null!;
}