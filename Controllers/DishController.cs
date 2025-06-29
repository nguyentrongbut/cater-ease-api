using cater_ease_api.Data;
using cater_ease_api.Models;
using cater_ease_api.Dtos.Dish;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishController : ControllerBase
    {
        private readonly IMongoCollection<DishModel> _dishes;

        public DishController(MongoDbService mongoDbService)
        {
            _dishes = mongoDbService.Database.GetCollection<DishModel>("dishes");
        }

        // [POST] api/dish
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDishDto form)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var dish = new DishModel
            {
                Name = form.Name,
                Deleted = false
            };

            await _dishes.InsertOneAsync(dish);
            return Ok(dish);
        }

        // [GET] api/dish
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var dishes = await _dishes.Find(d => !d.Deleted).ToListAsync();
            return Ok(dishes);
        }

        // [GET] api/dish/:id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var dish = await _dishes.Find(d => d.Id == id && !d.Deleted).FirstOrDefaultAsync();
            return dish == null ? NotFound() : Ok(dish);
        }

        // [PATCH] api/dish/:id
        [Authorize(Roles = "admin")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id, [FromBody] UpdateDishDto dto)
        {
            var dish = await _dishes.Find(d => d.Id == id && !d.Deleted).FirstOrDefaultAsync();
            if (dish == null) return NotFound("Dish not found");

            if (string.IsNullOrEmpty(dto.Name))
                return BadRequest("No valid fields to update.");

            var update = Builders<DishModel>.Update.Set(d => d.Name, dto.Name);
            var result = await _dishes.UpdateOneAsync(d => d.Id == id, update);

            return result.ModifiedCount == 0
                ? NotFound("Update failed")
                : Ok("Updated dish successfully.");
        }

        // [DELETE] api/dish/:id
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var update = Builders<DishModel>.Update.Set(d => d.Deleted, true);
            var result = await _dishes.UpdateOneAsync(d => d.Id == id && !d.Deleted, update);

            return result.ModifiedCount == 0
                ? NotFound("Dish not found or already deleted")
                : Ok("Dish deleted successfully.");
        }
    }
}