namespace cater_ease_api.Dtos.Cart;

public class CartItemDto
{
    public string Id { get; set; } = null!;
    public string AuthId { get; set; } = null!;
    public string DishId { get; set; } = null!;
    
    public string DishName { get; set; } = null!;
    public string DishImage { get; set; } = null!;
    
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal SubTotal => Price * Quantity;
}