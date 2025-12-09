# 🔄 Ví Dụ Migration Code Upload Ảnh

## Ví dụ 1: AccountController - UpdateProfile

### Code Cũ (Local Storage):
```csharp
if (profilePicture != null && profilePicture.Length > 0)
{
    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
    var fileExtension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
    if (!allowedExtensions.Contains(fileExtension))
    {
        ModelState.AddModelError("profilePicture", "Chỉ chấp nhận file ảnh");
    }
    else if (profilePicture.Length > 5 * 1024 * 1024)
    {
        ModelState.AddModelError("profilePicture", "Kích thước file không được vượt quá 5MB");
    }
    else
    {
        try
        {
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
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("profilePicture", $"Lỗi khi lưu file: {ex.Message}");
        }
    }
}
```

### Code Mới (Cloudinary):
```csharp
// 1. Thêm vào constructor
private readonly IImageUploadService _imageUploadService;

public AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext context,
    IImageUploadService imageUploadService) // Thêm parameter này
{
    _userManager = userManager;
    _signInManager = signInManager;
    _context = context;
    _imageUploadService = imageUploadService; // Thêm dòng này
}

// 2. Thay thế code upload
if (profilePicture != null && profilePicture.Length > 0)
{
    try
    {
        // Xóa ảnh cũ nếu có (optional)
        if (!string.IsNullOrEmpty(user.ProfilePicture) && 
            user.ProfilePicture.StartsWith("https://res.cloudinary.com"))
        {
            await _imageUploadService.DeleteImageAsync(user.ProfilePicture);
        }

        // Upload ảnh mới lên Cloudinary
        var imageUrl = await _imageUploadService.UploadImageAsync(profilePicture, "profiles");
        user.ProfilePicture = imageUrl;
    }
    catch (ArgumentException ex)
    {
        // Lỗi validation (file type, size)
        ModelState.AddModelError("profilePicture", ex.Message);
    }
    catch (Exception ex)
    {
        // Lỗi khác (network, Cloudinary API)
        ModelState.AddModelError("profilePicture", $"Lỗi khi upload ảnh: {ex.Message}");
    }
}
```

## Ví dụ 2: BlogController - CreatePost

### Code Cũ:
```csharp
if (image != null && image.Length > 0)
{
    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
    var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
    if (!allowedExtensions.Contains(fileExtension))
    {
        ModelState.AddModelError("image", "Chỉ chấp nhận file ảnh");
    }
    else if (image.Length > 5 * 1024 * 1024)
    {
        ModelState.AddModelError("image", "Kích thước file không được vượt quá 5MB");
    }
    else
    {
        try
        {
            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }
            var imagePath = Path.Combine(uploadDir, uniqueFileName);
            using (var stream = new FileStream(imagePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }
            blogPost.ImageUrl = "/images/" + uniqueFileName;
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("image", $"Lỗi khi lưu file: {ex.Message}");
        }
    }
}
```

### Code Mới:
```csharp
// 1. Thêm vào constructor
private readonly IImageUploadService _imageUploadService;

public BlogController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IImageUploadService imageUploadService) // Thêm parameter
{
    _context = context;
    _userManager = userManager;
    _imageUploadService = imageUploadService; // Thêm dòng này
}

// 2. Thay thế code upload
if (image != null && image.Length > 0)
{
    try
    {
        var imageUrl = await _imageUploadService.UploadImageAsync(image, "blog");
        blogPost.ImageUrl = imageUrl;
    }
    catch (ArgumentException ex)
    {
        ModelState.AddModelError("image", ex.Message);
    }
    catch (Exception ex)
    {
        ModelState.AddModelError("image", $"Lỗi khi upload ảnh: {ex.Message}");
    }
}
```

## Ví dụ 3: SupportRequestController - CreateRequest

### Code Cũ:
```csharp
if (ImageFile != null && ImageFile.Length > 0)
{
    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
    var fileExtension = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();
    if (!allowedExtensions.Contains(fileExtension))
    {
        ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh");
    }
    else if (ImageFile.Length > 5 * 1024 * 1024)
    {
        ModelState.AddModelError("ImageFile", "Kích thước file không được vượt quá 5MB");
    }
    else
    {
        try
        {
            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }
            var filePath = Path.Combine(uploadDir, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await ImageFile.CopyToAsync(stream);
            }
            model.ImageUrl = "/images/profiles/" + uniqueFileName;
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("ImageFile", $"Lỗi khi lưu file: {ex.Message}");
        }
    }
}
```

### Code Mới:
```csharp
// 1. Thêm vào constructor
private readonly IImageUploadService _imageUploadService;

public SupportRequestController(
    ApplicationDbContext context,
    SupportRequestService supportRequestService,
    IImageUploadService imageUploadService) // Thêm parameter
{
    _context = context;
    _supportRequestService = supportRequestService;
    _imageUploadService = imageUploadService; // Thêm dòng này
}

// 2. Thay thế code upload
if (ImageFile != null && ImageFile.Length > 0)
{
    try
    {
        var imageUrl = await _imageUploadService.UploadImageAsync(ImageFile, "support-requests");
        model.ImageUrl = imageUrl;
    }
    catch (ArgumentException ex)
    {
        ModelState.AddModelError("ImageFile", ex.Message);
    }
    catch (Exception ex)
    {
        ModelState.AddModelError("ImageFile", $"Lỗi khi upload ảnh: {ex.Message}");
    }
}
```

## Checklist Migration

- [ ] Cài package `CloudinaryDotNet`
- [ ] Thêm config vào `appsettings.json`
- [ ] Tạo `ImageUploadService.cs`
- [ ] Đăng ký service trong `Program.cs`
- [ ] Cập nhật `AccountController`
- [ ] Cập nhật `BlogController`
- [ ] Cập nhật `SupportRequestController`
- [ ] Cập nhật `SupporterController`
- [ ] Cập nhật `MaiAmAdminController`
- [ ] Thêm Railway environment variables
- [ ] Test upload ảnh mới
- [ ] Test xóa ảnh cũ (nếu implement)

## Lưu ý

1. **Backward Compatibility**: Ảnh cũ vẫn dùng local path (`/images/...`) sẽ vẫn hoạt động nếu file còn tồn tại. Chỉ ảnh mới upload sẽ dùng Cloudinary.

2. **Fallback Strategy**: Có thể implement fallback để nếu Cloudinary fail thì vẫn lưu local:
```csharp
try
{
    var imageUrl = await _imageUploadService.UploadImageAsync(file, "folder");
    return imageUrl;
}
catch
{
    // Fallback to local storage
    // ... local storage code ...
}
```

3. **Migration Ảnh Cũ**: Nếu muốn migrate ảnh cũ lên Cloudinary, có thể tạo script riêng để:
   - Đọc tất cả ảnh từ database
   - Upload lên Cloudinary
   - Update URL trong database


