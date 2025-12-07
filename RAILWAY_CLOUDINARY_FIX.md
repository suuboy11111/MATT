# 🔧 Sửa Lỗi Cloudinary trên Railway

## Lỗi hiện tại

```
System.Exception: Cloudinary configuration is missing! Please check appsettings.json or environment variables.
```

## Nguyên nhân

Railway chưa có environment variables cho Cloudinary. Service `ImageUploadService` cần 3 biến môi trường để hoạt động.

## Giải pháp

### Bước 1: Thêm Environment Variables vào Railway

1. Vào **Railway Dashboard**: https://railway.app
2. Chọn **Project** của bạn
3. Chọn **Service** (service chạy ứng dụng)
4. Click vào tab **Variables** (hoặc **Environment**)
5. Thêm 3 biến môi trường sau:

| Variable Name | Value |
|--------------|-------|
| `CLOUDINARY_CLOUD_NAME` | `dwoxexbvw` |
| `CLOUDINARY_API_KEY` | `976117337767364` |
| `CLOUDINARY_API_SECRET` | `5HNDkqmYeCG3xXRecQDL9bOuQzU` |

### Bước 2: Redeploy

Sau khi thêm variables:
1. Railway sẽ tự động detect changes và trigger redeploy
2. Hoặc click **Deploy** để force redeploy
3. Đợi deployment hoàn thành

### Bước 3: Kiểm tra Logs

Sau khi deploy, kiểm tra logs trong Railway. Bạn sẽ thấy:

```
info: MaiAmTinhThuong.Services.ImageUploadService[0]
      Checking Cloudinary configuration...
info: MaiAmTinhThuong.Services.ImageUploadService[0]
      CloudName from env: True
info: MaiAmTinhThuong.Services.ImageUploadService[0]
      API Key from env: True
info: MaiAmTinhThuong.Services.ImageUploadService[0]
      API Secret from env: True
info: MaiAmTinhThuong.Services.ImageUploadService[0]
      Cloudinary initialized successfully with CloudName: dwoxexbvw
```

### Bước 4: Test Upload

1. Truy cập `/Account/Profile`
2. Upload ảnh mới
3. Kiểm tra logs sẽ thấy:
```
info: MaiAmTinhThuong.Services.ImageUploadService[0]
      Image uploaded successfully to Cloudinary: https://res.cloudinary.com/dwoxexbvw/...
```

## Cách thêm Variables trong Railway

### Option 1: Thêm từng biến một
1. Click **+ New Variable**
2. Nhập tên: `CLOUDINARY_CLOUD_NAME`
3. Nhập giá trị: `dwoxexbvw`
4. Click **Add**
5. Lặp lại cho 2 biến còn lại

### Option 2: Import từ file (nếu Railway hỗ trợ)
Tạo file `.env`:
```
CLOUDINARY_CLOUD_NAME=dwoxexbvw
CLOUDINARY_API_KEY=976117337767364
CLOUDINARY_API_SECRET=5HNDkqmYeCG3xXRecQDL9bOuQzU
```

## Troubleshooting

### Vẫn bị lỗi sau khi thêm variables
1. **Kiểm tra tên biến**: Phải chính xác:
   - `CLOUDINARY_CLOUD_NAME` (không phải `CLOUDINARY_CLOUDNAME`)
   - `CLOUDINARY_API_KEY` (không phải `CLOUDINARY_APIKEY`)
   - `CLOUDINARY_API_SECRET` (không phải `CLOUDINARY_APISECRET`)

2. **Kiểm tra giá trị**: Không có khoảng trắng thừa ở đầu/cuối

3. **Redeploy**: Sau khi thêm variables, phải redeploy service

4. **Kiểm tra logs**: Xem logs để biết biến nào bị thiếu

### Lỗi: "CloudName from env: False"
- Biến `CLOUDINARY_CLOUD_NAME` chưa được set hoặc tên sai
- Kiểm tra lại trong Railway Variables tab

### Lỗi: "API Key from env: False"
- Biến `CLOUDINARY_API_KEY` chưa được set hoặc tên sai
- Kiểm tra lại trong Railway Variables tab

### Lỗi: "API Secret from env: False"
- Biến `CLOUDINARY_API_SECRET` chưa được set hoặc tên sai
- Kiểm tra lại trong Railway Variables tab

## Sau khi fix

Sau khi thêm variables và redeploy thành công:
- ✅ Service sẽ khởi tạo thành công
- ✅ Upload ảnh sẽ lưu lên Cloudinary
- ✅ URL trong database sẽ là Cloudinary URL
- ✅ Ảnh sẽ không bị mất khi redeploy

## Lưu ý

⚠️ **Quan trọng**: 
- API Secret là thông tin nhạy cảm, không share với ai
- Nếu API Secret bị lộ, tạo API Key mới trong Cloudinary Dashboard
- Variables trong Railway được mã hóa và an toàn
