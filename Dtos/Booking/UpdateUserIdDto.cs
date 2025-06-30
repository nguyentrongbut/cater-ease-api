namespace cater_ease_api.Dtos.Booking
{
    public class UpdateUserIdDto
    {
        public string AnonymousId { get; set; } = null!;
        public string RealUserId { get; set; } = null!;
    }
}