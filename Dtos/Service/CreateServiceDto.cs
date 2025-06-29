using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Service;

public class CreateServiceDto
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    [Range(0, double.MaxValue)]
    public double Price { get; set; }

    public string? Description { get; set; }
    
    public string? Icon { get; set; }

    public List<IFormFile>? Images { get; set; }
}