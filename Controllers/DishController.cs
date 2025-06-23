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
using Slugify;

namespace cater_ease_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishController : ControllerBase
    {
        private readonly IMongoCollection<DishModel> _dishes;
        private readonly IMongoCollection<CuisineModel> _cuisines;
        private readonly IMongoCollection<EventModel> _events;
        private readonly IMongoCollection<ReviewModel> _reviews;
        private readonly CloudinaryService _cloudinary;

        private readonly SlugHelper _slugHelper = new();

        public DishController(MongoDbService mongoDbService, CloudinaryService cloudinary)
        {
            _dishes = mongoDbService.Database.GetCollection<DishModel>("dishes");
            _cuisines = mongoDbService.Database.GetCollection<CuisineModel>("cuisines");
            _events = mongoDbService.Database.GetCollection<EventModel>("events");
            _reviews = mongoDbService.Database.GetCollection<ReviewModel>("reviews");
            _cloudinary = cloudinary;
        }

        // [POST] api/dish
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateDishDto form)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Upload main image
            string? imageUrl = null;
            if (form.Image != null)
                imageUrl = await _cloudinary.UploadAsync(form.Image);

            // Upload sub images
            List<string>? subImageUrls = null;
            if (form.SubImage != null && form.SubImage.Count > 0)
            {
                subImageUrls = new List<string>();
                foreach (var img in form.SubImage)
                {
                    var url = await _cloudinary.UploadAsync(img);
                    subImageUrls.Add(url);
                }
            }

            var dish = new DishModel
            {
                Name = form.Name,
                Slug = _slugHelper.GenerateSlug(form.Name),
                Description = form.Description,
                Price = form.Price,
                CuisineId = form.CuisineId,
                EventId = form.EventId,
                Image = imageUrl,
                SubImage = subImageUrls
            };

            await _dishes.InsertOneAsync(dish);
            return Ok(dish);
        }

        // [GET] api/dish
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var dishes = await _dishes.Find(_ => true).ToListAsync();
            var cuisineIds = dishes.Select(d => d.CuisineId).Distinct().ToList();
            var eventIds = dishes.Where(d => !string.IsNullOrEmpty(d.EventId))
                .Select(d => d.EventId!).Distinct().ToList();

            var cuisines = await _cuisines.Find(c => cuisineIds.Contains(c.Id)).ToListAsync();
            var events = await _events.Find(e => eventIds.Contains(e.Id)).ToListAsync();

            var result = new List<DishDetailDto>();

            foreach (var dish in dishes)
            {
                var cuisineTitle = cuisines.FirstOrDefault(c => c.Id == dish.CuisineId)?.Title ?? "Unknown";
                var eventTitle = events.FirstOrDefault(e => e.Id == dish.EventId)?.Title;

                var dishReviews = await _reviews.Find(r => r.DishId == dish.Id).ToListAsync();
                var avgRating = dishReviews.Count > 0 ? Math.Round(dishReviews.Average(r => r.Rating), 1) : 0;

                result.Add(new DishDetailDto
                {
                    Id = dish.Id!,
                    Name = dish.Name,
                    Slug = dish.Slug,
                    Description = dish.Description,
                    Price = dish.Price,
                    Image = dish.Image,
                    SubImage = dish.SubImage,
                    CuisineName = cuisineTitle,
                    EventName = eventTitle,
                    AverageRating = avgRating,
                    ReviewCount = dishReviews.Count
                });
            }

            return Ok(result);
        }
        
        // [GET] api/dish/:slug
        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var dish = await _dishes.Find(d => d.Slug == slug).FirstOrDefaultAsync();
            if (dish == null) return NotFound();
            var dishReviews = await _reviews.Find(r => r.DishId == dish.Id).ToListAsync();
            var avgRating = dishReviews.Count > 0 ? Math.Round(dishReviews.Average(r => r.Rating), 1) : 0;

            var cuisine = await _cuisines.Find(c => c.Id == dish.CuisineId).FirstOrDefaultAsync();
            var eventInfo = string.IsNullOrEmpty(dish.EventId)
                ? null
                : await _events.Find(e => e.Id == dish.EventId).FirstOrDefaultAsync();

            var result = new DishDetailDto
            {
                Id = dish.Id!,
                Name = dish.Name,
                Slug = dish.Slug,
                Description = dish.Description,
                Price = dish.Price,
                Image = dish.Image,
                SubImage = dish.SubImage,
                CuisineName = cuisine.Title ?? "Unknown",
                EventName = eventInfo?.Title,
                AverageRating = avgRating,
                ReviewCount = dishReviews.Count
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
            {
                updateDefs.Add(Builders<DishModel>.Update.Set(d => d.Name, dto.Name));
                var slug = _slugHelper.GenerateSlug(dto.Name);
                updateDefs.Add(Builders<DishModel>.Update.Set(d => d.Slug, slug));
            }

            if (!string.IsNullOrEmpty(dto.Slug))
                updateDefs.Add(Builders<DishModel>.Update.Set(d => d.Slug, dto.Slug));

            if (!string.IsNullOrEmpty(dto.Description))
                updateDefs.Add(Builders<DishModel>.Update.Set(d => d.Description, dto.Description));

            if (dto.Price.HasValue)
                updateDefs.Add(Builders<DishModel>.Update.Set(d => d.Price, dto.Price.Value));

            if (!string.IsNullOrEmpty(dto.Image))
                updateDefs.Add(Builders<DishModel>.Update.Set(d => d.Image, dto.Image));

            if (dto.SubImage != null)
                updateDefs.Add(Builders<DishModel>.Update.Set(d => d.SubImage, dto.SubImage));

            if (!string.IsNullOrEmpty(dto.CuisineId))
                updateDefs.Add(Builders<DishModel>.Update.Set(d => d.CuisineId, dto.CuisineId));

            if (!string.IsNullOrEmpty(dto.EventId))
                updateDefs.Add(Builders<DishModel>.Update.Set(d => d.EventId, dto.EventId));

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
            {
                await _cloudinary.DeleteAsync(dish.Image);
            }

            if (dish.SubImage != null)
            {
                foreach (var url in dish.SubImage)
                {
                    await _cloudinary.DeleteAsync(url);
                }
            }

            var result = await _dishes.DeleteOneAsync(d => d.Id == id);
            return result.DeletedCount == 0 ? NotFound() : Ok("Deleted dish successfully.");
        }
    }
}