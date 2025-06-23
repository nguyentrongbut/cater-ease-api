using cater_ease_api.Data;
using cater_ease_api.Dtos.Event;
using cater_ease_api.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IMongoCollection<EventModel> _events;

        public EventController(MongoDbService mongoDbService)
        {
            _events = mongoDbService.Database.GetCollection<EventModel>("events");
        }
        
        // [GET] api/event
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _events.Find(_ => true).ToListAsync();
            return Ok(data);
        }

        // [GET] api/event/:id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var dish = await _events.Find(d => d.Id == id).FirstOrDefaultAsync();
            return dish == null ? NotFound() : Ok(dish);
        }
        
        // [POST] api/event
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEventDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var model = new EventModel
            {
                Title = dto.Title,
                Description = dto.Description,
            };

            await _events.InsertOneAsync(model);
            return Ok(model);
        }

        // [PATCH] api/event/:id
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id, [FromBody] UpdateEventDto dto)
        {
            var updates = new List<UpdateDefinition<EventModel>>();
    
            if (dto.Title != null)
                updates.Add(Builders<EventModel>.Update.Set(e => e.Title, dto.Title));

            if (dto.Description != null)
                updates.Add(Builders<EventModel>.Update.Set(e => e.Description, dto.Description));

            if (updates.Count == 0)
                return BadRequest("No valid fields provided.");

            var update = Builders<EventModel>.Update.Combine(updates);
            var result = await _events.UpdateOneAsync(e => e.Id == id, update);

            return result.MatchedCount == 0
                ? NotFound()
                : Ok("Event updated successfully.");
        }
        
        // [DELETE] api/event/:id
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _events.DeleteOneAsync(e => e.Id == id);
            return result.DeletedCount == 0 ? NotFound() : Ok("Deleted event successfully.");
        }
    }
}
