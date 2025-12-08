# ⚠️ Vấn đề Lưu Trữ Ảnh trên Railway

## Vấn đề

**Railway filesystem là ephemeral (tạm thời)** - tất cả file upload vào `wwwroot/images/` sẽ **bị xóa** khi:
- Container restart
- Redeploy application
- Railway scale up/down
- Railway maintenance

Điều này dẫn đến **tất cả ảnh upload lên sẽ bị lỗi sau một thời gian**.

## Giải pháp

### Option 1: Sử dụng Cloud Storage (Khuyến nghị)

#### A. AWS S3 (hoặc DigitalOcean Spaces, Cloudflare R2)
1. Tạo S3 bucket
2. Cấu hình IAM user với quyền upload
3. Sử dụng `AWSSDK.S3` package
4. Upload ảnh lên S3 thay vì local filesystem
5. Lưu URL S3 vào database

#### B. Cloudinary (Dễ nhất)
1. Đăng ký tài khoản Cloudinary (free tier: 25GB storage, 25GB bandwidth)
2. Cài package: `CloudinaryDotNet`
3. Upload ảnh lên Cloudinary
4. Lưu Cloudinary URL vào database

### Option 2: Railway Volume (Tạm thời)

Railway hỗ trợ Persistent Volumes, nhưng cần cấu hình:
1. Tạo Volume trong Railway
2. Mount volume vào container
3. Lưu ảnh vào volume path

**Lưu ý**: Volume có giới hạn và tốn phí.

### Option 3: External File Server

Sử dụng file server riêng (VPS, NAS) để lưu ảnh.

## Cách xử lý hiện tại

Hiện tại code đã có:
- ✅ Fallback ảnh mặc định khi ảnh lỗi (`onerror` handler)
- ✅ Validation file type và size
- ✅ Unique filename để tránh conflict

**Nhưng vẫn cần migrate sang cloud storage để giải quyết triệt để.**

## Hướng dẫn migrate sang Cloudinary (Khuyến nghị)

### Bước 1: Cài package
```bash
dotnet add package CloudinaryDotNet
```

### Bước 2: Thêm vào appsettings.json
```json
{
  "Cloudinary": {
    "CloudName": "your-cloud-name",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret"
  }
}
```

### Bước 3: Tạo ImageUploadService
```csharp
public class ImageUploadService
{
    private readonly Cloudinary _cloudinary;
    
    public async Task<string> UploadImageAsync(IFormFile file)
    {
        // Upload to Cloudinary
        // Return public URL
    }
}
```

### Bước 4: Thay thế tất cả upload logic
Thay `Directory.GetCurrentDirectory() + "wwwroot/images"` bằng `ImageUploadService.UploadImageAsync()`

## Tạm thời: Cải thiện Fallback

Đã thêm:
- ✅ `onerror` handler cho tất cả `<img>` tags
- ✅ Fallback về ảnh mặc định
- ✅ Hiển thị placeholder khi ảnh lỗi

**Nhưng đây chỉ là giải pháp tạm thời. Cần migrate sang cloud storage sớm nhất có thể.**

