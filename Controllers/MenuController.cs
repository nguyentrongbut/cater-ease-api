using cater_ease_api.Data;
using cater_ease_api.Dtos.Dish;
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
public class MenuController : ControllerBase
{
    private readonly IMongoCollection<MenuModel> _menus;
    private readonly IMongoCollection<DishModel> _dishes;
    private readonly IMongoCollection<CategoryModel> _categories;
    private readonly IMongoCollection<ReviewModel> _reviews;
    private readonly IMongoCollection<EventModel> _events;
    private readonly CloudinaryService _cloudinary;
    private readonly SlugHelper _slugHelper = new();

    public MenuController(MongoDbService mongoDbService, CloudinaryService cloudinary)
    {
        _menus = mongoDbService.Database.GetCollection<MenuModel>("menus");
        _dishes = mongoDbService.Database.GetCollection<DishModel>("dishes");
        _categories = mongoDbService.Database.GetCollection<CategoryModel>("categories");
        _reviews = mongoDbService.Database.GetCollection<ReviewModel>("reviews");
        _events = mongoDbService.Database.GetCollection<EventModel>("events");
        _cloudinary = cloudinary;
    }

    // [GET] api/menu
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = "asc",
        [FromQuery] string? eventName = null)
    {
        List<string> eventIds = null;

        if (!string.IsNullOrWhiteSpace(eventName))
        {
            var matchedEvents = await _events.Find(e => e.Title.ToLower().Contains(eventName.ToLower())).ToListAsync();
            if (!matchedEvents.Any())
                return Ok(new { data = new List<MenuDetailDto>(), total = 0, page, pageSize, totalPages = 0 });

            eventIds = matchedEvents.Select(e => e.Id).ToList();
        }

        var filter = eventIds != null
            ? Builders<MenuModel>.Filter.In(m => m.EventId, eventIds)
            : Builders<MenuModel>.Filter.Empty;

        var menus = await _menus.Find(filter).ToListAsync();

        var allDishIds = menus.SelectMany(m => m.DishIds).Distinct().ToList();
        var allDishes = await _dishes.Find(d => allDishIds.Contains(d.Id)).ToListAsync();
        var categoryIds = allDishes.Select(d => d.CategoryId).Distinct().ToList();
        var categories = await _categories.Find(c => categoryIds.Contains(c.Id)).ToListAsync();

        var result = new List<MenuDetailDto>();

        foreach (var menu in menus)
        {
            var dishes = allDishes.Where(d => menu.DishIds.Contains(d.Id)).ToList();

            var dishDtos = dishes.Select(d => new DishDetailDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                Price = d.Price,
                Image = d.Image,
                CategoryName = categories.FirstOrDefault(c => c.Id == d.CategoryId)?.Name ?? "Unknown"
            }).ToList();

            var reviews = await _reviews.Find(r => r.MenuId == menu.Id).ToListAsync();
            var total = reviews.Count;
            var avg = total > 0 ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;

            result.Add(new MenuDetailDto
            {
                Id = menu.Id,
                Slug = menu.Slug,
                Name = menu.Name,
                Description = menu.Description,
                Dishes = dishDtos,
                Image = menu.Image,
                Price = dishes.Sum(d => d.Price),
                AverageRating = avg,
                TotalReviews = total,
                CreatedAt = menu.CreatedAt
            });
        }

        // Sort
        if (!string.IsNullOrEmpty(sortBy))
        {
            result = sortBy.ToLower() switch
            {
                "price" => sortOrder == "asc"
                    ? result.OrderBy(m => m.Price).ToList()
                    : result.OrderByDescending(m => m.Price).ToList(),
                "rating" => sortOrder == "asc"
                    ? result.OrderBy(m => m.AverageRating).ToList()
                    : result.OrderByDescending(m => m.AverageRating).ToList(),
                "createdAt" => sortOrder == "asc"
                    ? result.OrderBy(m => m.CreatedAt).ToList()
                    : result.OrderByDescending(m => m.CreatedAt).ToList(),
                _ => result
            };
        }

        var pagedResult = result.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new
        {
            data = pagedResult,
            total = result.Count,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)result.Count / pageSize)
        });
    }

    // [GET] api/menu/:slug
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var menu = await _menus.Find(m => m.Slug == slug).FirstOrDefaultAsync();
        if (menu == null) return NotFound();

        var dishes = await _dishes.Find(d => menu.DishIds.Contains(d.Id)).ToListAsync();
        var categoryIds = dishes.Select(d => d.CategoryId).Distinct().ToList();
        var categories = await _categories.Find(c => categoryIds.Contains(c.Id)).ToListAsync();

        var dishDtos = dishes.Select(d => new DishDetailDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            Price = d.Price,
            Image = d.Image,
            CategoryName = categories.FirstOrDefault(c => c.Id == d.CategoryId)?.Name ?? "Unknown"
        }).ToList();

        var menuReviews = await _reviews.Find(r => r.MenuId == menu.Id).ToListAsync();
        var total = menuReviews.Count;
        var avg = total > 0 ? Math.Round(menuReviews.Average(r => r.Rating), 1) : 0;

        return Ok(new MenuDetailDto
        {
            Id = menu.Id,
            Slug = menu.Slug,
            Name = menu.Name,
            Description = menu.Description,
            Dishes = dishDtos,
            Image = menu.Image,
            Price = dishes.Sum(d => d.Price),
            AverageRating = avg,
            TotalReviews = total
        });
    }

    // [GET] api/menu/top-rated
    [HttpGet("top-rated")]
    public async Task<IActionResult> GetTop3RatedMenus()
    {
        var menus = await _menus.Find(_ => true).ToListAsync();

        var allDishIds = menus.SelectMany(m => m.DishIds).Distinct().ToList();
        var allDishes = await _dishes.Find(d => allDishIds.Contains(d.Id)).ToListAsync();
        var categoryIds = allDishes.Select(d => d.CategoryId).Distinct().ToList();
        var categories = await _categories.Find(c => categoryIds.Contains(c.Id)).ToListAsync();

        var result = new List<MenuDetailDto>();

        foreach (var menu in menus)
        {
            var dishes = allDishes.Where(d => menu.DishIds.Contains(d.Id)).ToList();

            var dishDtos = dishes.Select(d => new DishDetailDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                Price = d.Price,
                Image = d.Image,
                CategoryName = categories.FirstOrDefault(c => c.Id == d.CategoryId)?.Name ?? "Unknown"
            }).ToList();

            var reviews = await _reviews.Find(r => r.MenuId == menu.Id).ToListAsync();
            var total = reviews.Count;
            var avg = total > 0 ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;

            result.Add(new MenuDetailDto
            {
                Id = menu.Id,
                Slug = menu.Slug,
                Name = menu.Name,
                Description = menu.Description,
                Dishes = dishDtos,
                Image = menu.Image,
                Price = dishes.Sum(d => d.Price),
                AverageRating = avg,
                TotalReviews = total,
                CreatedAt = menu.CreatedAt
            });
        }

        var topMenus = result
            .OrderByDescending(m => m.AverageRating)
            .ThenByDescending(m => m.TotalReviews)
            .Take(3)
            .ToList();

        return Ok(topMenus);
    }

    // [POST] api/menu
    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateMenuDto form)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var slug = _slugHelper.GenerateSlug(form.Name);
        int counter = 1;

        while (await _menus.Find(m => m.Slug == slug).AnyAsync())
        {
            slug = $"{slug}-{counter++}";
        }

        var imageUrl = form.Image != null ? await _cloudinary.UploadAsync(form.Image) : null;

        var menu = new MenuModel
        {
            Name = form.Name,
            Description = form.Description,
            DishIds = form.DishIds,
            Image = imageUrl,
            EventId = form.EventId,
            Slug = slug,
            CreatedAt = DateTime.UtcNow
        };

        await _menus.InsertOneAsync(menu);
        return Ok(menu);
    }

    // [PATCH] api/menu/{id}
    [Authorize(Roles = "admin")]
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromForm] UpdateMenuDto dto)
    {
        var menu = await _menus.Find(m => m.Id == id).FirstOrDefaultAsync();
        if (menu == null) return NotFound("Menu not found");

        var updateDefs = new List<UpdateDefinition<MenuModel>>();

        if (!string.IsNullOrEmpty(dto.Name))
            updateDefs.Add(Builders<MenuModel>.Update.Set(m => m.Name, dto.Name));

        if (!string.IsNullOrEmpty(dto.Description))
            updateDefs.Add(Builders<MenuModel>.Update.Set(m => m.Description, dto.Description));

        if (dto.Image != null)
        {
            if (!string.IsNullOrEmpty(menu.Image))
                await _cloudinary.DeleteAsync(menu.Image);

            var imageUrl = await _cloudinary.UploadAsync(dto.Image);
            updateDefs.Add(Builders<MenuModel>.Update.Set(m => m.Image, imageUrl));
        }
        
        var dishIds = menu.DishIds != null ? new List<string>(menu.DishIds) : new List<string>();

        if (dto.AddDishIds != null && dto.AddDishIds.Any())
            dishIds.AddRange(dto.AddDishIds);

        if (dto.RemoveDishIds != null && dto.RemoveDishIds.Any())
            dishIds = dishIds.Except(dto.RemoveDishIds).ToList();

        dishIds = dishIds.Distinct().ToList();

        updateDefs.Add(Builders<MenuModel>.Update.Set(m => m.DishIds, dishIds));

        if (!updateDefs.Any()) return BadRequest("No valid fields to update.");

        var update = Builders<MenuModel>.Update.Combine(updateDefs);
        var result = await _menus.UpdateOneAsync(m => m.Id == id, update);

        return result.MatchedCount == 0
            ? NotFound("Update failed")
            : Ok("Updated menu successfully.");
    }
}