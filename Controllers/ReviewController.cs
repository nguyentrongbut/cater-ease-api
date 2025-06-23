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
        private readonly IMongoCollection<AuthModel> _auth;

        public ReviewController(MongoDbService mongoDbService)
        {
            _reviews = mongoDbService.Database.GetCollection<ReviewModel>("reviews");
            _auth = mongoDbService.Database.GetCollection<AuthModel>("auth");
        }
        
        // [GET] api/review
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reviews = await _reviews.Find(_ => true).ToListAsync();

            var userIds = reviews.Select(r => r.AuthId).Distinct().ToList();
            var users = await _auth.Find(u => userIds.Contains(u.Id)).ToListAsync();

            var result = reviews.Select(r =>
            {
                var user = users.FirstOrDefault(u => u.Id == r.AuthId);
                return new ReviewDetailDto
                {
                    Id = r.Id,
                    MenuId = r.MenuId,
                    UserName = user?.Name ?? "Unknown",
                    Comment = r.Comment,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt
                };
            }).ToList();

            return Ok(result);
        }

        // [GET] api/review/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var review = await _reviews.Find(r => r.Id == id).FirstOrDefaultAsync();
            if (review == null) return NotFound();

            var user = await _auth.Find(u => u.Id == review.AuthId).FirstOrDefaultAsync();

            var result = new ReviewDetailDto
            {
                Id = review.Id,
                MenuId = review.MenuId,
                UserName = user?.Name ?? "Unknown",
                Comment = review.Comment,
                Rating = review.Rating,
                CreatedAt = review.CreatedAt
            };

            return Ok(result);
        }

        // [GET] api/review/menu/{menuId}
        [HttpGet("menu/{menuId}")]
        public async Task<IActionResult> GetByMenu(string menuId)
        {
            var reviews = await _reviews.Find(r => r.MenuId == menuId).ToListAsync();
            var userIds = reviews.Select(r => r.AuthId).Distinct().ToList();
            var users = await _auth.Find(u => userIds.Contains(u.Id)).ToListAsync();

            var result = reviews.Select(r =>
            {
                var user = users.FirstOrDefault(u => u.Id == r.AuthId);
                return new ReviewDetailDto
                {
                    Id = r.Id,
                    MenuId = r.MenuId,
                    UserName = user?.Name ?? "Unknown",
                    Comment = r.Comment,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt
                };
            }).ToList();

            return Ok(result);
        }

        // [GET] api/review/user/{userId}]
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(string userId)
        {
            var reviews = await _reviews.Find(r => r.AuthId == userId).ToListAsync();
            var user = await _auth.Find(u => u.Id == userId).FirstOrDefaultAsync();
            var userName = user?.Name ?? "Unknown";

            var result = reviews.Select(r => new ReviewDetailDto
            {
                Id = r.Id,
                MenuId = r.MenuId,
                UserName = userName,
                Comment = r.Comment,
                Rating = r.Rating,
                CreatedAt = r.CreatedAt
            }).ToList();

            return Ok(result);
        }

        // [POST] api/review
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Kiểm tra xem user đã review menu này chưa
            var existing = await _reviews
                .Find(r => r.AuthId == dto.AuthId && r.MenuId == dto.MenuId)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return BadRequest("You have already reviewed this menu.");
            }

            var review = new ReviewModel
            {
                AuthId = dto.AuthId,
                MenuId = dto.MenuId,
                Comment = dto.Comment,
                Rating = dto.Rating,
                CreatedAt = DateTime.UtcNow
            };

            await _reviews.InsertOneAsync(review);

            return Ok(review);
        }

        // [PATCH] api/review/{id}
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

            updateDefs.Add(Builders<ReviewModel>.Update.Set(r => r.CreatedAt, DateTime.UtcNow));

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
    }
}