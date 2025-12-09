# 🚀 Hướng dẫn Deploy lên Railway

## 📋 Checklist trước khi deploy

- [x] Đã có tài khoản Railway
- [x] Repository đã được push lên GitHub: `suuboy11111/MATT`
- [x] Code đã được cấu hình cho Railway

## 🔧 Các bước deploy

### Bước 1: Tạo Project trên Railway

1. Truy cập [Railway Dashboard](https://railway.app/dashboard)
2. Click **"New Project"**
3. Chọn **"Deploy from GitHub repo"**
4. Chọn repository: **`suuboy11111/MATT`**
5. Railway sẽ tự động detect project và bắt đầu build

### Bước 2: Thêm PostgreSQL Database

1. Trong project vừa tạo, click **"+ New"**
2. Chọn **"Database"** → **"Add PostgreSQL"**
3. Railway sẽ tự động tạo PostgreSQL database
4. **Lưu ý**: Railway tự động tạo biến `DATABASE_URL`, code đã được cấu hình để sử dụng biến này

### Bước 3: Cấu hình Environment Variables

Trong service **web** của bạn, vào tab **"Variables"** và thêm:

#### Bắt buộc:
```
ASPNETCORE_ENVIRONMENT=Production
```

#### PayOS (nếu cần):
```
PayOS__ClientId=9ca8c566-b2e8-4497-88fc-a5ad18f477f8
PayOS__ApiKey=4209e4e9-a757-4104-ad73-d21d18e9037a
PayOS__ChecksumKey=05a4aafcabab2416009875d0b95b999f5faa6827a08562b2fa2972ef3b5b55ab
```

**Lưu ý**: `DATABASE_URL` sẽ được Railway tự động thêm khi bạn tạo PostgreSQL service.

### Bước 4: Kết nối Database với Web Service

1. Trong PostgreSQL service, vào tab **"Settings"**
2. Tìm phần **"Connect"** hoặc **"Variables"**
3. Copy biến `DATABASE_URL`
4. Trong Web service, vào **"Variables"** → **"Add Variable"**
5. Thêm: `DATABASE_URL` = (giá trị từ PostgreSQL service)

**Hoặc** Railway có thể tự động link nếu bạn:
- Click vào Web service
- Vào tab **"Settings"**
- Tìm **"Connect"** hoặc **"Generate Domain"**
- Railway sẽ tự động link database

### Bước 5: Deploy

Railway sẽ tự động:
1. Detect .NET project
2. Build project
3. Deploy application
4. Chạy migrations tự động (code đã được cấu hình)

### Bước 6: Kiểm tra và Lấy URL

1. Sau khi deploy thành công, vào Web service
2. Click tab **"Settings"**
3. Tìm **"Generate Domain"** để tạo public URL
4. Hoặc Railway sẽ tự động tạo URL

## 🔍 Kiểm tra Logs

Nếu có lỗi, kiểm tra logs:
1. Vào Web service
2. Click tab **"Deployments"**
3. Click vào deployment mới nhất
4. Xem logs để debug

## ⚙️ Cấu hình quan trọng

### Port Configuration
- Railway tự động set biến `PORT`
- Code đã được cấu hình để sử dụng biến này
- Không cần cấu hình thủ công

### Database Migration
- Code tự động chạy migration khi khởi động
- Hỗ trợ cả PostgreSQL và SQL Server
- Tự động detect database type từ connection string

### Static Files
- Đảm bảo folder `wwwroot` được commit vào git
- Tất cả images, CSS, JS sẽ được serve tự động

## 🐛 Troubleshooting

### Build failed
- Kiểm tra logs trong Railway dashboard
- Đảm bảo `.csproj` file đúng
- Kiểm tra .NET version (project dùng .NET 9.0)

### Database connection error
- Kiểm tra `DATABASE_URL` đã được set chưa
- Kiểm tra PostgreSQL service đã được tạo chưa
- Xem logs để biết lỗi cụ thể

### Static files không load
- Đảm bảo `wwwroot` folder được commit
- Kiểm tra đường dẫn trong code

### Port already in use
- Railway tự động quản lý PORT
- Không cần cấu hình thủ công

## 📝 Lưu ý bảo mật

- **KHÔNG** commit file `appsettings.json` có thông tin nhạy cảm
- Sử dụng Environment Variables cho các thông tin nhạy cảm
- PayOS keys nên được set qua Environment Variables

## ✅ Sau khi deploy thành công

1. Truy cập URL được Railway cung cấp
2. Kiểm tra website hoạt động
3. Test các chức năng chính
4. Kiểm tra database connection
5. Test upload images (nếu có)

---

**Chúc bạn deploy thành công! 🎉**







