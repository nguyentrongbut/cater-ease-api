using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Room;

public class CreateRoomDto
{
    [Required(ErrorMessage = "Room name is required")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Area is required")]
    public string Area { get; set; } = null!;

    [Required(ErrorMessage = "People capacity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "People must be greater than 0")]
    public int People { get; set; }

    [Required(ErrorMessage = "Table number is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Table must be greater than 0")]
    public int Table { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative")]
    public double Price { get; set; }

    [Required(ErrorMessage = "Image is required")]
    public IFormFile Image { get; set; } = null!;
}