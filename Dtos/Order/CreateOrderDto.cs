namespace cater_ease_api.Dtos.Order;

public class CreateOrderDto
{
    public string? AuthId { get; set; }

    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Note { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }
}

public class OrderItemDto
{
    public string DishId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Image { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}