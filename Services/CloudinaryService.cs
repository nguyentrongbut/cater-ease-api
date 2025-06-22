using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace cater_ease_api.Services;

public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration config)
    {
        var cloudName = config["Cloudinary:CloudName"];
        var apiKey = config["Cloudinary:ApiKey"];
        var apiSecret = config["Cloudinary:ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            throw new Exception("Cloudinary config is missing or invalid.");

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
    }

    public async Task<string> UploadAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };
        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        return uploadResult.SecureUrl.ToString();
    }
    
    public async Task DeleteAsync(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return;

        var uri = new Uri(imageUrl);
        var path = uri.AbsolutePath;
        
        var index = path.IndexOf("/upload/");
        if (index == -1) return;

        var publicPart = path[(index + "/upload/".Length)..];
        var filename = Path.GetFileNameWithoutExtension(publicPart); 

        var deleteParams = new DeletionParams(filename);
        var result = await _cloudinary.DestroyAsync(deleteParams);

        if (result.Result != "ok")
            Console.WriteLine($"Failed to delete Cloudinary image: {imageUrl} — Result: {result.Result}");
        else
            Console.WriteLine($"Successfully deleted: {filename}");
    }
}
