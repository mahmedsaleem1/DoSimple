using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Server.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _logger = logger;
    }

    public async Task<string?> UploadImageAsync(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file provided for upload");
                return null;
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                _logger.LogWarning("Invalid file type: {ContentType}", file.ContentType);
                return null;
            }

            // Validate file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                _logger.LogWarning("File size too large: {Size} bytes", file.Length);
                return null;
            }

            using var stream = file.OpenReadStream();
            
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "dosimple/tasks",
                Transformation = new Transformation().Width(1200).Height(1200).Crop("limit").Quality("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("Image uploaded successfully: {PublicId}", uploadResult.PublicId);
                return uploadResult.SecureUrl.ToString();
            }

            _logger.LogError("Image upload failed: {Error}", uploadResult.Error?.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image to Cloudinary");
            return null;
        }
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return false;
            }

            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("Image deleted successfully: {PublicId}", publicId);
                return true;
            }

            _logger.LogWarning("Image deletion failed: {Error}", result.Error?.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image from Cloudinary");
            return false;
        }
    }
}
