using cater_ease_api.Data;
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

    public MenuController(MongoDbService mongoDbService)
    {
        _menus = mongoDbService.Database.GetCollection<MenuModel>("menus");
    }

    // [GET] api/menu
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var menus = await _menus.Find(_ => true).ToListAsync();
        return Ok(menus);
    }

    // [GET] api/menu/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var menu = await _menus.Find(m => m.Id == id).FirstOrDefaultAsync();
        return menu == null ? NotFound() : Ok(menu);
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
            DishIds = dto.DishIds
        };

        await _menus.InsertOneAsync(menu);
        return Ok(menu);
    }

    // [PATCH] api/menu/{id}
    [Authorize(Roles = "admin")]
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateMenuDto dto)
    {
        var updateDefs = new List<UpdateDefinition<MenuModel>>();

        if (!string.IsNullOrEmpty(dto.Name))
            updateDefs.Add(Builders<MenuModel>.Update.Set(m => m.Name, dto.Name));

        if (dto.AddDishIds != null && dto.AddDishIds.Any())
        {
            var menu = await _menus.Find(m => m.Id == id).FirstOrDefaultAsync();
            if (menu == null) return NotFound();

            var updatedDishIds = menu.DishIds.Union(dto.AddDishIds).Distinct().ToList();
            updateDefs.Add(Builders<MenuModel>.Update.Set(m => m.DishIds, updatedDishIds));
        }

        if (dto.RemoveDishIds != null && dto.RemoveDishIds.Any())
        {
            var menu = await _menus.Find(m => m.Id == id).FirstOrDefaultAsync();
            if (menu == null) return NotFound();

            var updatedDishIds = menu.DishIds.Except(dto.RemoveDishIds).ToList();
            updateDefs.Add(Builders<MenuModel>.Update.Set(m => m.DishIds, updatedDishIds));
        }

        if (!updateDefs.Any()) return BadRequest("No valid fields to update.");

        var update = Builders<MenuModel>.Update.Combine(updateDefs);
        var result = await _menus.UpdateOneAsync(m => m.Id == id, update);

        return result.MatchedCount == 0
            ? NotFound("Update failed")
            : Ok("Updated menu successfully.");
    }

    // [DELETE] api/menu/{id}
    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _menus.DeleteOneAsync(m => m.Id == id);
        return result.DeletedCount == 0
            ? NotFound()
            : Ok("Deleted menu successfully.");
    }
}
