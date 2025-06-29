using cater_ease_api.Data;
using cater_ease_api.Models;
using cater_ease_api.Dtos.Service;
using cater_ease_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ServiceController : ControllerBase
{
    private readonly IMongoCollection<ServiceModel> _services;
    private readonly CloudinaryService _cloudinary;

    public ServiceController(MongoDbService mongoDbService, CloudinaryService cloudinary)
    {
        _services = mongoDbService.Database.GetCollection<ServiceModel>("services");
        _cloudinary = cloudinary;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var services = await _services.Find(s => !s.Deleted).ToListAsync();
        return Ok(services);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var service = await _services.Find(s => s.Id == id && !s.Deleted).FirstOrDefaultAsync();
        return service == null ? NotFound("Service not found") : Ok(service);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateServiceDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var images = await UploadImages(dto.Images);

        var service = new ServiceModel
        {
            Name = dto.Name,
            Price = dto.Price,
            Description = dto.Description,
            Icon = dto.Icon,
            Images = images,
            Deleted = false
        };

        await _services.InsertOneAsync(service);
        return Ok(service);
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromForm] UpdateServiceDto dto)
    {
        var service = await _services.Find(s => s.Id == id && !s.Deleted).FirstOrDefaultAsync();
        if (service == null) return NotFound("Service not found");

        var updates = new List<UpdateDefinition<ServiceModel>>();

        if (!string.IsNullOrEmpty(dto.Name))
            updates.Add(Builders<ServiceModel>.Update.Set(s => s.Name, dto.Name));

        if (dto.Price.HasValue)
            updates.Add(Builders<ServiceModel>.Update.Set(s => s.Price, dto.Price.Value));

        if (!string.IsNullOrEmpty(dto.Description))
            updates.Add(Builders<ServiceModel>.Update.Set(s => s.Description, dto.Description));

        if (!string.IsNullOrEmpty(dto.Icon))
            updates.Add(Builders<ServiceModel>.Update.Set(s => s.Icon, dto.Icon));

        if (dto.RemoveImages != null && dto.RemoveImages.Any())
        {
            foreach (var url in dto.RemoveImages)
                await _cloudinary.DeleteAsync(url);

            service.Images = service.Images.Except(dto.RemoveImages).ToList();
        }

        if (dto.AddImages != null && dto.AddImages.Any())
        {
            var added = await UploadImages(dto.AddImages);
            service.Images.AddRange(added);
        }

        updates.Add(Builders<ServiceModel>.Update.Set(s => s.Images, service.Images.Distinct().ToList()));

        if (!updates.Any()) return BadRequest("No data to update.");

        var result = await _services.UpdateOneAsync(s => s.Id == id, Builders<ServiceModel>.Update.Combine(updates));
        return result.ModifiedCount > 0 ? Ok("Service updated.") : Ok("No changes made.");
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var service = await _services.Find(s => s.Id == id && !s.Deleted).FirstOrDefaultAsync();
        if (service == null) return NotFound("Service not found");

        // Soft delete
        var result = await _services.UpdateOneAsync(
            s => s.Id == id,
            Builders<ServiceModel>.Update.Set(s => s.Deleted, true)
        );

        return result.ModifiedCount == 0 ? NotFound("Delete failed") : Ok("Service soft deleted.");
    }

    private async Task<List<string>> UploadImages(List<IFormFile>? files)
    {
        var urls = new List<string>();
        if (files == null) return urls;

        foreach (var file in files)
        {
            var url = await _cloudinary.UploadAsync(file);
            urls.Add(url);
        }

        return urls;
    }
}
