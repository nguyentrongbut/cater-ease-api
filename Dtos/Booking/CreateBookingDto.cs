using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Booking;

public class CreateBookingDto
{
    [Required] public string UserId { get; set; } = null!;

    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Phone is required")]
    public string Phone { get; set; } = null!;

    [Required(ErrorMessage = "EventDate is required")]
    public string EventDate { get; set; } = null!;

    [Required(ErrorMessage = "EventTime is required")]
    public string EventTime { get; set; } = null!;

    [Required(ErrorMessage = "EventId is required")]
    public string EventId { get; set; } = null!;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "People must be greater than 0")]
    public int People { get; set; }

    public string Address { get; set; } = null!;

    [Required(ErrorMessage = "Payment method is required")]
    [RegularExpression("^(full|deposit)$", ErrorMessage = "PaymentMethod must be 'full' or 'deposit'")]
    public string PaymentMethod { get; set; } = null!;

    public string? RoomId { get; set; }   // nullable
    [Required(ErrorMessage = "MenuId is required")]
    public string MenuId { get; set; } = null!;
    public string? VenueId { get; set; }  // nullable

    public List<string> ServiceIds { get; set; } = new();
    public string? Notes { get; set; }
}
