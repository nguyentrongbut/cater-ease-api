using System.Globalization;
using System.Text.Json;
using cater_ease_api.Data;
using cater_ease_api.Models;
using cater_ease_api.Dtos.Dish;
using cater_ease_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace cater_ease_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishController : ControllerBase
    {
        private readonly IMongoCollection<DishModel> _dishes;
        private readonly CloudinaryService _cloudinary;
        private readonly IMongoCollection<CategoryModel> _categories;

        public DishController(MongoDbService mongoDbService, CloudinaryService cloudinary)
        {
            _dishes = mongoDbService.Database.GetCollection<DishModel>("dishes");
            _categories = mongoDbService.Database.GetCollection<CategoryModel>("categories");
            _cloudinary = cloudinary;
        }

        // [POST] api/dish
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateDishDto form)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            string? imageUrl = null;
            if (form.Image != null)
                imageUrl = await _cloudinary.UploadAsync(form.Image);

            var dish = new DishModel
            {
                Name = form.Name,
                Description = form.Description,
                Price = form.Price,
                CategoryId = form.CategoryId,
                Image = imageUrl,
            };

            await _dishes.InsertOneAsync(dish);
            return Ok(dish);
        }

        // [GET] api/dish
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var dishes = await _dishes.Find(_ => true).ToListAsync();

            var categoryIds = dishes.Select(d => d.CategoryId).Distinct().ToList();
            var categories = await _categories.Find(c => categoryIds.Contains(c.Id)).ToListAsync();

            var result = dishes.Select(dish =>
            {
                var categoryName = categories.FirstOrDefault(c => c.Id == dish.CategoryId)?.Name ?? "Unknown";
                return new DishDetailDto
                {
                    Id = dish.Id,
                    Name = dish.Name,
                    Description = dish.Description,
                    Price = dish.Price,
                    Image = dish.Image,
                    CategoryName = categoryName
                };
            }).ToList();

            return Ok(result);
        }

        // [GET] api/dish/:id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var dish = await _dishes.Find(d => d.Id == id).FirstOrDefaultAsync();
            if (dish == null) return NotFound();

            var category = await _categories.Find(c => c.Id == dish.CategoryId).FirstOrDefaultAsync();
            var categoryName = category?.Name ?? "Unknown";

            var result = new DishDetailDto
            {
                Id = dish.Id,
                Name = dish.Name,
                Description = dish.Description,
                Price = dish.Price,
                Image = dish.Image,
                CategoryName = categoryName
            };

            return Ok(result);
        }

        // [PATCH] api/dish/:id
        [Authorize(Roles = "admin")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id, [FromBody] UpdateDishDto dto)
        {
            var updateDefs = new List<UpdateDefinition<DishModel>>();

            if (!string.IsNullOrEmpty(dto.Name))
                updateDefs.Add(Builders<DishModel>.Update.Set(d => d.Name, dto.Name));

            if (!string.IsNullOrEmpty(dto.Description))
                updateDefs.Add(Builders<DishModel>.Update.Set(d => d.Description, dto.Description));

            if (dto.Price.HasValue)
                updateDefs.Add(Builders<DishModel>.Update.Set(d => d.Price, dto.Price.Value));

            if (!string.IsNullOrEmpty(dto.Image))
                updateDefs.Add(Builders<DishModel>.Update.Set(d => d.Image, dto.Image));

            if (!string.IsNullOrEmpty(dto.CategoryId))
                updateDefs.Add(Builders<DishModel>.Update.Set(d => d.CategoryId, dto.CategoryId));

            if (!updateDefs.Any())
                return BadRequest("No valid fields to update.");

            var update = Builders<DishModel>.Update.Combine(updateDefs);
            var result = await _dishes.UpdateOneAsync(d => d.Id == id, update);

            return result.ModifiedCount == 0 ? NotFound() : Ok("Updated dish successfully.");
        }

        // [DELETE] api/dish/:id
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var dish = await _dishes.Find(d => d.Id == id).FirstOrDefaultAsync();
            if (dish == null) return NotFound();

            if (!string.IsNullOrEmpty(dish.Image))
                await _cloudinary.DeleteAsync(dish.Image);

            var result = await _dishes.DeleteOneAsync(d => d.Id == id);
            return result.DeletedCount == 0 ? NotFound() : Ok("Deleted dish successfully.");
        }
    }
}