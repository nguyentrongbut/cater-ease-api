using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System.Security.Cryptography;
using cater_ease_api.Models;

namespace cater_ease_api.Services;

public class MomoService : IMomoService
{
    private readonly MomoOptionModel _options;

    public MomoService(IOptions<MomoOptionModel> options)
    {
        _options = options.Value;
    }

    public async Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel model)
    {
        // Tạo OrderId duy nhất
        model.OrderId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        model.OrderInfo = $"Khách hàng: {model.FullName}. Nội dung: {model.OrderInfo}";

        var rawHash = $"partnerCode={_options.PartnerCode}" +
                      $"&accessKey={_options.AccessKey}" +
                      $"&requestId={model.OrderId}" +
                      $"&amount={model.Amount}" +
                      $"&orderId={model.OrderId}" +
                      $"&orderInfo={model.OrderInfo}" +
                      $"&returnUrl={_options.ReturnUrl}" +
                      $"&notifyUrl={_options.NotifyUrl}" +
                      $"&extraData=";

        var signature = ComputeHmacSha256(rawHash, _options.SecretKey);

        var requestBody = new
        {
            partnerCode = _options.PartnerCode,
            accessKey = _options.AccessKey,
            requestId = model.OrderId,
            orderId = model.OrderId,
            orderInfo = model.OrderInfo,
            amount = model.Amount,
            returnUrl = _options.ReturnUrl,
            notifyUrl = _options.NotifyUrl,
            extraData = "",
            requestType = _options.RequestType,
            signature = signature,
            lang = "vi"
        };

        var client = new RestClient(_options.MomoApiUrl);
        var request = new RestRequest("", Method.Post);
        request.AddHeader("Content-Type", "application/json");
        request.AddJsonBody(requestBody);

        var response = await client.ExecuteAsync(request);

        if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
        {
            throw new Exception("Lỗi khi gọi MoMo API");
        }

        var result = JsonConvert.DeserializeObject<MomoCreatePaymentResponseModel>(response.Content);
        return result;
    }

    public MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection)
    {
        var amount = collection["amount"];
        var orderInfo = collection["orderInfo"];
        var orderId = collection["orderId"];

        return new MomoExecuteResponseModel
        {
            Amount = amount,
            OrderId = orderId,
            OrderInfo = orderInfo
        };
    }

    private string ComputeHmacSha256(string message, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(messageBytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}
