using cater_ease_api.Data;
using cater_ease_api.Dtos.Event;
using cater_ease_api.Dtos.Menu;
using cater_ease_api.Dtos.Dish;
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
    private readonly CloudinaryService _cloudinary;
    private readonly SlugHelper _slugHelper = new();

    public EventController(MongoDbService mongoDbService, CloudinaryService cloudinary)
    {
        _events = mongoDbService.Database.GetCollection<EventModel>("events");
        _menus = mongoDbService.Database.GetCollection<MenuModel>("menus");
        _dishes = mongoDbService.Database.GetCollection<DishModel>("dishes");
        _cloudinary = cloudinary;
    }

    [HttpGet]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var events = await _events.Find(_ => true).ToListAsync();
        var allMenuIds = events.SelectMany(e => e.MenuIds).Distinct().ToList();
        var menus = await _menus.Find(m => allMenuIds.Contains(m.Id)).ToListAsync();
        var dishIds = menus.SelectMany(m => m.DishIds).Distinct().ToList();
        var dishes = await _dishes.Find(d => dishIds.Contains(d.Id)).ToListAsync();

        var result = events.Select(ev =>
        {
            var eventMenus = menus.Where(m => ev.MenuIds.Contains(m.Id)).ToList();

            var menuDtos = eventMenus.Select(menu =>
            {
                var menuDishes = dishes.Where(d => menu.DishIds.Contains(d.Id)).ToList();
                var dishDtos = menuDishes.Select(d => new DishDetailDto
                {
                    Id = d.Id,
                    Name = d.Name,
                }).ToList();

                return new MenuDetailDto
                {
                    Id = menu.Id,
                    Name = menu.Name,
                    Dishes = dishDtos,
                };
            }).ToList();

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
                Menus = menuDtos
            };
        });

        return Ok(result);
    }


    [Authorize(Roles = "admin")]
    [HttpGet("by-id/{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var ev = await _events.Find(e => e.Id == id).FirstOrDefaultAsync();
        if (ev == null) return NotFound();

        var menus = await _menus.Find(m => ev.MenuIds.Contains(m.Id)).ToListAsync();
        var dishIds = menus.SelectMany(m => m.DishIds).Distinct().ToList();
        var dishes = await _dishes.Find(d => dishIds.Contains(d.Id)).ToListAsync();

        var resultMenus = menus.Select(menu =>
        {
            var dishList = dishes.Where(d => menu.DishIds.Contains(d.Id)).ToList();
            var dishDtos = dishList.Select(d => new DishDetailDto
            {
                Id = d.Id,
                Name = d.Name,
            }).ToList();

            return new MenuDetailDto
            {
                Id = menu.Id,
                Name = menu.Name,
                Dishes = dishDtos,
            };
        }).ToList();

        return Ok(new
        {
            ev.Id,
            ev.Name,
            ev.SubName,
            ev.Slug,
            ev.Description,
            ev.Icon,
            ev.Images,
            ev.Hot,
            Menus = resultMenus
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

        var model = new EventModel
        {
            Name = dto.Name,
            SubName = dto.SubName,
            Icon = dto.Icon,
            Slug = slug,
            Description = dto.Description,
            MenuIds = dto.MenuIds ?? new List<string>(),
            ServiceIds = dto.ServiceIds ?? new List<string>(),
            Images = images,
            Hot = dto.Hot
        };

        await _events.InsertOneAsync(model);
        return Ok(model);
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(string id, [FromForm] UpdateEventDto dto)
    {
        var eventObj = await _events.Find(e => e.Id == id).FirstOrDefaultAsync();
        if (eventObj == null) return NotFound("Event not found");

        var updates = new List<UpdateDefinition<EventModel>>();
        bool hasChanges = false;

        // Cập nhật tên & slug
        if (!string.IsNullOrEmpty(dto.Name))
        {
            updates.Add(Builders<EventModel>.Update.Set(e => e.Name, dto.Name));
            var slug = _slugHelper.GenerateSlug(dto.Name);
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

        // Xử lý ảnh
        var currentImages = eventObj.Images ?? new List<string>();

        if (dto.RemoveImages != null && dto.RemoveImages.Any())
        {
            foreach (var url in dto.RemoveImages)
            {
                await _cloudinary.DeleteAsync(url); // Không cần gán kết quả nếu phương thức không trả giá trị
            }

            currentImages = currentImages.Except(dto.RemoveImages).ToList();
            hasChanges = true;
        }

        if (dto.AddImages != null && dto.AddImages.Any())
        {
            foreach (var file in dto.AddImages)
            {
                var uploadedUrl = await _cloudinary.UploadAsync(file);
                currentImages.Add(uploadedUrl);
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

        // Xử lý MenuIds
        var updatedMenus = eventObj.MenuIds.ToList();

        if (dto.AddMenuIds != null && dto.AddMenuIds.Any())
        {
            var added = dto.AddMenuIds.Except(updatedMenus).ToList();
            if (added.Any())
            {
                updatedMenus.AddRange(added);
                hasChanges = true;
            }
        }

        if (dto.RemoveMenuIds != null && dto.RemoveMenuIds.Any())
        {
            var removed = updatedMenus.Intersect(dto.RemoveMenuIds).ToList();
            if (removed.Any())
            {
                updatedMenus = updatedMenus.Except(removed).ToList();
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            updates.Add(Builders<EventModel>.Update.Set(e => e.MenuIds, updatedMenus.Distinct().ToList()));
        }

        if (!hasChanges)
        {
            return BadRequest("No valid fields provided or no data changed.");
        }

        // Xử lý ServiceIds
        var updatedServices = eventObj.ServiceIds.ToList();

        if (dto.AddServiceIds != null && dto.AddServiceIds.Any())
        {
            var added = dto.AddServiceIds.Except(updatedServices).ToList();
            if (added.Any())
            {
                updatedServices.AddRange(added);
                hasChanges = true;
            }
        }

        if (dto.RemoveServiceIds != null && dto.RemoveServiceIds.Any())
        {
            var removed = updatedServices.Intersect(dto.RemoveServiceIds).ToList();
            if (removed.Any())
            {
                updatedServices = updatedServices.Except(removed).ToList();
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            updates.Add(Builders<EventModel>.Update.Set(e => e.ServiceIds, updatedServices.Distinct().ToList()));
        }


        var update = Builders<EventModel>.Update.Combine(updates);
        var result = await _events.UpdateOneAsync(e => e.Id == id, update);

        return result.ModifiedCount == 0
            ? Ok("No actual changes were made to the event.")
            : Ok("Event updated successfully.");
    }


    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        // Lấy event từ DB trước
        var eventObj = await _events.Find(e => e.Id == id).FirstOrDefaultAsync();
        if (eventObj == null)
            return NotFound("Event not found");

        // Nếu có ảnh, tiến hành xóa trên Cloudinary
        if (eventObj.Images != null && eventObj.Images.Any())
        {
            foreach (var imageUrl in eventObj.Images)
            {
                await _cloudinary.DeleteAsync(imageUrl);
            }
        }

        // Xóa khỏi DB
        var result = await _events.DeleteOneAsync(e => e.Id == id);
        return result.DeletedCount == 0
            ? NotFound("Delete failed")
            : Ok("Deleted event and related images successfully.");
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var eventObj = await _events.Find(e => e.Slug == slug).FirstOrDefaultAsync();
        if (eventObj == null) return NotFound("Event not found");

        var menuList = await _menus.Find(m => eventObj.MenuIds.Contains(m.Id)).ToListAsync();
        var allDishIds = menuList.SelectMany(m => m.DishIds).Distinct().ToList();
        var allDishes = await _dishes.Find(d => allDishIds.Contains(d.Id)).ToListAsync();

        var resultMenus = menuList.Select(menu =>
        {
            var dishes = allDishes.Where(d => menu.DishIds.Contains(d.Id)).ToList();
            var dishDtos = dishes.Select(d => new DishDetailDto
            {
                Id = d.Id,
                Name = d.Name,
            }).ToList();

            return new MenuDetailDto
            {
                Id = menu.Id,
                Name = menu.Name,
                Dishes = dishDtos,
            };
        }).ToList();

        return Ok(new
        {
            eventInfo = new
            {
                eventObj.Id,
                eventObj.Name,
                eventObj.SubName,
                eventObj.Slug,
                eventObj.Description,
                eventObj.Icon,
                eventObj.Images,
                eventObj.Hot
            },
            menus = resultMenus
        });
    }
}