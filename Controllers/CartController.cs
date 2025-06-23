using cater_ease_api.Data;
using cater_ease_api.Dtos.Cart;
using cater_ease_api.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly IMongoCollection<CartModel> _cart;

        public CartController(MongoDbService mongo)
        {
            _cart = mongo.Database.GetCollection<CartModel>("cart");
        }

        // [POST] api/cart
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var existing = await _cart.Find(x => x.AuthId == dto.AuthId && x.DishId == dto.DishId)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                // Nếu đã có thì cộng thêm số lượng
                var update = Builders<CartModel>.Update
                    .Inc(x => x.Quantity, dto.Quantity);
                await _cart.UpdateOneAsync(x => x.Id == existing.Id, update);
                return Ok("Updated quantity");
            }

            // Nếu chưa có thì thêm mới
            var cartItem = new CartModel
            {
                AuthId = dto.AuthId,
                DishId = dto.DishId,
                DishName = dto.DishName,
                DishImage = dto.DishImage,
                Quantity = dto.Quantity,
                Price = dto.Price
            };

            await _cart.InsertOneAsync(cartItem);
            return Ok(cartItem);
        }

        // [GET] api/cart/user/{authId}
        [HttpGet("user/{authId}")]
        public async Task<IActionResult> GetByUser(string authId)
        {
            var items = await _cart.Find(x => x.AuthId == authId).ToListAsync();

            var result = items.Select(x => new CartItemDto
            {
                Id = x.Id!,
                AuthId = x.AuthId,
                DishId = x.DishId,
                DishName = x.DishName,
                DishImage = x.DishImage,
                Quantity = x.Quantity,
                Price = x.Price
            }).ToList();

            var totalQuantity = result.Sum(x => x.Quantity);
            var totalAmount = result.Sum(x => x.SubTotal);

            return Ok(new
            {
                items = result,
                totalQuantity,
                totalAmount
            });
        }

        // [PATCH] api/cart/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateQuantity(string id, [FromBody] UpdateCartDto dto)
        {
            var update = Builders<CartModel>.Update.Set(x => x.Quantity, dto.Quantity);
            var result = await _cart.UpdateOneAsync(x => x.Id == id, update);
            return result.ModifiedCount == 0 ? NotFound() : Ok("Updated cart successfully.");
        }

        // [DELETE] api/cart/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _cart.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount == 0 ? NotFound() : Ok("Deleted dish successfully.");
        }

        // [DELETE] api/cart/user/{authId}
        [HttpDelete("user/{authId}")]
        public async Task<IActionResult> ClearUserCart(string authId)
        {
            await _cart.DeleteManyAsync(x => x.AuthId == authId);
            return Ok("Cleared cart successfully.");
        }
    }
}