
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

    public PaymentController(
        IMomoService momoService,
        MongoDbService mongoDbService)
    {
        _momoService = momoService;
        _bookings = mongoDbService.Database.GetCollection<BookingModel>("bookings");
        _menus = mongoDbService.Database.GetCollection<MenuModel>("menus");
        _services = mongoDbService.Database.GetCollection<ServiceModel>("services");
    }

    [HttpPost("momo")]
    public async Task<IActionResult> CreateMomoPayment([FromBody] BookingPaymentDto dto)
    {
        if (string.IsNullOrEmpty(dto.BookingId))
            return BadRequest(new { message = "bookingId is required" });

        var booking = await _bookings.Find(b => b.Id == dto.BookingId).FirstOrDefaultAsync();
        if (booking == null)
            return NotFound("Booking not found");

        var menu = await _menus.Find(m => m.Id == booking.MenuId).FirstOrDefaultAsync();
        var services = await _services.Find(s => booking.ServiceIds.Contains(s.Id)).ToListAsync();

        decimal totalAmount = menu?.Price ?? 0;
        
        totalAmount += services.Sum(s => (decimal)s.Price);

        decimal amountToPay = booking.PaymentMethod == "deposit"
            ? Math.Round(totalAmount * 0.3M, 0)
            : totalAmount;

        var momoResponse = await _momoService.CreatePaymentAsync(new OrderInfoModel
        {
            FullName = booking.Name,
            Amount = amountToPay.ToString(),
            OrderInfo = $"Thanh toán {booking.PaymentMethod} cho đơn {booking.OrderCode}"
        });

        return Ok(new
        {
            momoPayUrl = momoResponse.PayUrl,
            amountToPay
        });
    }
}
