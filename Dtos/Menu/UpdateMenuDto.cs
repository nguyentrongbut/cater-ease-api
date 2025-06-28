namespace cater_ease_api.Dtos.Menu;

public class UpdateMenuDto
{
    public string? Name { get; set; }

    public List<string>? AddDishIds { get; set; }

    public List<string>? RemoveDishIds { get; set; }
}