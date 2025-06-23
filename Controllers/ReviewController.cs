using cater_ease_api.Data;
using cater_ease_api.Dtos.Review;
using cater_ease_api.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly IMongoCollection<ReviewModel> _reviews;

        public ReviewController(MongoDbService mongoDbService)
        {
            _reviews = mongoDbService.Database.GetCollection<ReviewModel>("reviews");
        }

        // [GET] api/review
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reviews = await _reviews.Find(_ => true).ToListAsync();
            return Ok(reviews);
        }

        // [GET] api/review/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var review = await _reviews.Find(r => r.Id == id).FirstOrDefaultAsync();
            return review == null ? NotFound() : Ok(review);
        }

        // [POST] api/review
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var review = new ReviewModel
            {
                DishId = dto.DishId,
                AuthId = dto.AuthId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            await _reviews.InsertOneAsync(review);
            return Ok(review);
        }

        // PATCH: api/review/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id, [FromBody] UpdateReviewDto dto)
        {
            var updateDefs = new List<UpdateDefinition<ReviewModel>>();

            if (dto.Rating.HasValue)
                updateDefs.Add(Builders<ReviewModel>.Update.Set(r => r.Rating, dto.Rating.Value));

            if (!string.IsNullOrEmpty(dto.Comment))
                updateDefs.Add(Builders<ReviewModel>.Update.Set(r => r.Comment, dto.Comment));

            if (updateDefs.Count == 0)
                return BadRequest("No valid fields to update.");

            var update = Builders<ReviewModel>.Update.Combine(updateDefs);
            var result = await _reviews.UpdateOneAsync(r => r.Id == id, update);

            return result.ModifiedCount == 0 ? NotFound() : Ok("Updated");
        }

        // [DELETE] api/review/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _reviews.DeleteOneAsync(r => r.Id == id);
            return result.DeletedCount == 0 ? NotFound() : Ok("Deleted");
        }

        // [GET] api/review/dish/{dishId}
        [HttpGet("dish/{dishId}")]
        public async Task<IActionResult> GetByDish(string dishId)
        {
            var reviews = await _reviews.Find(r => r.DishId == dishId).ToListAsync();
            return Ok(reviews);
        }

        // [GET] api/review/auth/{authId}
        [HttpGet("auth/{authId}")]
        public async Task<IActionResult> GetByUser(string authId)
        {
            var reviews = await _reviews.Find(r => r.AuthId == authId).ToListAsync();
            return Ok(reviews);
        }
    }
}