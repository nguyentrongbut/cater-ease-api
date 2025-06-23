namespace cater_ease_api.Dtos.Auth;

public class UpdateProfileDto
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Password { get; set; }
    public IFormFile? Avatar { get; set; }
}