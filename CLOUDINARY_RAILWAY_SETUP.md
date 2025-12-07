# 🚂 Cấu Hình Cloudinary trên Railway

## Thông tin Cloudinary của bạn

- **Cloud Name**: `dwoxexbvw`
- **API Key**: `976117337767364`
- **API Secret**: `5HNDkqmYeCG3xXRecQDL9bOuQzU`

## Bước 1: Thêm Environment Variables vào Railway

1. Vào **Railway Dashboard** → Chọn project → Chọn service
2. Click vào tab **Variables**
3. Thêm 3 biến môi trường sau:

| Variable Name | Value |
|--------------|-------|
| `CLOUDINARY_CLOUD_NAME` | `dwoxexbvw` |
| `CLOUDINARY_API_KEY` | `976117337767364` |
| `CLOUDINARY_API_SECRET` | `5HNDkqmYeCG3xXRecQDL9bOuQzU` |

4. Click **Deploy** để apply changes

## Bước 2: Kiểm tra cấu hình

Sau khi deploy, kiểm tra logs để đảm bảo không có lỗi:
```
Image uploaded successfully to Cloudinary: https://res.cloudinary.com/dwoxexbvw/...
```

## Bước 3: Test Upload

1. Upload một ảnh mới qua form
2. Kiểm tra URL trong database - phải là Cloudinary URL
3. Kiểm tra ảnh hiển thị trên website
4. Kiểm tra trong Cloudinary Dashboard → Media Library

## Lưu ý Bảo Mật

✅ **Đã làm:**
- Secrets được lưu trong `appsettings.Development.json` (đã có trong .gitignore)
- `appsettings.json` chỉ chứa placeholder (an toàn để commit)

⚠️ **Quan trọng:**
- **KHÔNG** commit `appsettings.Development.json` vào git
- **KHÔNG** share API Secret với ai
- Nếu API Secret bị lộ, hãy tạo API Key mới trong Cloudinary Dashboard

## Troubleshooting

### Lỗi: "Cloudinary configuration is missing!"
- Kiểm tra Railway environment variables đã được set chưa
- Đảm bảo tên biến đúng: `CLOUDINARY_CLOUD_NAME`, `CLOUDINARY_API_KEY`, `CLOUDINARY_API_SECRET`
- Redeploy service sau khi thêm variables

### Ảnh không upload được
- Kiểm tra logs trong Railway để xem lỗi chi tiết
- Kiểm tra API Key và Secret có đúng không
- Kiểm tra Cloudinary Dashboard → Settings → Security → Allowed domains

### Ảnh upload nhưng không hiển thị
- Kiểm tra URL trong database có đúng format Cloudinary không
- Kiểm tra CORS settings trong Cloudinary
- Kiểm tra browser console có lỗi không
