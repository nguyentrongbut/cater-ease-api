using cater_ease_api.Data;
using cater_ease_api.Dtos.Order;
using cater_ease_api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IMongoCollection<OrderModel> _orders;
        
        public OrderController(MongoDbService mongoDbService)
        {
            _orders = mongoDbService.Database.GetCollection<OrderModel>("orders");
        }
        
        // [POST] api/order
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var order = new OrderModel
            {
                AuthId = dto.AuthId,
                Name = dto.Name,
                Phone = dto.Phone,
                Email = dto.Email,
                Note = dto.Note,
                Items = dto.Items.Select(i => new OrderItem
                {
                    DishId = i.DishId,
                    Name = i.Name,
                    Image = i.Image,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList(),
                SubTotal = dto.SubTotal,
                Total = dto.Total,
                CreatedAt = DateTime.UtcNow,
                Status = "pending"
            };

            await _orders.InsertOneAsync(order);
            return Ok(order);
        }

        // [GET] api/order
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _orders.Find(_ => true).ToListAsync();
            return Ok(orders);
        }

        // [GET] api/order/:id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var order = await _orders.Find(o => o.Id == id).FirstOrDefaultAsync();
            return order == null ? NotFound() : Ok(order);
        }
        
        // [GET] api/order/auth/{authId}
        [HttpGet("auth/{authId}")]
        public async Task<IActionResult> GetByAuth(string authId)
        {
            var orders = await _orders.Find(o => o.AuthId == authId).ToListAsync();
            return Ok(orders);
        }

        // [PATCH] api/order/:id
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest("Status is required.");

            var update = Builders<OrderModel>.Update.Set(o => o.Status, dto.Status);
            var result = await _orders.UpdateOneAsync(o => o.Id == id, update);

            return result.ModifiedCount == 0 ? NotFound() : Ok("Updated status");
        }

        // [DELETE] api/order/:id
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _orders.DeleteOneAsync(o => o.Id == id);
            return result.DeletedCount == 0 ? NotFound() : Ok("Deleted order");
        }
    }
}
