namespace cater_ease_api.Dtos.Room;

public class UpdateRoomDto
{
    public string? Name { get; set; }
    public string? Area { get; set; }
    public int? People { get; set; }
    public int? Table { get; set; }
    public double? Price { get; set; }
    public IFormFile? Image { get; set; }
}