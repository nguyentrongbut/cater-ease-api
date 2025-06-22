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
        public async Task<IActionResult> Patch(string id, [FromBody] Dictionary<string, object> updates)
        {
            if (updates == null || updates.Count == 0)
                return BadRequest("No updates provided.");

            var allowedFields = new HashSet<string>
            {
                "Name", "Slug", "Description", "Price", "Image", "SubImage", "CuisineId", "EventId"
            };

            var updateDefs = new List<UpdateDefinition<DishModel>>();

            foreach (var kvp in updates)
            {
                var key = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(kvp.Key);
                if (!allowedFields.Contains(key)) continue;

                object? value = kvp.Value;
                
                if (value is JsonElement json)
                {
                    value = json.ValueKind switch
                    {
                        JsonValueKind.String => json.GetString(),
                        JsonValueKind.Number when json.TryGetInt32(out var i) => i,
                        JsonValueKind.Number when json.TryGetDecimal(out var d) => d,
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Array => json.EnumerateArray().Select(e => e.GetString()).Where(x => x != null)
                            .ToList(),
                        _ => null
                    };
                }

                if (key == "Name" && value is string name)
                {
                    updateDefs.Add(Builders<DishModel>.Update.Set("Name", name));
                    var slug = _slugHelper.GenerateSlug(name);
                    updateDefs.Add(Builders<DishModel>.Update.Set("Slug", slug));
                }
                else if (value != null)
                {
                    updateDefs.Add(Builders<DishModel>.Update.Set(key, BsonValue.Create(value)));
                }
            }

            if (updateDefs.Count == 0)
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