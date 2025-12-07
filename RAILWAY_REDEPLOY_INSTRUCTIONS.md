# 🔄 Hướng Dẫn Redeploy sau khi thêm Environment Variables

## Vấn đề

Sau khi thêm environment variables vào Railway, service có thể chưa nhận được variables ngay lập tức. Cần **redeploy** để variables được apply.

## Giải pháp

### Bước 1: Kiểm tra Variables đã được thêm chưa

1. Vào Railway Dashboard → Project → Service
2. Click tab **Variables**
3. Đảm bảo có 3 biến:
   - ✅ `CLOUDINARY_CLOUD_NAME` = `dwoxexbvw`
   - ✅ `CLOUDINARY_API_KEY` = `976117337767364`
   - ✅ `CLOUDINARY_API_SECRET` = `5HNDkqmYeCG3xXRecQDL9bOuQzU`

### Bước 2: Redeploy Service

Có 2 cách:

#### Cách 1: Manual Redeploy (Khuyến nghị)
1. Trong Railway Dashboard → Service
2. Click vào **Settings** (hoặc menu 3 chấm)
3. Click **Redeploy** hoặc **Restart**
4. Đợi deployment hoàn thành (thường 1-3 phút)

#### Cách 2: Trigger bằng Git Push
1. Tạo một commit nhỏ (ví dụ: thêm comment)
2. Push lên GitHub
3. Railway sẽ tự động detect và redeploy

### Bước 3: Kiểm tra Logs

Sau khi redeploy, kiểm tra logs trong Railway. Bạn sẽ thấy:

**✅ Thành công:**
```
info: MaiAmTinhThuong.Services.ImageUploadService[0]
      🔍 Checking Cloudinary configuration...
info: MaiAmTinhThuong.Services.ImageUploadService[0]
      CloudName - Config: False, Env: True, Final: True
info: MaiAmTinhThuong.Services.ImageUploadService[0]
      API Key - Config: False, Env: True, Final: True
info: MaiAmTinhThuong.Services.ImageUploadService[0]
      API Secret - Config: False, Env: True, Final: True
info: MaiAmTinhThuong.Services.ImageUploadService[0]
      Found Cloudinary-related env vars: CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY, CLOUDINARY_API_SECRET
info: MaiAmTinhThuong.Services.ImageUploadService[0]
      Cloudinary initialized successfully with CloudName: dwoxexbvw
```

**❌ Vẫn lỗi:**
```
warn: MaiAmTinhThuong.Services.ImageUploadService[0]
      ⚠️ No CLOUDINARY environment variables found!
```

Nếu vẫn lỗi, xem phần Troubleshooting bên dưới.

### Bước 4: Test Upload

Sau khi thấy logs thành công:
1. Truy cập `/Account/Profile`
2. Upload ảnh mới
3. Kiểm tra logs sẽ thấy:
```
info: Image uploaded successfully to Cloudinary: https://res.cloudinary.com/dwoxexbvw/...
```

## Troubleshooting

### Vẫn bị lỗi sau khi redeploy

1. **Kiểm tra tên biến có đúng không:**
   - ❌ `CLOUDINARY_CLOUDNAME` (sai)
   - ✅ `CLOUDINARY_CLOUD_NAME` (đúng)
   
   - ❌ `CLOUDINARY_APIKEY` (sai)
   - ✅ `CLOUDINARY_API_KEY` (đúng)
   
   - ❌ `CLOUDINARY_APISECRET` (sai)
   - ✅ `CLOUDINARY_API_SECRET` (đúng)

2. **Kiểm tra giá trị:**
   - Không có khoảng trắng ở đầu/cuối
   - Không có dấu ngoặc kép thừa
   - Copy-paste chính xác

3. **Kiểm tra Service đúng chưa:**
   - Đảm bảo bạn đang thêm variables vào **Service chạy ứng dụng**, không phải Database service
   - Nếu có nhiều services, kiểm tra service nào đang chạy web app

4. **Xóa và thêm lại:**
   - Xóa 3 biến cũ
   - Thêm lại từ đầu
   - Redeploy

5. **Kiểm tra logs chi tiết:**
   - Xem logs có hiển thị "Found Cloudinary-related env vars" không
   - Nếu không có, variables chưa được load

### Variables có trong Railway nhưng logs vẫn báo "No CLOUDINARY environment variables found!"

- **Nguyên nhân:** Service chưa được redeploy sau khi thêm variables
- **Giải pháp:** Redeploy service (xem Bước 2)

### Lỗi: "CloudName - Env: False"

- Biến `CLOUDINARY_CLOUD_NAME` chưa được set hoặc tên sai
- Kiểm tra lại trong Railway Variables tab
- Redeploy sau khi sửa

## Lưu ý

⚠️ **Quan trọng:**
- Sau khi thêm/sửa environment variables, **PHẢI** redeploy service
- Railway không tự động apply variables cho container đang chạy
- Cần restart container để variables mới có hiệu lực

✅ **Sau khi fix:**
- Service sẽ khởi tạo thành công
- Upload ảnh sẽ lưu lên Cloudinary
- URL trong database sẽ là Cloudinary URL (bắt đầu với `https://res.cloudinary.com/...`)
- Ảnh sẽ không bị mất khi redeploy
