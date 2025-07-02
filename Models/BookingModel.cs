using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace cater_ease_api.Models;

public class BookingModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public string UserId { get; set; } = null!;

    [Required]
    public string OrderCode { get; set; } = null!;

    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Phone is required")]
    public string Phone { get; set; } = null!;

    [Required(ErrorMessage = "Event date is required")]
    public string EventDate { get; set; } = null!;

    [Required(ErrorMessage = "Event time is required")]
    public string EventTime { get; set; } = null!;

    [Required(ErrorMessage = "EventId is required")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string EventId { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "People must be greater than 0")]
    public int People { get; set; }

    public string? Address { get; set; }

    [Required(ErrorMessage = "Payment method is required")]
    [RegularExpression("^(full|deposit)$", ErrorMessage = "Payment method must be 'full' or 'deposit'")]
    public string PaymentMethod { get; set; } = null!;

    [Required(ErrorMessage = "MenuId is required")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string MenuId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]  // không serialize nếu null
    public string? RoomId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? VenueId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> ServiceIds { get; set; } = new();

    public string? Notes { get; set; }

    public string Status { get; set; } = "pending";
    // Trạng thái thanh toán thực tế: unpaid, partial, paid
    public string PaymentStatus { get; set; } = "unpaid";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public string? CancelledBy { get; set; } // "user" | "admin"
}
