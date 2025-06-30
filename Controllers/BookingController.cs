using cater_ease_api.Data;
using cater_ease_api.Models;
using cater_ease_api.Helpers;
using cater_ease_api.Dtos.Booking;
using cater_ease_api.Dtos.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BookingController : ControllerBase
{
    private readonly IMongoCollection<BookingModel> _bookings;
    private readonly IMongoCollection<MenuModel> _menus;
    private readonly IMongoCollection<DishModel> _dishes;
    private readonly IMongoCollection<ServiceModel> _services;
    private readonly IMongoCollection<RoomModel> _rooms;
    private readonly IMongoCollection<VenueModel> _venues;
    private readonly IMongoCollection<EventModel> _events;
    private readonly EmailService _emailService;


    public BookingController(MongoDbService mongoDbService, EmailService emailService)
    {
        _bookings = mongoDbService.Database.GetCollection<BookingModel>("bookings");
        _menus = mongoDbService.Database.GetCollection<MenuModel>("menus");
        _dishes = mongoDbService.Database.GetCollection<DishModel>("dishes");
        _services = mongoDbService.Database.GetCollection<ServiceModel>("services");
        _rooms = mongoDbService.Database.GetCollection<RoomModel>("rooms");
        _venues = mongoDbService.Database.GetCollection<VenueModel>("venues");
        _events = mongoDbService.Database.GetCollection<EventModel>("events");
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var bookings = await _bookings.Find(_ => true)
            .SortByDescending(b => b.CreatedAt)
            .ToListAsync();

        var menuIds = bookings.Select(b => b.MenuId).Distinct().ToList();
        var allMenus = await _menus.Find(m => menuIds.Contains(m.Id)).ToListAsync();

        var dishIds = allMenus.SelectMany(m => m.DishIds).Distinct().ToList();
        var allDishes = await _dishes.Find(d => dishIds.Contains(d.Id)).ToListAsync();

        var serviceIds = bookings.SelectMany(b => b.ServiceIds).Distinct().ToList();
        var allServices = await _services.Find(s => serviceIds.Contains(s.Id)).ToListAsync();

        var roomIds = bookings.Select(b => b.RoomId).Distinct().ToList();
        var allRooms = await _rooms.Find(r => roomIds.Contains(r.Id)).ToListAsync();

        var venueIds = bookings.Select(b => b.VenueId).Distinct().ToList();
        var allVenues = await _venues.Find(v => venueIds.Contains(v.Id)).ToListAsync();

        var eventIds = bookings.Select(b => b.EventId).Distinct().ToList();
        var allEvents = await _events.Find(e => eventIds.Contains(e.Id)).ToListAsync();

        var result = bookings.Select(b =>
        {
            var menu = allMenus.FirstOrDefault(m => m.Id == b.MenuId);
            var dishes = menu != null
                ? allDishes
                    .Where(d => menu.DishIds.Contains(d.Id))
                    .Select(d => (object)new { d.Id, d.Name })
                    .ToList()
                : new List<object>();

            var services = allServices
                .Where(s => b.ServiceIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name })
                .ToList();

            var room = allRooms.FirstOrDefault(r => r.Id == b.RoomId);
            var venue = allVenues.FirstOrDefault(v => v.Id == b.VenueId);
            var ev = allEvents.FirstOrDefault(e => e.Id == b.EventId);

            return new
            {
                b.Id,
                b.OrderCode,
                b.Name,
                b.Email,
                b.Phone,
                b.EventDate,
                b.EventTime,
                Event = ev != null ? new { ev.Id, ev.Name } : null,
                Venue = venue != null ? new { venue.Id, venue.Name } : null,
                Room = room != null ? new { room.Id, room.Name } : null,
                Menu = menu != null
                    ? new
                    {
                        menu.Id,
                        menu.Name,
                        menu.Price,
                        Dishes = dishes
                    }
                    : null,
                Services = services,
                b.People,
                b.Address,
                b.PaymentMethod,
                b.Notes,
                b.Status,
                b.CreatedAt
            };
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var booking = await _bookings.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (booking == null) return NotFound("Booking not found");

        var menu = await _menus.Find(m => m.Id == booking.MenuId).FirstOrDefaultAsync();
        var dishes = menu != null
            ? await _dishes.Find(d => menu.DishIds.Contains(d.Id)).ToListAsync()
            : new List<DishModel>();

        var services = await _services.Find(s => booking.ServiceIds.Contains(s.Id)).ToListAsync();
        var room = await _rooms.Find(r => r.Id == booking.RoomId).FirstOrDefaultAsync();
        var venue = await _venues.Find(v => v.Id == booking.VenueId).FirstOrDefaultAsync();
        var ev = await _events.Find(e => e.Id == booking.EventId).FirstOrDefaultAsync();

        return Ok(new
        {
            booking.Id,
            booking.OrderCode,
            booking.Name,
            booking.Email,
            booking.Phone,
            booking.EventDate,
            booking.EventTime,
            Event = ev,
            Venue = venue,
            Room = room,
            Menu = menu != null
                ? new
                {
                    menu.Id,
                    menu.Name,
                    menu.Price,
                    Dishes = dishes.Select(d => new { d.Id, d.Name })
                }
                : null,
            Services = services.Select(s => new { s.Id, s.Name, s.Price }),
            booking.People,
            booking.Address,
            booking.PaymentMethod,
            booking.Notes,
            booking.Status,
            booking.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var countToday = await _bookings.CountDocumentsAsync(b => b.CreatedAt.Date == DateTime.UtcNow.Date);
        var orderCode = $"CE-{today}-{(countToday + 1):D4}";

        var booking = new BookingModel
        {
            UserId = dto.UserId,
            OrderCode = orderCode,
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            EventDate = dto.EventDate,
            EventTime = dto.EventTime,
            EventId = dto.EventId,
            People = dto.People,
            Address = dto.Address,
            PaymentMethod = dto.PaymentMethod,
            RoomId = string.IsNullOrWhiteSpace(dto.RoomId) ? null : dto.RoomId,
            VenueId = string.IsNullOrWhiteSpace(dto.VenueId) ? null : dto.VenueId,
            MenuId = dto.MenuId,
            ServiceIds = dto.ServiceIds ?? new List<string>(),
            Notes = dto.Notes,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        await _bookings.InsertOneAsync(booking);

        // Lấy dữ liệu liên quan để tạo nội dung email
        var menu = await _menus.Find(m => m.Id == booking.MenuId).FirstOrDefaultAsync();
        var services = await _services.Find(s => booking.ServiceIds.Contains(s.Id)).ToListAsync();
        var venue = await _venues.Find(v => v.Id == booking.VenueId).FirstOrDefaultAsync();
        var room = await _rooms.Find(r => r.Id == booking.RoomId).FirstOrDefaultAsync();
        var ev = await _events.Find(e => e.Id == booking.EventId).FirstOrDefaultAsync();

        var dishList = new List<DishModel>();
        if (menu != null)
        {
            dishList = await _dishes.Find(d => menu.DishIds.Contains(d.Id)).ToListAsync();
        }

        // Tạo nội dung HTML cho email
        var emailBody = EmailTemplateHelper.GenerateBookingConfirmationEmail(
            booking,
            ev,
            venue,
            room,
            menu,
            dishList,
            services
        );

        // Gửi email
        await _emailService.SendEmailAsync(
            booking.Email,
            $"[CaterEase] Xác nhận đơn đặt tiệc - {booking.OrderCode}",
            emailBody
        );

        return Ok(booking);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId)
    {
        var bookings = await _bookings
            .Find(b => b.UserId == userId)
            .SortByDescending(b => b.CreatedAt)
            .ToListAsync();

        var menuIds = bookings.Select(b => b.MenuId).Distinct().ToList();
        var allMenus = await _menus.Find(m => menuIds.Contains(m.Id)).ToListAsync();

        var dishIds = allMenus.SelectMany(m => m.DishIds).Distinct().ToList();
        var allDishes = await _dishes.Find(d => dishIds.Contains(d.Id)).ToListAsync();

        var serviceIds = bookings.SelectMany(b => b.ServiceIds).Distinct().ToList();
        var allServices = await _services.Find(s => serviceIds.Contains(s.Id)).ToListAsync();

        var roomIds = bookings.Select(b => b.RoomId).Distinct().ToList();
        var allRooms = await _rooms.Find(r => roomIds.Contains(r.Id)).ToListAsync();

        var venueIds = bookings.Select(b => b.VenueId).Distinct().ToList();
        var allVenues = await _venues.Find(v => venueIds.Contains(v.Id)).ToListAsync();

        var eventIds = bookings.Select(b => b.EventId).Distinct().ToList();
        var allEvents = await _events.Find(e => eventIds.Contains(e.Id)).ToListAsync();

        var result = bookings.Select(b =>
        {
            var menu = allMenus.FirstOrDefault(m => m.Id == b.MenuId);
            var dishes = menu != null
                ? allDishes
                    .Where(d => menu.DishIds.Contains(d.Id))
                    .Select(d => (object)new { d.Id, d.Name })
                    .ToList()
                : new List<object>();

            var services = allServices
                .Where(s => b.ServiceIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name })
                .ToList();

            var room = allRooms.FirstOrDefault(r => r.Id == b.RoomId);
            var venue = allVenues.FirstOrDefault(v => v.Id == b.VenueId);
            var ev = allEvents.FirstOrDefault(e => e.Id == b.EventId);

            return new
            {
                b.Id,
                b.OrderCode,
                b.Name,
                b.Email,
                b.Phone,
                b.EventDate,
                b.EventTime,
                Event = ev != null ? new { ev.Id, ev.Name } : null,
                Venue = venue != null ? new { venue.Id, venue.Name } : null,
                Room = room != null ? new { room.Id, room.Name } : null,
                Menu = menu != null
                    ? new
                    {
                        menu.Id,
                        menu.Name,
                        menu.Price,
                        Dishes = dishes
                    }
                    : null,
                Services = services,
                b.People,
                b.Address,
                b.PaymentMethod,
                b.Notes,
                b.Status,
                b.CreatedAt
            };
        });

        return Ok(result);
    }

    [HttpPatch("update-user-id")]
    [Authorize]
    public async Task<IActionResult> UpdateUserId([FromBody] UpdateUserIdDto dto)
    {
        var currentUserId = User.FindFirst("userId")?.Value;

        if (currentUserId != dto.RealUserId)
            return Forbid("Bạn không có quyền cập nhật userId này.");

        var filter = Builders<BookingModel>.Filter.Eq(b => b.UserId, dto.AnonymousId);
        var update = Builders<BookingModel>.Update.Set(b => b.UserId, dto.RealUserId);

        var result = await _bookings.UpdateManyAsync(filter, update);

        return Ok(new
        {
            UpdatedCount = result.ModifiedCount,
            Message = $"{result.ModifiedCount} booking(s) đã được cập nhật sang userId mới."
        });
    }


    [Authorize]
    [HttpPatch("cancel/{id}")]
    public async Task<IActionResult> Cancel(string id, [FromBody] CancelBookingDto dto)
    {
        var booking = await _bookings.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (booking == null) return NotFound("Booking not found");

        if (booking.Status == "cancelled")
            return BadRequest("This booking is already cancelled.");

        var currentUserId = User.FindFirst("userId")?.Value;
        var isAdmin = User.IsInRole("admin");

        if (!isAdmin && booking.UserId != currentUserId)
            return Forbid();

        var update = Builders<BookingModel>.Update
            .Set(b => b.Status, "cancelled")
            .Set(b => b.CancelledAt, DateTime.UtcNow)
            .Set(b => b.CancelReason, dto.Reason)
            .Set(b => b.CancelledBy, isAdmin ? "admin" : "user");

        await _bookings.UpdateOneAsync(b => b.Id == id, update);
        return Ok("Booking cancelled successfully.");
    }

    [Authorize]
    [HttpPost("rebook/{id}")]
    public async Task<IActionResult> Rebook(string id)
    {
        var oldBooking = await _bookings.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (oldBooking == null) return NotFound("Old booking not found");

        if (oldBooking.Status != "cancelled")
            return BadRequest("Only cancelled bookings can be rebooked.");

        var currentUserId = User.FindFirst("userId")?.Value;
        var isAdmin = User.IsInRole("admin");

        if (!isAdmin && oldBooking.UserId != currentUserId)
            return Forbid();

        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var countToday = await _bookings.CountDocumentsAsync(b => b.CreatedAt.Date == DateTime.UtcNow.Date);
        var orderCode = $"CE-{today}-{(countToday + 1):D4}";

        var newBooking = new BookingModel
        {
            UserId = oldBooking.UserId,
            OrderCode = orderCode,
            Name = oldBooking.Name,
            Phone = oldBooking.Phone,
            Email = oldBooking.Email,
            EventDate = oldBooking.EventDate,
            EventTime = oldBooking.EventTime,
            EventId = oldBooking.EventId,
            People = oldBooking.People,
            Address = oldBooking.Address,
            PaymentMethod = oldBooking.PaymentMethod,
            RoomId = oldBooking.RoomId,
            MenuId = oldBooking.MenuId,
            VenueId = oldBooking.VenueId,
            ServiceIds = oldBooking.ServiceIds,
            Notes = oldBooking.Notes,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        await _bookings.InsertOneAsync(newBooking);
        return Ok(newBooking);
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("status/{id}")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateBookingStatusDto dto)
    {
        var booking = await _bookings.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (booking == null) return NotFound("Booking not found");

        if (booking.Status == dto.Status)
            return BadRequest("Status is already set.");

        var update = Builders<BookingModel>.Update
            .Set(b => b.Status, dto.Status);

        await _bookings.UpdateOneAsync(b => b.Id == id, update);

        // Lấy dữ liệu liên quan để gửi email
        var menu = await _menus.Find(m => m.Id == booking.MenuId).FirstOrDefaultAsync();
        var services = await _services.Find(s => booking.ServiceIds.Contains(s.Id)).ToListAsync();
        var venue = await _venues.Find(v => v.Id == booking.VenueId).FirstOrDefaultAsync();
        var room = await _rooms.Find(r => r.Id == booking.RoomId).FirstOrDefaultAsync();
        var ev = await _events.Find(e => e.Id == booking.EventId).FirstOrDefaultAsync();
        var dishList = new List<DishModel>();

        if (menu != null)
        {
            dishList = await _dishes.Find(d => menu.DishIds.Contains(d.Id)).ToListAsync();
        }

        // Tạo nội dung email
        var emailBody = EmailTemplateHelper.GenerateBookingStatusUpdateEmail(
            booking,
            dto.Status,
            ev,
            venue,
            room,
            menu,
            dishList,
            services
        );

        await _emailService.SendEmailAsync(
            booking.Email,
            $"[CaterEase] Cập nhật trạng thái đơn hàng - {booking.OrderCode}",
            emailBody
        );

        return Ok(new { message = "Booking status updated and email sent." });
    }


    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _bookings.DeleteOneAsync(b => b.Id == id);
        return result.DeletedCount > 0 ? Ok("Booking deleted") : NotFound("Booking not found");
    }
    
    
}