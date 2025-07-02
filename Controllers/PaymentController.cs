using System.Text.Json;
using cater_ease_api.Data;
using cater_ease_api.Dtos.Payment;
using cater_ease_api.Models;
using cater_ease_api.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly IMomoService _momoService;
    private readonly IMongoCollection<BookingModel> _bookings;
    private readonly IMongoCollection<MenuModel> _menus;
    private readonly IMongoCollection<ServiceModel> _services;
    private readonly IMongoCollection<RoomModel> _rooms;

    public PaymentController(
        IMomoService momoService,
        MongoDbService mongoDbService)
    {
        _momoService = momoService;
        _bookings = mongoDbService.Database.GetCollection<BookingModel>("bookings");
        _menus = mongoDbService.Database.GetCollection<MenuModel>("menus");
        _services = mongoDbService.Database.GetCollection<ServiceModel>("services");
        _rooms = mongoDbService.Database.GetCollection<RoomModel>("rooms");
    }

    [HttpPost("momo")]
    public async Task<IActionResult> CreateMomoPayment([FromBody] BookingPaymentDto dto)
    {
        if (string.IsNullOrEmpty(dto.BookingId))
            return BadRequest(new { message = "bookingId is required" });

        var booking = await _bookings.Find(b => b.Id == dto.BookingId).FirstOrDefaultAsync();
        if (booking == null)
            return NotFound("Booking not found");

        // Truy vấn các dữ liệu liên quan
        var menu = await _menus.Find(m => m.Id == booking.MenuId).FirstOrDefaultAsync();
        var room = await _rooms.Find(r => r.Id == booking.RoomId).FirstOrDefaultAsync();
        var services = await _services.Find(s => booking.ServiceIds.Contains(s.Id)).ToListAsync();

        // Tính toán giá từng phần
        decimal menuTotal = (decimal)(menu?.Price ?? 0) * booking.People;
        decimal roomPrice = (decimal)(room?.Price ?? 0);
        decimal serviceTotal = services.Sum(s => (decimal)s.Price);

        // Tổng giá trị cần thanh toán
        decimal totalAmount = menuTotal + roomPrice + serviceTotal;

        // Áp dụng phương thức thanh toán (deposit hoặc full)
        decimal amountToPay = booking.PaymentMethod == "deposit"
            ? Math.Round(totalAmount * 0.3M, 0)
            : totalAmount;

        // Gọi MomoService để tạo link thanh toán
        var momoResponse = await _momoService.CreatePaymentAsync(new OrderInfoModel
        {
            FullName = booking.Name,
            Amount = amountToPay.ToString(),
            OrderInfo = $"Thanh toán {booking.PaymentMethod} cho đơn {booking.OrderCode}",
            ExtraData = booking.Id // Gửi kèm bookingId
        });

        return Ok(new
        {
            momoPayUrl = momoResponse.PayUrl,
            amountToPay,
            totalAmount
        });
    }


    [HttpPost("momo/callback")]
    public async Task<IActionResult> MomoCallback([FromBody] MomoNotifyModel payload)
    {
        try
        {
            Console.WriteLine("Received callback payload:");
            Console.WriteLine(JsonSerializer.Serialize(payload));

            if (payload.errorCode != "0") return Ok();

            var bookingId = payload.extraData;
            var booking = await _bookings.Find(b => b.Id == bookingId).FirstOrDefaultAsync();
            if (booking == null) return NotFound("Booking not found");
    
            var newStatus = booking.PaymentMethod == "deposit" ? "confirmed" : "paid";
            var paymentStatus = booking.PaymentMethod == "deposit" ? "partial" : "paid";

            var update = Builders<BookingModel>.Update
                .Set(b => b.Status, newStatus)
                .Set(b => b.PaymentStatus, paymentStatus);

            await _bookings.UpdateOneAsync(b => b.Id == booking.Id, update);

            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Callback Exception: " + ex.Message);
            return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
        }
    }

}