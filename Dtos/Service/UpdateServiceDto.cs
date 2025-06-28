using Microsoft.AspNetCore.Http;

namespace cater_ease_api.Dtos.Service;

public class UpdateServiceDto
{
    public string? Name { get; set; }

    public double? Price { get; set; }

    public string? Description { get; set; }

    public List<IFormFile>? AddImages { get; set; }

    public List<string>? RemoveImages { get; set; }
}