using System.ComponentModel.DataAnnotations;

namespace cater_ease_api.Dtos.Booking;

public class UpdateBookingStatusDto
{
    [Required]
    public string Status { get; set; } = null!;
}