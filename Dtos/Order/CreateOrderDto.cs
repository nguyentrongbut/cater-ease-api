namespace cater_ease_api.Dtos.Order;

public class CreateOrderDto
{
    public string? AuthId { get; set; }

    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Note { get; set; }
    public string Address { get; set; } = null!;
    public DateTime EventDate { get; set; }
    public int TableNumber { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
   
}

public class OrderItemDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Image { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}