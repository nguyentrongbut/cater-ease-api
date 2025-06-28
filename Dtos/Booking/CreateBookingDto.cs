using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Booking;

public class CreateBookingDto
{
    [Required] public string AuthId { get; set; } = null!;
    [Required] public string Name { get; set; } = null!;
    [Required] public string Phone { get; set; } = null!;
    [Required, EmailAddress] public string Email { get; set; } = null!;
    [Required] public string EventName { get; set; } = null!;
    [Required] public string LocationType { get; set; } = null!;
    [Required] public int People { get; set; }
    [Required] public string Address { get; set; } = null!;
    [Required] public string EventTime { get; set; } = null!;
    [Required] public string EventDate { get; set; } = null!;
    public List<string> MenuIds { get; set; } = new();
    public List<string> ServiceIds { get; set; } = new();
    public string? Note { get; set; }
}