using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;

namespace MaiAmTinhThuong.Services
{
    public interface IImageUploadService
    {
        Task<string> UploadImageAsync(IFormFile file, string folder = "maiam");
        Task<bool> DeleteImageAsync(string imageUrl);
    }

    public class ImageUploadService : IImageUploadService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<ImageUploadService> _logger;

        public ImageUploadService(IConfiguration configuration, ILogger<ImageUploadService> logger)
        {
            _logger = logger;
            
            // Log để debug - chi tiết hơn
            _logger.LogInformation("🔍 Checking Cloudinary configuration...");
            
            // Đọc từ config hoặc environment variables, và trim whitespace
            var cloudNameFromConfig = configuration["Cloudinary:CloudName"];
            var cloudNameFromEnv = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
            var cloudName = (cloudNameFromConfig ?? cloudNameFromEnv)?.Trim();
            
            var apiKeyFromConfig = configuration["Cloudinary:ApiKey"];
            var apiKeyFromEnv = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");
            var apiKey = (apiKeyFromConfig ?? apiKeyFromEnv)?.Trim();
            
            var apiSecretFromConfig = configuration["Cloudinary:ApiSecret"];
            var apiSecretFromEnv = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");
            var apiSecret = (apiSecretFromConfig ?? apiSecretFromEnv)?.Trim();
            
            // Log chi tiết với giá trị (masked)
            _logger.LogInformation($"CloudName - Config: {!string.IsNullOrWhiteSpace(cloudNameFromConfig)}, Env: {!string.IsNullOrWhiteSpace(cloudNameFromEnv)}, Value: {(string.IsNullOrWhiteSpace(cloudName) ? "EMPTY" : cloudName.Substring(0, Math.Min(3, cloudName.Length)) + "***")}");
            _logger.LogInformation($"API Key - Config: {!string.IsNullOrWhiteSpace(apiKeyFromConfig)}, Env: {!string.IsNullOrWhiteSpace(apiKeyFromEnv)}, Value: {(string.IsNullOrWhiteSpace(apiKey) ? "EMPTY" : apiKey.Substring(0, Math.Min(3, apiKey.Length)) + "***")}");
            _logger.LogInformation($"API Secret - Config: {!string.IsNullOrWhiteSpace(apiSecretFromConfig)}, Env: {!string.IsNullOrWhiteSpace(apiSecretFromEnv)}, Value: {(string.IsNullOrWhiteSpace(apiSecret) ? "EMPTY" : "***" + apiSecret.Substring(Math.Max(0, apiSecret.Length - 3)))}");
            
            // Log tất cả environment variables có chứa CLOUDINARY để debug
            var allEnvVars = Environment.GetEnvironmentVariables();
            var cloudinaryVars = allEnvVars.Keys.Cast<string>()
                .Where(k => k.Contains("CLOUDINARY", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (cloudinaryVars.Any())
            {
                _logger.LogInformation($"Found Cloudinary-related env vars: {string.Join(", ", cloudinaryVars)}");
                
                // Log giá trị thực tế (masked) của từng biến
                foreach (var varName in cloudinaryVars)
                {
                    var varValue = Environment.GetEnvironmentVariable(varName);
                    if (!string.IsNullOrWhiteSpace(varValue))
                    {
                        var maskedValue = varValue.Length > 6 
                            ? varValue.Substring(0, 3) + "***" + varValue.Substring(varValue.Length - 3)
                            : "***";
                        _logger.LogInformation($"  {varName} = {maskedValue} (Length: {varValue.Length})");
                    }
                    else
                    {
                        _logger.LogWarning($"  {varName} = EMPTY or NULL (Length: {varValue?.Length ?? 0})");
                    }
                }
            }
            else
            {
                _logger.LogWarning("⚠️ No CLOUDINARY environment variables found!");
            }

            if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            {
                var errorMsg = "Cloudinary configuration is missing! " +
                    "Please set the following environment variables in Railway: " +
                    "CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY, CLOUDINARY_API_SECRET. " +
                    "Or add them to appsettings.json for local development. " +
                    $"Current status - CloudName: {(string.IsNullOrWhiteSpace(cloudName) ? "MISSING" : "OK")}, " +
                    $"API Key: {(string.IsNullOrWhiteSpace(apiKey) ? "MISSING" : "OK")}, " +
                    $"API Secret: {(string.IsNullOrWhiteSpace(apiSecret) ? "MISSING" : "OK")}";
                _logger.LogError(errorMsg);
                throw new Exception(errorMsg);
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _logger.LogInformation($"Cloudinary initialized successfully with CloudName: {cloudName}");
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder = "maiam")
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty");
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException("Invalid file type. Only images are allowed (jpg, jpeg, png, gif, webp).");
            }

            // Validate file size (max 10MB for Cloudinary free tier)
            if (file.Length > 10 * 1024 * 1024)
            {
                throw new ArgumentException("File size exceeds 10MB limit");
            }

            try
            {
                // Convert IFormFile to byte array
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                // Generate unique public ID
                var publicId = $"{folder}/{Guid.NewGuid()}";

                // Upload to Cloudinary
                // Note: ResourceType is read-only and defaults to Image for ImageUploadParams
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, new MemoryStream(fileBytes)),
                    PublicId = publicId,
                    Overwrite = false,
                    Transformation = new Transformation()
                        .Quality("auto") // Auto optimize quality
                        .FetchFormat("auto") // Auto format (webp if supported)
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation($"Image uploaded successfully to Cloudinary: {uploadResult.SecureUrl}");
                    return uploadResult.SecureUrl.ToString(); // Return HTTPS URL
                }
                else
                {
                    _logger.LogError($"Failed to upload image to Cloudinary: {uploadResult.Error?.Message}");
                    throw new Exception($"Upload failed: {uploadResult.Error?.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to Cloudinary");
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return false;

            // Only delete if it's a Cloudinary URL
            if (!imageUrl.Contains("res.cloudinary.com"))
            {
                _logger.LogWarning($"Not a Cloudinary URL, skipping delete: {imageUrl}");
                return false;
            }

            try
            {
                // Extract public_id from Cloudinary URL
                // URL format: https://res.cloudinary.com/{cloud_name}/image/upload/{folder}/{public_id}.{ext}
                var uri = new Uri(imageUrl);
                var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                
                // Find the index of "upload" in the path
                var uploadIndex = Array.IndexOf(pathParts, "upload");
                if (uploadIndex == -1 || uploadIndex >= pathParts.Length - 1)
                {
                    _logger.LogWarning($"Invalid Cloudinary URL format: {imageUrl}");
                    return false;
                }

                // Get public_id (everything after "upload" minus the extension)
                var publicIdParts = pathParts.Skip(uploadIndex + 1).ToArray();
                var publicId = string.Join("/", publicIdParts);
                
                // Remove file extension
                publicId = Path.ChangeExtension(publicId, null);

                var deleteParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image
                };

                var result = await _cloudinary.DestroyAsync(deleteParams);
                
                if (result.Result == "ok")
                {
                    _logger.LogInformation($"Image deleted successfully from Cloudinary: {publicId}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Failed to delete image from Cloudinary: {result.Result}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting image from Cloudinary: {imageUrl}");
                return false;
            }
        }
    }
}

