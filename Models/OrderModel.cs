using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace cater_ease_api.Models;

public class OrderModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? AuthId { get; set; }

    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Address { get; set; }
    public string Email { get; set; } = null!;
    public string? Note { get; set; }
    public int TableNumber { get; set; }
    public DateTime EventDate { get; set; }

    public List<OrderItem> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }

    public string Status { get; set; } = "pending"; // pending, confirmed, cancelled...
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OrderItem
{
    [BsonElement("DishId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Image { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}