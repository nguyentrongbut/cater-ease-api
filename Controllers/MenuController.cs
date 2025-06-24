using cater_ease_api.Data;
using cater_ease_api.Dtos.Dish;
using cater_ease_api.Dtos.Menu;
using cater_ease_api.Models;
using cater_ease_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        private readonly IMongoCollection<MenuModel> _menus;
        private readonly IMongoCollection<DishModel> _dishes;
        private readonly IMongoCollection<CategoryModel> _categories;
        private readonly IMongoCollection<ReviewModel> _reviews;
        private readonly CloudinaryService _cloudinary;
        

        public MenuController(MongoDbService mongoDbService, CloudinaryService cloudinary)
        {
            _menus = mongoDbService.Database.GetCollection<MenuModel>("menus");
            _dishes = mongoDbService.Database.GetCollection<DishModel>("dishes");
            _categories = mongoDbService.Database.GetCollection<CategoryModel>("categories");
            _reviews = mongoDbService.Database.GetCollection<ReviewModel>("reviews");
            _cloudinary = cloudinary;
        }

        // [GET] api/event
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var menus = await _menus.Find(_ => true).ToListAsync();
            var allDishIds = menus.SelectMany(m => m.DishIds).Distinct().ToList();
            var allDishes = await _dishes.Find(d => allDishIds.Contains(d.Id)).ToListAsync();
            var categoryIds = allDishes.Select(d => d.CategoryId).Distinct().ToList();
            var categories = await _categories.Find(c => categoryIds.Contains(c.Id)).ToListAsync();

            var result = new List<MenuDetailDto>();

            foreach (var menu in menus)
            {
                var menuDishes = allDishes
                    .Where(d => menu.DishIds.Contains(d.Id))
                    .Select(d => new DishDetailDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Description = d.Description,
                        Price = d.Price,
                        Image = d.Image,
                        CategoryName = categories.FirstOrDefault(c => c.Id == d.CategoryId)?.Name ?? "Unknown"
                    })
                    .ToList();

                var menuReviews = await _reviews.Find(r => r.MenuId == menu.Id).ToListAsync();
                var total = menuReviews.Count;
                var avg = total > 0 ? Math.Round(menuReviews.Average(r => r.Rating), 1) : 0;

                result.Add(new MenuDetailDto
                {
                    Id = menu.Id,
                    Name = menu.Name,
                    Description = menu.Description,
                    Dishes = menuDishes,
                    Image = menu.Image,
                    Price = menu.Price,
                    AverageRating = avg,
                    TotalReviews = total
                });
            }

            return Ok(result);
        }

        // [GET] api/:id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var menu = await _menus.Find(m => m.Id == id).FirstOrDefaultAsync();
            if (menu == null) return NotFound();

            var dishes = await _dishes.Find(d => menu.DishIds.Contains(d.Id)).ToListAsync();
            var categoryIds = dishes.Select(d => d.CategoryId).Distinct().ToList();
            var categories = await _categories.Find(c => categoryIds.Contains(c.Id)).ToListAsync();

            var dishDtos = dishes.Select(d => new DishDetailDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                Price = d.Price,
                Image = d.Image,
                CategoryName = categories.FirstOrDefault(c => c.Id == d.CategoryId)?.Name ?? "Unknown"
            }).ToList();

            var menuReviews = await _reviews.Find(r => r.MenuId == menu.Id).ToListAsync();
            var total = menuReviews.Count;
            var avg = total > 0 ? Math.Round(menuReviews.Average(r => r.Rating), 1) : 0;

            var result = new MenuDetailDto
            {
                Id = menu.Id,
                Name = menu.Name,
                Description = menu.Description,
                Dishes = dishDtos,
                Image = menu.Image,
                Price = menu.Price,
                AverageRating = avg,
                TotalReviews = total
            };

            return Ok(result);
        }

        // [POST] api/event
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateMenuDto form)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Upload ảnh
            string imageUrl = form.Image != null ? await _cloudinary.UploadAsync(form.Image) : null;

            // Lấy danh sách món ăn
            var dishes = await _dishes.Find(d => form.DishIds.Contains(d.Id)).ToListAsync();
            var totalPrice = dishes.Sum(d => d.Price);

            var menu = new MenuModel
            {
                Name = form.Name,
                Description = form.Description,
                DishIds = form.DishIds,
                Image = imageUrl,
                Price = totalPrice,
                EventId = form.EventId
            };

            await _menus.InsertOneAsync(menu);
            return Ok(menu);
        }

        // [PATCH] api/event/:id
        [Authorize(Roles = "admin")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateMenuDto dto)
        {
            var updateDefs = new List<UpdateDefinition<MenuModel>>();

            if (!string.IsNullOrEmpty(dto.Name))
                updateDefs.Add(Builders<MenuModel>.Update.Set(m => m.Name, dto.Name));
            if (!string.IsNullOrEmpty(dto.Description))
                updateDefs.Add(Builders<MenuModel>.Update.Set(m => m.Description, dto.Description));
            if (dto.DishIds != null)
                updateDefs.Add(Builders<MenuModel>.Update.Set(m => m.DishIds, dto.DishIds));
            if (!string.IsNullOrEmpty(dto.Image))
                updateDefs.Add(Builders<MenuModel>.Update.Set(m => m.Image, dto.Image));
            if (dto.Price.HasValue)
                updateDefs.Add(Builders<MenuModel>.Update.Set(m => m.Price, dto.Price.Value));

            if (!updateDefs.Any()) return BadRequest("No valid fields to update.");

            var update = Builders<MenuModel>.Update.Combine(updateDefs);
            var result = await _menus.UpdateOneAsync(m => m.Id == id, update);

            return result.MatchedCount == 0 ? NotFound() : Ok("Updated menu successfully.");
        }

        // [DELETE] api/event/:id
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var menu = await _menus.Find(m => m.Id == id).FirstOrDefaultAsync();
            if (menu == null) return NotFound();

            if (!string.IsNullOrEmpty(menu.Image))
                await _cloudinary.DeleteAsync(menu.Image);

            var result = await _menus.DeleteOneAsync(m => m.Id == id);
            return result.DeletedCount == 0 ? NotFound() : Ok("Deleted menu successfully.");
        }
        
        // [GET] api/event/:eventId
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetByEvent(string eventId)
        {
            var menus = await _menus.Find(m => m.EventId == eventId).ToListAsync();
            var allDishIds = menus.SelectMany(m => m.DishIds).Distinct().ToList();
            var allDishes = await _dishes.Find(d => allDishIds.Contains(d.Id)).ToListAsync();
            var categoryIds = allDishes.Select(d => d.CategoryId).Distinct().ToList();
            var categories = await _categories.Find(c => categoryIds.Contains(c.Id)).ToListAsync();

            var result = new List<MenuDetailDto>();

            foreach (var menu in menus)
            {
                var dishes = allDishes
                    .Where(d => menu.DishIds.Contains(d.Id))
                    .Select(d => new DishDetailDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Description = d.Description,
                        Price = d.Price,
                        Image = d.Image,
                        CategoryName = categories.FirstOrDefault(c => c.Id == d.CategoryId)?.Name ?? "Unknown"
                    })
                    .ToList();

                var menuReviews = await _reviews.Find(r => r.MenuId == menu.Id).ToListAsync();
                var total = menuReviews.Count;
                var avg = total > 0 ? Math.Round(menuReviews.Average(r => r.Rating), 1) : 0;

                result.Add(new MenuDetailDto
                {
                    Id = menu.Id,
                    Name = menu.Name,
                    Description = menu.Description,
                    Dishes = dishes,
                    Image = menu.Image,
                    Price = menu.Price,
                    AverageRating = avg,
                    TotalReviews = total
                });
            }

            return Ok(result);
        }
    }
}