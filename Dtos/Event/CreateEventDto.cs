using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Event;

public class CreateEventDto
{
    [Required(ErrorMessage = "Event title is required")]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }
}