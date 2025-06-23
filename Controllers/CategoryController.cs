using cater_ease_api.Data;
using cater_ease_api.Models;
using cater_ease_api.Dtos.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly IMongoCollection<CategoryModel> _categories;

        public CategoryController(MongoDbService mongoDbService)
        {
            _categories = mongoDbService.Database.GetCollection<CategoryModel>("categories");
        }

        // [GET] api/category
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categories.Find(_ => true).ToListAsync();
            var result = categories.Select(c => new CategoryDetailDto
            {
                Id = c.Id,
                Name = c.Name
            }).ToList();
            return Ok(result);
        }

        // [GET] api/category/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var category = await _categories.Find(c => c.Id == id).FirstOrDefaultAsync();
            if (category == null) return NotFound();

            var result = new CategoryDetailDto
            {
                Id = category.Id,
                Name = category.Name
            };
            return Ok(result);
        }

        // [POST] api/category
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var category = new CategoryModel { Name = dto.Name };
            await _categories.InsertOneAsync(category);

            var result = new CategoryDetailDto
            {
                Id = category.Id,
                Name = category.Name
            };
            return Ok(result);
        }

        // [PATCH] api/category/{id}
        [Authorize(Roles = "admin")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateCategoryDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var update = Builders<CategoryModel>.Update.Set(c => c.Name, dto.Name);
            var result = await _categories.UpdateOneAsync(c => c.Id == id, update);

            return result.MatchedCount == 0 ? NotFound() : Ok("Updated category successfully.");
        }

        // [DELETE] api/category/{id}
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _categories.DeleteOneAsync(c => c.Id == id);
            return result.DeletedCount == 0 ? NotFound() : Ok("Deleted category successfully.");
        }
    }
}