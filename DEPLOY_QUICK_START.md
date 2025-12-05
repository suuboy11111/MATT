# 🚀 Hướng dẫn Deploy Nhanh - Railway

## ⚡ Các bước nhanh nhất để deploy

### Bước 1: Thêm package PostgreSQL (Nếu chưa có)
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

### Bước 2: Đẩy code lên GitHub

1. **Khởi tạo Git** (nếu chưa có):
```bash
git init
git add .
git commit -m "Prepare for deployment"
```

2. **Tạo repository trên GitHub**:
   - Vào https://github.com/new
   - Tạo repo mới (ví dụ: `mai-am-tinh-thuong`)

3. **Đẩy code**:
```bash
git remote add origin https://github.com/YOUR_USERNAME/mai-am-tinh-thuong.git
git branch -M main
git push -u origin main
```

### Bước 3: Deploy trên Railway

1. **Đăng ký Railway**: https://railway.app (đăng nhập bằng GitHub)

2. **Tạo Project**:
   - Click "New Project"
   - Chọn "Deploy from GitHub repo"
   - Chọn repository vừa tạo

3. **Thêm PostgreSQL Database**:
   - Trong project, click "New" → "Database" → "Add PostgreSQL"
   - Railway sẽ tự động tạo và cung cấp connection string

4. **Cấu hình Environment Variables**:
   - Vào tab "Variables" của service
   - Thêm các biến sau (lấy connection string từ PostgreSQL service):
   
   ```
   ConnectionStrings__DefaultConnection = <PostgreSQL connection string từ Railway>
   PayOS__ClientId = 9ca8c566-b2e8-4497-88fc-a5ad18f477f8
   PayOS__ApiKey = 4209e4e9-a757-4104-ad73-d21d18e9037a
   PayOS__ChecksumKey = 05a4aafcabab2416009875d0b95b999f5faa6827a08562b2fa2972ef3b5b55ab
   ASPNETCORE_ENVIRONMENT = Production
   ```

5. **Lấy URL**:
   - Railway sẽ tự động tạo URL: `https://your-app-name.railway.app`
   - Copy URL này

6. **Cập nhật PayOS BaseUrl**:
   - Thêm vào Variables:
   ```
   PayOS__BaseUrl = https://your-app-name.railway.app
   ```

### Bước 4: Chạy Migration

Sau khi deploy xong, migration sẽ tự động chạy (đã được cấu hình trong `Program.cs`).

Nếu cần chạy thủ công:
- Vào tab "Deployments" → Click deployment mới nhất → "Shell"
- Chạy: `dotnet ef database update`

### Bước 5: Cấu hình PayOS Webhook

1. Vào PayOS Dashboard: https://pay.payos.vn/
2. Vào phần **Webhook**
3. Thêm webhook URL: `https://your-app-name.railway.app/Payment/Webhook`
4. Chọn events: `payment.paid`, `payment.cancelled`

### Bước 6: Test

1. Truy cập: `https://your-app-name.railway.app`
2. Test thanh toán: `/Payment/Donate`
3. Kiểm tra database có lưu transaction không

## ✅ Hoàn thành!

Website của bạn đã được deploy và có thể truy cập công khai!

## 🔧 Lưu ý

- **Database**: Railway dùng PostgreSQL, code đã được cập nhật để tự động phát hiện và dùng PostgreSQL
- **Migration**: Tự động chạy khi khởi động (đã cấu hình trong `Program.cs`)
- **HTTPS**: Railway tự động cung cấp HTTPS
- **Custom Domain**: Có thể thêm custom domain miễn phí trong Railway settings

## 🐛 Xử lý lỗi

### Lỗi: "Package Npgsql not found"
```bash
dotnet restore
```

### Lỗi: "Migration failed"
- Kiểm tra connection string đã đúng chưa
- Xem logs trong Railway để biết lỗi chi tiết

### Lỗi: "PayOS webhook không hoạt động"
- Kiểm tra PayOS BaseUrl đã đúng chưa
- Kiểm tra webhook URL trong PayOS dashboard

---

**Chúc bạn deploy thành công! 🎉**

