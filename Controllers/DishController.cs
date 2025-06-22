using System.Globalization;
using System.Text.Json;
using cater_ease_api.Data;
using cater_ease_api.Models;
using cater_ease_api.Dtos.Dish;
using cater_ease_api.Services;
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
        private readonly CloudinaryService _cloudinary;

        private readonly SlugHelper _slugHelper = new();

        public DishController(MongoDbService mongoDbService, CloudinaryService cloudinary)
        {
            _dishes = mongoDbService.Database.GetCollection<DishModel>("dishes");
            _cloudinary = cloudinary;
        }

        // [POST] api/dish
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
            var data = await _dishes.Find(_ => true).ToListAsync();
            return Ok(data);
        }

        // [GET] api/dish/:id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var dish = await _dishes.Find(d => d.Id == id).FirstOrDefaultAsync();
            return dish == null ? NotFound() : Ok(dish);
        }

        
        // [PATCH] api/dish/:id
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