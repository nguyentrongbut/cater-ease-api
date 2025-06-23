using cater_ease_api.Data;
using cater_ease_api.Dtos.Cuisine;
using cater_ease_api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CuisineController : ControllerBase
    {
        private readonly IMongoCollection<CuisineModel> _cuisines;

        public CuisineController(MongoDbService mongoDbService)
        {
            _cuisines = mongoDbService.Database.GetCollection<CuisineModel>("cuisines");
        }
        
        // [GET] api/cuisine
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _cuisines.Find(_ => true).ToListAsync();
            return Ok(data);
        }

        // [GET] api/cuisine/:id
        [Authorize(Roles = "admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var dish = await _cuisines.Find(d => d.Id == id).FirstOrDefaultAsync();
            return dish == null ? NotFound() : Ok(dish);
        }
        
        // [POST] api/cuisine
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCuisineDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var model = new CuisineModel
            {
                Title = dto.Title,
                Description = dto.Description,
            };

            await _cuisines.InsertOneAsync(model);
            return Ok(model);
        }

        // [PATCH] api/cuisine/:id
        [Authorize(Roles = "admin")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id, [FromBody] UpdateCuisineDto dto)
        {
            var updates = new List<UpdateDefinition<CuisineModel>>();
    
            if (dto.Title != null)
                updates.Add(Builders<CuisineModel>.Update.Set(e => e.Title, dto.Title));

            if (dto.Description != null)
                updates.Add(Builders<CuisineModel>.Update.Set(e => e.Description, dto.Description));

            if (updates.Count == 0)
                return BadRequest("No valid fields provided.");

            var update = Builders<CuisineModel>.Update.Combine(updates);
            var result = await _cuisines.UpdateOneAsync(e => e.Id == id, update);

            return result.MatchedCount == 0
                ? NotFound()
                : Ok("Cuisine updated successfully.");
        }
        
        // [DELETE] api/cuisine/:id
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _cuisines.DeleteOneAsync(e => e.Id == id);
            return result.DeletedCount == 0 ? NotFound() : Ok("Deleted cuisine successfully.");
        }
    }
}
