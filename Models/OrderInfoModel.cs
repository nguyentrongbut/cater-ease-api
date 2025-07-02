namespace cater_ease_api.Models;

public class OrderInfoModel
{
    public string FullName { get; set; }
    public string Amount { get; set; }
    public string OrderInfo { get; set; }
    public string OrderId { get; set; }  // Sẽ được gán bên trong MomoService
    public string ExtraData { get; set; }
}