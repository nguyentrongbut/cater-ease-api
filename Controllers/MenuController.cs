using cater_ease_api.Data;
using cater_ease_api.Dtos.Dish;
using cater_ease_api.Dtos.Menu;
using cater_ease_api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MenuController : ControllerBase
{
    private readonly IMongoCollection<MenuModel> _menus;
    private readonly IMongoCollection<DishModel> _dishes;

    public MenuController(MongoDbService mongoDbService)
    {
        _menus = mongoDbService.Database.GetCollection<MenuModel>("menus");
        _dishes = mongoDbService.Database.GetCollection<DishModel>("dishes");
    }

    // [GET] api/menu
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var menus = await _menus.Find(m => !m.Deleted).ToListAsync();
        var result = new List<MenuWithDishesDto>();

        foreach (var menu in menus)
        {
            var dishes = await _dishes.Find(d => menu.DishIds.Contains(d.Id) && !d.Deleted).ToListAsync();
            var dishDtos = dishes.Select(d => new DishDetailDto
            {
                Id = d.Id,
                Name = d.Name
            }).ToList();

            result.Add(new MenuWithDishesDto
            {
                Id = menu.Id,
                Name = menu.Name,
                Price = menu.Price,
                Dishes = dishDtos
            });
        }

        return Ok(result);
    }

    // [GET] api/menu/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var menu = await _menus.Find(m => m.Id == id && !m.Deleted).FirstOrDefaultAsync();
        if (menu == null) return NotFound();

        var dishes = await _dishes.Find(d => menu.DishIds.Contains(d.Id) && !d.Deleted).ToListAsync();
        var dishDtos = dishes.Select(d => new DishDetailDto
        {
            Id = d.Id,
            Name = d.Name
        }).ToList();

        var dto = new MenuWithDishesDto
        {
            Id = menu.Id,
            Name = menu.Name,
            Price = menu.Price,
            Dishes = dishDtos
        };

        return Ok(dto);
    }

    // [POST] api/menu
    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMenuDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var menu = new MenuModel
        {
            Name = dto.Name,
            DishIds = dto.DishIds ?? new(),
            Price = dto.Price,
            Deleted = false
        };

        await _menus.InsertOneAsync(menu);
        return Ok(menu);
    }

    // [PATCH] api/menu/{id}
    [Authorize(Roles = "admin")]
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateMenuDto dto)
    {
        var menu = await _menus.Find(m => m.Id == id && !m.Deleted).FirstOrDefaultAsync();
        if (menu == null) return NotFound("Menu not found");

        var updates = new List<UpdateDefinition<MenuModel>>();
        bool hasChanges = false;

        if (!string.IsNullOrEmpty(dto.Name))
        {
            updates.Add(Builders<MenuModel>.Update.Set(m => m.Name, dto.Name));
            hasChanges = true;
        }

        if (dto.DishIds != null)
        {
            updates.Add(Builders<MenuModel>.Update.Set(m => m.DishIds, dto.DishIds));
            hasChanges = true;
        }

        if (dto.Price.HasValue)
        {
            updates.Add(Builders<MenuModel>.Update.Set(m => m.Price, dto.Price.Value));
            hasChanges = true;
        }

        if (!hasChanges) return BadRequest("No changes provided");

        var result = await _menus.UpdateOneAsync(
            m => m.Id == id,
            Builders<MenuModel>.Update.Combine(updates)
        );

        return result.ModifiedCount > 0 ? Ok("Updated") : Ok("No changes made");
    }

    // [DELETE] api/menu/{id}
    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var update = Builders<MenuModel>.Update.Set(m => m.Deleted, true);
        var result = await _menus.UpdateOneAsync(m => m.Id == id && !m.Deleted, update);
        return result.ModifiedCount == 0
            ? NotFound("Menu not found or already deleted.")
            : Ok("Deleted menu successfully.");
    }
}
