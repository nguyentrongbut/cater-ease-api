using cater_ease_api.Data;
using cater_ease_api.Dtos.Dish;
using cater_ease_api.Dtos.Event;
using cater_ease_api.Dtos.Menu;
using cater_ease_api.Models;
using cater_ease_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Slugify;

namespace cater_ease_api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EventController : ControllerBase
{
    private readonly IMongoCollection<EventModel> _events;
    private readonly IMongoCollection<MenuModel> _menus;
    private readonly IMongoCollection<DishModel> _dishes;
    private readonly IMongoCollection<ServiceModel> _services;
    private readonly CloudinaryService _cloudinary;
    private readonly SlugHelper _slugHelper = new();

    public EventController(MongoDbService mongoDbService, CloudinaryService cloudinary)
    {
        _events = mongoDbService.Database.GetCollection<EventModel>("events");
        _menus = mongoDbService.Database.GetCollection<MenuModel>("menus");
        _dishes = mongoDbService.Database.GetCollection<DishModel>("dishes");
        _services = mongoDbService.Database.GetCollection<ServiceModel>("services");
        _cloudinary = cloudinary;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var events = await _events.Find(e => !e.Deleted).ToListAsync();
        var allMenuIds = events.SelectMany(e => e.MenuIds).Distinct().ToList();
        var menus = await _menus.Find(m => allMenuIds.Contains(m.Id) && !m.Deleted).ToListAsync();
        var dishIds = menus.SelectMany(m => m.DishIds).Distinct().ToList();
        var dishes = await _dishes.Find(d => dishIds.Contains(d.Id) && !d.Deleted).ToListAsync();
        var allServiceIds = events.SelectMany(e => e.ServiceIds).Distinct().ToList();
        var allServices = await _services.Find(s => allServiceIds.Contains(s.Id) && !s.Deleted).ToListAsync();

        var result = events.Select(ev =>
        {
            var menuDtos = menus
                .Where(m => ev.MenuIds.Contains(m.Id))
                .Select(menu =>
                {
                    var dishDtos = dishes
                        .Where(d => menu.DishIds.Contains(d.Id))
                        .Select(d => new DishDetailDto { Id = d.Id, Name = d.Name })
                        .ToList();

                    return new MenuDetailDto
                    {
                        Id = menu.Id,
                        Name = menu.Name,
                        Price = menu.Price,
                        Dishes = dishDtos
                    };
                }).ToList();

            var serviceDtos = allServices
                .Where(s => ev.ServiceIds.Contains(s.Id))
                .ToList();

            return new
            {
                ev.Id,
                ev.Name,
                ev.SubName,
                ev.Slug,
                ev.Description,
                ev.Icon,
                ev.Images,
                ev.Hot,
                Menus = menuDtos,
                Services = serviceDtos
            };
        });

        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var ev = await _events.Find(e => e.Slug == slug && !e.Deleted).FirstOrDefaultAsync();
        if (ev == null) return NotFound("Event not found");

        var menus = await _menus.Find(m => ev.MenuIds.Contains(m.Id) && !m.Deleted).ToListAsync();
        var dishIds = menus.SelectMany(m => m.DishIds).Distinct().ToList();
        var dishes = await _dishes.Find(d => dishIds.Contains(d.Id) && !d.Deleted).ToListAsync();
        var services = await _services.Find(s => ev.ServiceIds.Contains(s.Id) && !s.Deleted).ToListAsync();
        

        var menuDtos = menus.Select(menu =>
        {
            var dishDtos = dishes
                .Where(d => menu.DishIds.Contains(d.Id))
                .Select(d => new DishDetailDto { Id = d.Id, Name = d.Name })
                .ToList();

            return new MenuDetailDto
            {
                Id = menu.Id,
                Name = menu.Name,
                Price = menu.Price,
                Dishes = dishDtos
            };
        }).ToList();

        return Ok(new
        {
            eventInfo = new
            {
                ev.Id,
                ev.Name,
                ev.SubName,
                ev.Slug,
                ev.Description,
                ev.Icon,
                ev.Images,
                ev.Hot
            },
            Menus = menuDtos,
            Services = services
        });
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateEventDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var slug = _slugHelper.GenerateSlug(dto.Name);
        int counter = 1;
        while (await _events.Find(e => e.Slug == slug).AnyAsync())
        {
            slug = $"{slug}-{counter++}";
        }

        var images = new List<string>();
        if (dto.Images != null && dto.Images.Any())
        {
            foreach (var image in dto.Images)
            {
                var url = await _cloudinary.UploadAsync(image);
                images.Add(url);
            }
        }

        var newEvent = new EventModel
        {
            Name = dto.Name,
            SubName = dto.SubName,
            Icon = dto.Icon,
            Slug = slug,
            Description = dto.Description,
            MenuIds = dto.MenuIds ?? new List<string>(),
            ServiceIds = dto.ServiceIds ?? new List<string>(),
            Images = images,
            Hot = dto.Hot,
            Deleted = false
        };

        await _events.InsertOneAsync(newEvent);
        return Ok(newEvent);
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(string id, [FromForm] UpdateEventDto dto)
    {
        var ev = await _events.Find(e => e.Id == id).FirstOrDefaultAsync();
        if (ev == null) return NotFound("Event not found");

        var updates = new List<UpdateDefinition<EventModel>>();
        var hasChanges = false;

        if (!string.IsNullOrEmpty(dto.Name))
        {
            var slug = _slugHelper.GenerateSlug(dto.Name);
            updates.Add(Builders<EventModel>.Update.Set(e => e.Name, dto.Name));
            updates.Add(Builders<EventModel>.Update.Set(e => e.Slug, slug));
            hasChanges = true;
        }

        if (!string.IsNullOrEmpty(dto.SubName))
        {
            updates.Add(Builders<EventModel>.Update.Set(e => e.SubName, dto.SubName));
            hasChanges = true;
        }

        if (!string.IsNullOrEmpty(dto.Icon))
        {
            updates.Add(Builders<EventModel>.Update.Set(e => e.Icon, dto.Icon));
            hasChanges = true;
        }

        if (!string.IsNullOrEmpty(dto.Description))
        {
            updates.Add(Builders<EventModel>.Update.Set(e => e.Description, dto.Description));
            hasChanges = true;
        }

        var currentImages = ev.Images.ToList();
        if (dto.RemoveImages != null)
        {
            foreach (var url in dto.RemoveImages)
            {
                await _cloudinary.DeleteAsync(url);
            }
            currentImages = currentImages.Except(dto.RemoveImages).ToList();
            hasChanges = true;
        }

        if (dto.AddImages != null)
        {
            foreach (var file in dto.AddImages)
            {
                var uploaded = await _cloudinary.UploadAsync(file);
                currentImages.Add(uploaded);
            }
            hasChanges = true;
        }

        if (hasChanges)
        {
            updates.Add(Builders<EventModel>.Update.Set(e => e.Images, currentImages.Distinct().ToList()));
        }

        if (dto.Hot.HasValue)
        {
            updates.Add(Builders<EventModel>.Update.Set(e => e.Hot, dto.Hot.Value));
            hasChanges = true;
        }

        var menuIds = ev.MenuIds.ToList();
        if (dto.AddMenuIds != null)
        {
            menuIds.AddRange(dto.AddMenuIds.Except(menuIds));
            hasChanges = true;
        }
        if (dto.RemoveMenuIds != null)
        {
            menuIds = menuIds.Except(dto.RemoveMenuIds).ToList();
            hasChanges = true;
        }
        updates.Add(Builders<EventModel>.Update.Set(e => e.MenuIds, menuIds.Distinct().ToList()));

        var serviceIds = ev.ServiceIds.ToList();
        if (dto.AddServiceIds != null)
        {
            serviceIds.AddRange(dto.AddServiceIds.Except(serviceIds));
            hasChanges = true;
        }
        if (dto.RemoveServiceIds != null)
        {
            serviceIds = serviceIds.Except(dto.RemoveServiceIds).ToList();
            hasChanges = true;
        }
        updates.Add(Builders<EventModel>.Update.Set(e => e.ServiceIds, serviceIds.Distinct().ToList()));

        if (!hasChanges) return BadRequest("No changes detected.");

        var result = await _events.UpdateOneAsync(e => e.Id == id, Builders<EventModel>.Update.Combine(updates));
        return result.ModifiedCount > 0 ? Ok("Event updated.") : Ok("No changes made.");
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var ev = await _events.Find(e => e.Id == id).FirstOrDefaultAsync();
        if (ev == null) return NotFound("Event not found");

        foreach (var img in ev.Images)
        {
            await _cloudinary.DeleteAsync(img);
        }

        var update = Builders<EventModel>.Update.Set(e => e.Deleted, true);
        var result = await _events.UpdateOneAsync(e => e.Id == id, update);

        return result.ModifiedCount > 0 ? Ok("Deleted successfully.") : NotFound("Delete failed.");
    }
}
