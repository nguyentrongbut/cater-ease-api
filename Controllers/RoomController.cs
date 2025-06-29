using cater_ease_api.Data;
using cater_ease_api.Models;
using cater_ease_api.Dtos.Room;
using cater_ease_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RoomController : ControllerBase
{
    private readonly IMongoCollection<RoomModel> _rooms;
    private readonly CloudinaryService _cloudinary;

    public RoomController(MongoDbService mongoDbService, CloudinaryService cloudinary)
    {
        _rooms = mongoDbService.Database.GetCollection<RoomModel>("rooms");
        _cloudinary = cloudinary;
    }

    // [GET] api/room
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var rooms = await _rooms.Find(r => !r.Deleted).ToListAsync();
        return Ok(rooms);
    }

    // [GET] api/room/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var room = await _rooms.Find(r => r.Id == id && !r.Deleted).FirstOrDefaultAsync();
        return room == null ? NotFound("Room not found") : Ok(room);
    }

    // [POST] api/room
    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateRoomDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var imageUrl = await _cloudinary.UploadAsync(dto.Image);

        var model = new RoomModel
        {
            Name = dto.Name,
            Area = dto.Area,
            People = dto.People,
            Table = dto.Table,
            Price = dto.Price,
            Image = imageUrl,
            Deleted = false
        };

        await _rooms.InsertOneAsync(model);
        return Ok(model);
    }

    // [PATCH] api/room/{id}
    [Authorize(Roles = "admin")]
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromForm] UpdateRoomDto dto)
    {
        var room = await _rooms.Find(r => r.Id == id && !r.Deleted).FirstOrDefaultAsync();
        if (room == null) return NotFound("Room not found");

        var updates = new List<UpdateDefinition<RoomModel>>();

        if (!string.IsNullOrEmpty(dto.Name))
            updates.Add(Builders<RoomModel>.Update.Set(r => r.Name, dto.Name));

        if (!string.IsNullOrEmpty(dto.Area))
            updates.Add(Builders<RoomModel>.Update.Set(r => r.Area, dto.Area));

        if (dto.People.HasValue)
            updates.Add(Builders<RoomModel>.Update.Set(r => r.People, dto.People.Value));

        if (dto.Table.HasValue)
            updates.Add(Builders<RoomModel>.Update.Set(r => r.Table, dto.Table.Value));

        if (dto.Price.HasValue)
            updates.Add(Builders<RoomModel>.Update.Set(r => r.Price, dto.Price.Value));

        if (dto.Image != null)
        {
            if (!string.IsNullOrEmpty(room.Image))
                await _cloudinary.DeleteAsync(room.Image);

            var imageUrl = await _cloudinary.UploadAsync(dto.Image);
            updates.Add(Builders<RoomModel>.Update.Set(r => r.Image, imageUrl));
        }

        if (!updates.Any()) return BadRequest("No valid fields to update");

        var result = await _rooms.UpdateOneAsync(r => r.Id == id, Builders<RoomModel>.Update.Combine(updates));
        return result.MatchedCount == 0 ? NotFound("Update failed") : Ok("Room updated successfully");
    }

    // [DELETE] api/room/{id}
    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var room = await _rooms.Find(r => r.Id == id && !r.Deleted).FirstOrDefaultAsync();
        if (room == null) return NotFound("Room not found");

        var update = Builders<RoomModel>.Update.Set(r => r.Deleted, true);
        var result = await _rooms.UpdateOneAsync(r => r.Id == id, update);

        return result.ModifiedCount == 0 ? NotFound("Delete failed") : Ok("Room deleted (soft delete) successfully");
    }
}
