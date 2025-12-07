# 📸 Hướng Dẫn Tích Hợp Cloudinary cho Lưu Trữ Ảnh

## Bước 1: Đăng ký tài khoản Cloudinary

1. Truy cập: https://cloudinary.com/users/register/free
2. Đăng ký tài khoản miễn phí (Free tier: 25GB storage, 25GB bandwidth/tháng)
3. Sau khi đăng ký, vào **Dashboard** → **Settings** → **Security**
4. Copy các thông tin sau:
   - **Cloud Name** (ví dụ: `dxyz123`)
   - **API Key** (ví dụ: `123456789012345`)
   - **API Secret** (ví dụ: `abcdefghijklmnopqrstuvwxyz`)

## Bước 2: Cài đặt Package

Mở terminal/PowerShell trong thư mục project và chạy:

```bash
dotnet add package CloudinaryDotNet
```

Hoặc thêm vào `MaiAmTinhThuong.csproj`:

```xml
<PackageReference Include="CloudinaryDotNet" Version="1.21.0" />
```

## Bước 3: Thêm Configuration vào appsettings.json

Thêm section `Cloudinary` vào `appsettings.json`:

```json
{
  "Cloudinary": {
    "CloudName": "your-cloud-name",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret"
  }
}
```

**⚠️ QUAN TRỌNG**: Đối với Railway production, thêm các biến môi trường:
- `CLOUDINARY_CLOUD_NAME`
- `CLOUDINARY_API_KEY`
- `CLOUDINARY_API_SECRET`

## Bước 4: Tạo ImageUploadService

Tạo file `Services/ImageUploadService.cs`:

```csharp
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;

namespace MaiAmTinhThuong.Services
{
    public interface IImageUploadService
    {
        Task<string> UploadImageAsync(IFormFile file, string folder = "maiam");
        Task<bool> DeleteImageAsync(string publicId);
    }

    public class ImageUploadService : IImageUploadService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<ImageUploadService> _logger;

        public ImageUploadService(IConfiguration configuration, ILogger<ImageUploadService> logger)
        {
            var cloudName = configuration["Cloudinary:CloudName"] 
                ?? Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
            var apiKey = configuration["Cloudinary:ApiKey"] 
                ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");
            var apiSecret = configuration["Cloudinary:ApiSecret"] 
                ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                throw new Exception("Cloudinary configuration is missing!");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _logger = logger;
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
                throw new ArgumentException("Invalid file type. Only images are allowed.");
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

                // Upload to Cloudinary
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, new MemoryStream(fileBytes)),
                    Folder = folder, // Organize images in folders
                    PublicId = Guid.NewGuid().ToString(), // Unique ID
                    Overwrite = false,
                    ResourceType = ResourceType.Image,
                    Transformation = new Transformation()
                        .Quality("auto") // Auto optimize quality
                        .FetchFormat("auto") // Auto format (webp if supported)
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation($"Image uploaded successfully: {uploadResult.SecureUrl}");
                    return uploadResult.SecureUrl.ToString(); // Return HTTPS URL
                }
                else
                {
                    _logger.LogError($"Failed to upload image: {uploadResult.Error?.Message}");
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

            try
            {
                // Extract public_id from Cloudinary URL
                // URL format: https://res.cloudinary.com/{cloud_name}/image/upload/{folder}/{public_id}.{ext}
                var uri = new Uri(imageUrl);
                var pathParts = uri.AbsolutePath.Split('/');
                
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
                    _logger.LogInformation($"Image deleted successfully: {publicId}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Failed to delete image: {result.Result}");
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
```

## Bước 5: Đăng ký Service trong Program.cs

Thêm vào `Program.cs` trong method `builder.Services.Add...`:

```csharp
// Add Cloudinary Image Upload Service
builder.Services.AddScoped<IImageUploadService, ImageUploadService>();
```

## Bước 6: Cập nhật Controllers

### Ví dụ 1: AccountController (Profile Picture)

**Trước:**
```csharp
var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
if (!Directory.Exists(uploadDir))
{
    Directory.CreateDirectory(uploadDir);
}
var filePath = Path.Combine(uploadDir, uniqueFileName);
using (var stream = new FileStream(filePath, FileMode.Create))
{
    await profilePicture.CopyToAsync(stream);
}
user.ProfilePicture = "/images/profiles/" + uniqueFileName;
```

**Sau:**
```csharp
// Inject service vào constructor
private readonly IImageUploadService _imageUploadService;

public AccountController(..., IImageUploadService imageUploadService)
{
    _imageUploadService = imageUploadService;
}

// Trong action method:
try
{
    var imageUrl = await _imageUploadService.UploadImageAsync(profilePicture, "profiles");
    user.ProfilePicture = imageUrl;
}
catch (Exception ex)
{
    ModelState.AddModelError("profilePicture", $"Lỗi khi upload ảnh: {ex.Message}");
}
```

### Ví dụ 2: BlogController

**Trước:**
```csharp
var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
var imagePath = Path.Combine(uploadDir, uniqueFileName);
using (var stream = new FileStream(imagePath, FileMode.Create))
{
    await image.CopyToAsync(stream);
}
blogPost.ImageUrl = "/images/" + uniqueFileName;
```

**Sau:**
```csharp
// Inject service
private readonly IImageUploadService _imageUploadService;

// Trong action:
try
{
    var imageUrl = await _imageUploadService.UploadImageAsync(image, "blog");
    blogPost.ImageUrl = imageUrl;
}
catch (Exception ex)
{
    ModelState.AddModelError("image", $"Lỗi khi upload ảnh: {ex.Message}");
}
```

### Ví dụ 3: SupportRequestController

**Trước:**
```csharp
var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles");
var filePath = Path.Combine(uploadDir, uniqueFileName);
using (var stream = new FileStream(filePath, FileMode.Create))
{
    await ImageFile.CopyToAsync(stream);
}
model.ImageUrl = "/images/profiles/" + uniqueFileName;
```

**Sau:**
```csharp
// Inject service
private readonly IImageUploadService _imageUploadService;

// Trong action:
try
{
    var imageUrl = await _imageUploadService.UploadImageAsync(ImageFile, "support-requests");
    model.ImageUrl = imageUrl;
}
catch (Exception ex)
{
    ModelState.AddModelError("ImageFile", $"Lỗi khi upload ảnh: {ex.Message}");
}
```

## Bước 7: Cấu hình Railway Environment Variables

1. Vào Railway Dashboard → Project → Service
2. Click vào **Variables** tab
3. Thêm 3 biến môi trường:
   - `CLOUDINARY_CLOUD_NAME` = `your-cloud-name`
   - `CLOUDINARY_API_KEY` = `your-api-key`
   - `CLOUDINARY_API_SECRET` = `your-api-secret`

4. Click **Deploy** để apply changes

## Bước 8: Xóa ảnh cũ khi cập nhật (Optional)

Nếu muốn xóa ảnh cũ trên Cloudinary khi user upload ảnh mới:

```csharp
// Trong AccountController UpdateProfile
if (profilePicture != null && !string.IsNullOrEmpty(user.ProfilePicture))
{
    // Xóa ảnh cũ trên Cloudinary nếu là Cloudinary URL
    if (user.ProfilePicture.StartsWith("https://res.cloudinary.com"))
    {
        await _imageUploadService.DeleteImageAsync(user.ProfilePicture);
    }
    
    // Upload ảnh mới
    var imageUrl = await _imageUploadService.UploadImageAsync(profilePicture, "profiles");
    user.ProfilePicture = imageUrl;
}
```

## Lợi ích của Cloudinary

✅ **Persistent Storage**: Ảnh không bị mất khi redeploy  
✅ **CDN**: Ảnh được serve từ CDN, load nhanh hơn  
✅ **Auto Optimization**: Tự động optimize format và quality  
✅ **Transformations**: Có thể resize, crop ảnh on-the-fly  
✅ **Free Tier**: 25GB storage + 25GB bandwidth/tháng (đủ cho nhiều dự án nhỏ)

## Troubleshooting

### Lỗi: "Cloudinary configuration is missing!"
- Kiểm tra `appsettings.json` hoặc Railway environment variables
- Đảm bảo tên biến đúng: `CLOUDINARY_CLOUD_NAME`, `CLOUDINARY_API_KEY`, `CLOUDINARY_API_SECRET`

### Lỗi: "Invalid file type"
- Chỉ chấp nhận: `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`

### Lỗi: "File size exceeds limit"
- Cloudinary free tier: max 10MB/file
- Có thể tăng lên 20MB nếu upgrade plan

### Ảnh không hiển thị
- Kiểm tra URL trả về có đúng format HTTPS không
- Kiểm tra CORS settings trong Cloudinary Dashboard (Settings → Security)

## Migration Strategy

1. **Phase 1**: Setup Cloudinary và test với 1 controller (ví dụ: BlogController)
2. **Phase 2**: Migrate các controller còn lại
3. **Phase 3**: (Optional) Migrate ảnh cũ từ local lên Cloudinary nếu cần

## Test

Sau khi setup, test bằng cách:
1. Upload ảnh mới qua form
2. Kiểm tra URL trong database (phải là Cloudinary URL)
3. Kiểm tra ảnh hiển thị trên website
4. Kiểm tra ảnh trong Cloudinary Dashboard → Media Library
