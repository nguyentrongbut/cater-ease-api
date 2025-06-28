using cater_ease_api.Data;
using cater_ease_api.Dtos.Booking;
using cater_ease_api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BookingController : ControllerBase
{
    private readonly IMongoCollection<BookingModel> _bookings;

    public BookingController(MongoDbService mongoDbService)
    {
        _bookings = mongoDbService.Database.GetCollection<BookingModel>("bookings");
    }

    // [GET] api/booking
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll()
    {
        var data = await _bookings.Find(_ => true).ToListAsync();
        return Ok(data);
    }

    // [GET] api/booking/{id}
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(string id)
    {
        var booking = await _bookings.Find(b => b.Id == id).FirstOrDefaultAsync();
        return booking == null ? NotFound() : Ok(booking);
    }

    // [GET] api/booking/by-auth/{authId}
    [HttpGet("by-auth/{authId}")]
    [Authorize]
    public async Task<IActionResult> GetByAuthId(string authId)
    {
        var data = await _bookings.Find(b => b.AuthId == authId).ToListAsync();
        return Ok(data);
    }

    // [POST] api/booking
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var booking = new BookingModel
        {
            AuthId = dto.AuthId,
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            EventName = dto.EventName,
            LocationType = dto.LocationType,
            People = dto.People,
            Address = dto.Address,
            EventTime = dto.EventTime,
            EventDate = dto.EventDate,
            MenuIds = dto.MenuIds,
            ServiceIds = dto.ServiceIds,
            Note = dto.Note,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        await _bookings.InsertOneAsync(booking);
        return Ok(booking);
    }

    // [PATCH] api/booking/{id}
    [HttpPatch("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateBookingStatusDto dto)
    {
        var update = Builders<BookingModel>.Update.Set(b => b.Status, dto.Status);
        var result = await _bookings.UpdateOneAsync(b => b.Id == id, update);

        return result.ModifiedCount == 0
            ? NotFound("Update failed")
            : Ok("Booking status updated.");
    }

    // [DELETE] api/booking/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _bookings.DeleteOneAsync(b => b.Id == id);
        return result.DeletedCount == 0 ? NotFound() : Ok("Deleted booking successfully.");
    }
}
