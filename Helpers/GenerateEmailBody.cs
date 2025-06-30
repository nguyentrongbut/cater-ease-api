using cater_ease_api.Models;

namespace cater_ease_api.Helpers;

public static class EmailTemplateHelper
{
    public static string GenerateBookingConfirmationEmail(
        BookingModel booking,
        EventModel? ev,
        VenueModel? venue,
        RoomModel? room,
        MenuModel? menu,
        List<DishModel> dishes,
        List<ServiceModel> services)
    {
        var serviceNames = string.Join(", ", services.Select(s => s.Name));
        var dishHtmlList = string.Join("", dishes.Select(dish =>
            $"<span style='display:inline-block;background:#fff;padding:4px 8px;margin:2px;border-radius:4px;font-size:14px;'>{dish.Name}</span>"
        ));

        var statusColor = booking.Status?.ToLower() switch
        {
            "pending" => "#f59e0b", // vàng
            "confirmed" => "#3b82f6", // xanh dương
            "paid" => "#10b981", // xanh lá
            "cancelled" => "#ef4444", // đỏ
            _ => "#6b7280"
        };

        var statusLabel = booking.Status?.ToLower() switch
        {
            "pending" => "🕐 Chờ xử lý",
            "confirmed" => "✅ Đã xác nhận",
            "paid" => "💰 Đã thanh toán",
            "cancelled" => "❌ Đã hủy",
            _ => "🕐 Chờ xử lý"
        };

        var nextStepMessage = booking.Status?.ToLower() switch
        {
            "pending" => "Chúng tôi sẽ sớm liên hệ với bạn trong vòng 24 giờ để xác nhận đơn hàng.",
            "confirmed" => "Đơn hàng của bạn đã được xác nhận. Vui lòng tiến hành thanh toán để chúng tôi giữ chỗ.",
            "paid" => "Đơn hàng đã được thanh toán. Chúng tôi sẽ chuẩn bị mọi thứ cho sự kiện của bạn.",
            "cancelled" => "Đơn hàng đã bị hủy. Nếu có bất kỳ thay đổi nào, vui lòng liên hệ lại với chúng tôi.",
            _ => "Chúng tôi sẽ sớm liên hệ với bạn để xác nhận đơn hàng."
        };


        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family:Segoe UI,Tahoma,Geneva,Verdana,sans-serif; background:#f4f4f4; color:#333; }}
        .container {{ max-width:600px;margin:20px auto;background:white;padding:30px;border-radius:12px;box-shadow:0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background:#667eea;color:white;padding:20px;border-radius:8px 8px 0 0;text-align:center; }}
        .info-row {{ display:flex;justify-content:space-between;padding:6px 0;border-bottom:1px solid #eee; }}
        .info-label {{ font-weight:bold; }}
        .info-value {{ text-align:right; }}
        .next-step {{ background:#e7f3ff;padding:15px;border-left:4px solid #0056b3;margin-top:20px;border-radius:4px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>CaterEase - Đặt tiệc thành công</h2>
        </div>
        <p>Xin chào <strong>{booking.Name}</strong>,</p>
        <p>Bạn đã đặt tiệc thành công trên hệ thống CaterEase. Dưới đây là thông tin chi tiết:</p>

        <div class='info-row'><span class='info-label'>Mã đơn hàng:</span><span class='info-value'>{booking.OrderCode}</span></div>
        <div class='info-row'><span class='info-label'>Trạng thái:</span><span class='info-value' style='color:{statusColor}'>{statusLabel}</span></div>
        <div class='info-row'><span class='info-label'>Ngày tổ chức:</span><span class='info-value'>{booking.EventDate:dd/MM/yyyy} lúc {booking.EventTime}</span></div>
        <div class='info-row'><span class='info-label'>Sự kiện:</span><span class='info-value'>{ev?.Name}</span></div>
        <div class='info-row'><span class='info-label'>Địa điểm:</span><span class='info-value'>{venue?.Name} - {room?.Name}</span></div>
        <div class='info-row'><span class='info-label'>Thực đơn:</span><span class='info-value'>{menu?.Name} ({menu?.Price:N0} VND)</span></div>

        <h4 style='margin-top:20px;'>🍽️ Món ăn:</h4>
        <div>{dishHtmlList}</div>

        {(string.IsNullOrEmpty(serviceNames) ? "" : $"<h4 style='margin-top:20px;'>⭐ Dịch vụ kèm theo:</h4><div>{serviceNames}</div>")}

        <div class='next-step'>
            <strong>Bước tiếp theo:</strong><br>
            {nextStepMessage}
        </div>

        <p style='margin-top:30px;'>Trân trọng,<br><em>CaterEase Team</em></p>
    </div>
</body>
</html>
";
    }
    
    public static string GenerateBookingStatusUpdateEmail(
    BookingModel booking,
    string newStatus,
    EventModel? ev,
    VenueModel? venue,
    RoomModel? room,
    MenuModel? menu,
    List<DishModel> dishes,
    List<ServiceModel> services)
{
    var serviceNames = string.Join(", ", services.Select(s => s.Name));
    var dishHtmlList = string.Join("", dishes.Select(dish =>
        $"<span style='display:inline-block;background:#fff;padding:4px 8px;margin:2px;border-radius:4px;font-size:14px;'>{dish.Name}</span>"
    ));

    var statusColor = newStatus.ToLower() switch
    {
        "pending" => "#f59e0b", // vàng
        "confirmed" => "#3b82f6", // xanh dương
        "paid" => "#10b981", // xanh lá
        "cancelled" => "#ef4444", // đỏ
        _ => "#6b7280"
    };

    var statusLabel = newStatus.ToLower() switch
    {
        "pending" => "🕐 Chờ xử lý",
        "confirmed" => "✅ Đã xác nhận",
        "paid" => "💰 Đã thanh toán",
        "cancelled" => "❌ Đã hủy",
        _ => "🕐 Chờ xử lý"
    };

    var updateMessage = newStatus.ToLower() switch
    {
        "pending" => "Đơn hàng của bạn đang trong trạng thái chờ xử lý.",
        "confirmed" => "Đơn hàng của bạn đã được xác nhận. Vui lòng chuẩn bị thanh toán.",
        "paid" => "Chúng tôi đã nhận được thanh toán. Cảm ơn bạn!",
        "cancelled" => "Rất tiếc, đơn hàng của bạn đã bị hủy.",
        _ => "Trạng thái đơn hàng đã được cập nhật."
    };

    return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family:Segoe UI,Tahoma,Geneva,Verdana,sans-serif; background:#f4f4f4; color:#333; }}
        .container {{ max-width:600px;margin:20px auto;background:white;padding:30px;border-radius:12px;box-shadow:0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background:#10b981;color:white;padding:20px;border-radius:8px 8px 0 0;text-align:center; }}
        .info-row {{ display:flex;justify-content:space-between;padding:6px 0;border-bottom:1px solid #eee; }}
        .info-label {{ font-weight:bold; }}
        .info-value {{ text-align:right; }}
        .note {{ background:#e7f3ff;padding:15px;border-left:4px solid #0056b3;margin-top:20px;border-radius:4px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Cập nhật trạng thái đơn hàng</h2>
        </div>
        <p>Xin chào <strong>{booking.Name}</strong>,</p>
        <p>Trạng thái đơn hàng <strong>{booking.OrderCode}</strong> của bạn vừa được cập nhật:</p>

        <div class='info-row'><span class='info-label'>Trạng thái mới:</span><span class='info-value' style='color:{statusColor}'>{statusLabel}</span></div>
        <div class='info-row'><span class='info-label'>Ngày tổ chức:</span><span class='info-value'>{booking.EventDate:dd/MM/yyyy} lúc {booking.EventTime}</span></div>
        <div class='info-row'><span class='info-label'>Sự kiện:</span><span class='info-value'>{ev?.Name}</span></div>
        <div class='info-row'><span class='info-label'>Địa điểm:</span><span class='info-value'>{venue?.Name} - {room?.Name}</span></div>
        <div class='info-row'><span class='info-label'>Thực đơn:</span><span class='info-value'>{menu?.Name} ({menu?.Price:N0} VND)</span></div>

        <h4 style='margin-top:20px;'>🍽️ Món ăn:</h4>
        <div>{dishHtmlList}</div>

        {(string.IsNullOrEmpty(serviceNames) ? "" : $"<h4 style='margin-top:20px;'>⭐ Dịch vụ kèm theo:</h4><div>{serviceNames}</div>")}

        <div class='note'>
            <strong>Ghi chú:</strong><br>
            {updateMessage}
        </div>

        <p style='margin-top:30px;'>Trân trọng,<br><em>CaterEase Team</em></p>
    </div>
</body>
</html>
";
}

}