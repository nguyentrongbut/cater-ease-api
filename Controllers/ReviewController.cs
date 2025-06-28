using cater_ease_api.Data;
using cater_ease_api.Dtos.Review;
using cater_ease_api.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers;

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
                EventId = r.EventId,
                UserName = user?.Name ?? "Unknown",
                Comment = r.Comment,
                Rating = r.Rating,
                CreatedAt = r.CreatedAt
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var review = await _reviews.Find(r => r.Id == id).FirstOrDefaultAsync();
        if (review == null) return NotFound();

        var user = await _auth.Find(u => u.Id == review.AuthId).FirstOrDefaultAsync();
        var result = new ReviewDetailDto
        {
            Id = review.Id,
            EventId = review.EventId,
            UserName = user?.Name ?? "Unknown",
            Comment = review.Comment,
            Rating = review.Rating,
            CreatedAt = review.CreatedAt
        };

        return Ok(result);
    }

    [HttpGet("event/{eventId}")]
    public async Task<IActionResult> GetByEvent(string eventId)
    {
        var reviews = await _reviews.Find(r => r.EventId == eventId).ToListAsync();
        var userIds = reviews.Select(r => r.AuthId).Distinct().ToList();
        var users = await _auth.Find(u => userIds.Contains(u.Id)).ToListAsync();

        var result = reviews.Select(r =>
        {
            var user = users.FirstOrDefault(u => u.Id == r.AuthId);
            return new ReviewDetailDto
            {
                Id = r.Id,
                EventId = r.EventId,
                UserName = user?.Name ?? "Unknown",
                Comment = r.Comment,
                Rating = r.Rating,
                CreatedAt = r.CreatedAt
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(string userId)
    {
        var reviews = await _reviews.Find(r => r.AuthId == userId).ToListAsync();
        var user = await _auth.Find(u => u.Id == userId).FirstOrDefaultAsync();
        var userName = user?.Name ?? "Unknown";

        var result = reviews.Select(r => new ReviewDetailDto
        {
            Id = r.Id,
            EventId = r.EventId,
            UserName = userName,
            Comment = r.Comment,
            Rating = r.Rating,
            CreatedAt = r.CreatedAt
        }).ToList();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existing = await _reviews
            .Find(r => r.AuthId == dto.AuthId && r.EventId == dto.EventId)
            .FirstOrDefaultAsync();

        if (existing != null)
            return BadRequest("You have already reviewed this event.");

        var review = new ReviewModel
        {
            AuthId = dto.AuthId,
            EventId = dto.EventId,
            Comment = dto.Comment,
            Rating = dto.Rating,
            CreatedAt = DateTime.UtcNow
        };

        await _reviews.InsertOneAsync(review);
        return Ok(review);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(string id, [FromBody] UpdateReviewDto dto)
    {
        var updateDefs = new List<UpdateDefinition<ReviewModel>>();

        if (dto.Rating.HasValue)
            updateDefs.Add(Builders<ReviewModel>.Update.Set(r => r.Rating, dto.Rating.Value));

        if (!string.IsNullOrEmpty(dto.Comment))
            updateDefs.Add(Builders<ReviewModel>.Update.Set(r => r.Comment, dto.Comment));

        if (!updateDefs.Any())
            return BadRequest("No valid fields to update.");

        updateDefs.Add(Builders<ReviewModel>.Update.Set(r => r.CreatedAt, DateTime.UtcNow));

        var update = Builders<ReviewModel>.Update.Combine(updateDefs);
        var result = await _reviews.UpdateOneAsync(r => r.Id == id, update);

        return result.ModifiedCount == 0 ? NotFound() : Ok("Updated");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _reviews.DeleteOneAsync(r => r.Id == id);
        return result.DeletedCount == 0 ? NotFound() : Ok("Deleted");
    }
}
