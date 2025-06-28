using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Event;

public class CreateEventDto
{
    [Required(ErrorMessage = "Event name is required")]
    public string Name { get; set; } = null!;

    public string? SubName { get; set; }

    public string? Description { get; set; }

    public string? Icon { get; set; }

    public List<IFormFile>? Images { get; set; }

    public bool Hot { get; set; } = false;
    public List<string>? MenuIds { get; set; }
    
    public List<string>? ServiceIds { get; set; }
    
}