namespace cater_ease_api.Dtos.Event;

public class UpdateEventDto
{
    public string? Name { get; set; }

    public string? SubName { get; set; }

    public string? Description { get; set; }

    public string? Icon { get; set; }
    
    public List<IFormFile>? AddImages { get; set; }   
    public List<string>? RemoveImages { get; set; }

    public bool? Hot { get; set; }

    public List<string>? AddMenuIds { get; set; }

    public List<string>? RemoveMenuIds { get; set; }
    
    public List<string>? AddServiceIds { get; set; }         
    public List<string>? RemoveServiceIds { get; set; }
}