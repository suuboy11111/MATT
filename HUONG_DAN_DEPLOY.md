# 🚀 Hướng dẫn Deploy Website Mái Ấm Tình Thương

## 📋 Tổng quan

Dự án sử dụng:
- **.NET 9.0** (ASP.NET Core MVC)
- **SQL Server** database
- **PayOS** payment gateway
- **Identity** authentication

## 🎯 Các lựa chọn Deploy (Miễn phí)

### 1. **Railway** (Khuyến nghị - Dễ nhất) ⭐
- ✅ Free tier: $5 credit/tháng
- ✅ Hỗ trợ .NET tốt
- ✅ Tự động build và deploy từ GitHub
- ✅ Có thể dùng PostgreSQL (free) hoặc SQL Server
- ✅ HTTPS tự động
- ✅ Custom domain miễn phí

### 2. **Render**
- ✅ Free tier: 750 giờ/tháng
- ✅ Hỗ trợ .NET
- ✅ PostgreSQL free
- ✅ HTTPS tự động
- ⚠️ Sleep sau 15 phút không dùng (free tier)

### 3. **Azure App Service**
- ✅ Free tier: F1 (có giới hạn)
- ✅ Hỗ trợ .NET tốt nhất
- ✅ SQL Database (có free tier)
- ⚠️ Cần thẻ tín dụng để đăng ký

### 4. **Fly.io**
- ✅ Free tier: 3 VMs
- ✅ Hỗ trợ .NET
- ✅ PostgreSQL free
- ⚠️ Phức tạp hơn một chút

---

## 🚀 Hướng dẫn Deploy trên Railway (Khuyến nghị)

### Bước 1: Chuẩn bị Database

Railway hỗ trợ PostgreSQL miễn phí. Bạn có 2 lựa chọn:

#### Option A: Dùng PostgreSQL (Miễn phí - Khuyến nghị)
- Cần migrate database từ SQL Server sang PostgreSQL
- Railway cung cấp PostgreSQL free

#### Option B: Dùng SQL Server từ bên ngoài
- Có thể dùng Azure SQL Database (free tier)
- Hoặc SQL Server từ nhà cung cấp khác

**Tôi khuyến nghị Option A (PostgreSQL)** vì đơn giản và miễn phí.

### Bước 2: Cập nhật Code để hỗ trợ PostgreSQL

1. **Thêm package PostgreSQL**:
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

2. **Cập nhật `Program.cs`** để hỗ trợ cả SQL Server và PostgreSQL:
```csharp
// Thay dòng này:
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Thành:
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionString.Contains("PostgreSQL") || connectionString.Contains("postgres"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}
```

### Bước 3: Tạo file cấu hình cho Railway

Tạo file `railway.json`:
```json
{
  "$schema": "https://railway.app/railway.schema.json",
  "build": {
    "builder": "NIXPACKS"
  },
  "deploy": {
    "startCommand": "dotnet MaiAmTinhThuong.dll",
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 10
  }
}
```

Tạo file `Procfile` (cho Railway):
```
web: dotnet MaiAmTinhThuong.dll
```

### Bước 4: Tạo file .railwayignore (tương tự .gitignore)

Tạo file `.railwayignore`:
```
bin/
obj/
.vs/
*.user
*.suo
*.cache
```

### Bước 5: Đẩy code lên GitHub

1. **Khởi tạo Git** (nếu chưa có):
```bash
git init
git add .
git commit -m "Initial commit"
```

2. **Tạo repository trên GitHub**:
   - Vào https://github.com/new
   - Tạo repository mới (ví dụ: `mai-am-tinh-thuong`)

3. **Đẩy code lên GitHub**:
```bash
git remote add origin https://github.com/YOUR_USERNAME/mai-am-tinh-thuong.git
git branch -M main
git push -u origin main
```

### Bước 6: Deploy trên Railway

1. **Đăng ký Railway**:
   - Truy cập: https://railway.app
   - Đăng nhập bằng GitHub

2. **Tạo Project mới**:
   - Click "New Project"
   - Chọn "Deploy from GitHub repo"
   - Chọn repository vừa tạo

3. **Thêm PostgreSQL Database**:
   - Trong project, click "New" → "Database" → "Add PostgreSQL"
   - Railway sẽ tự động tạo database và cung cấp connection string

4. **Cấu hình Environment Variables**:
   - Vào tab "Variables"
   - Thêm các biến sau:
     ```
     ConnectionStrings__DefaultConnection = <PostgreSQL connection string từ Railway>
     PayOS__ClientId = 9ca8c566-b2e8-4497-88fc-a5ad18f477f8
     PayOS__ApiKey = 4209e4e9-a757-4104-ad73-d21d18e9037a
     PayOS__ChecksumKey = 05a4aafcabab2416009875d0b95b999f5faa6827a08562b2fa2972ef3b5b55ab
     PayOS__BaseUrl = https://your-app-name.railway.app
     ASPNETCORE_ENVIRONMENT = Production
     ```

5. **Chạy Migration**:
   - Railway sẽ tự động build và deploy
   - Sau khi deploy xong, vào tab "Deployments" → Click vào deployment mới nhất
   - Mở "Shell" và chạy:
     ```bash
     dotnet ef database update
     ```
   - Hoặc tạo một script để tự động chạy migration khi deploy

6. **Lấy URL**:
   - Railway sẽ tự động tạo URL: `https://your-app-name.railway.app`
   - Có thể thêm custom domain miễn phí

### Bước 7: Cấu hình PayOS Webhook

1. Vào PayOS Dashboard: https://pay.payos.vn/
2. Vào phần **Webhook**
3. Thêm webhook URL: `https://your-app-name.railway.app/Payment/Webhook`
4. Chọn events: `payment.paid`, `payment.cancelled`

### Bước 8: Cập nhật BaseUrl trong Environment Variables

Trong Railway, cập nhật:
```
PayOS__BaseUrl = https://your-app-name.railway.app
```

---

## 🔧 Tạo Script Migration tự động

Tạo file `Program.cs` để tự động chạy migration khi khởi động:

Cập nhật `Program.cs` (thêm trước `app.Run()`):

```csharp
// Tự động chạy migration khi khởi động (chỉ trong Production)
if (!app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database.");
        }
    }
}
```

---

## 🎯 Hướng dẫn Deploy trên Render (Lựa chọn 2)

### Bước 1: Đăng ký Render
- Truy cập: https://render.com
- Đăng nhập bằng GitHub

### Bước 2: Tạo Web Service
1. Click "New" → "Web Service"
2. Connect GitHub repository
3. Cấu hình:
   - **Name**: mai-am-tinh-thuong
   - **Environment**: .NET
   - **Build Command**: `dotnet publish -c Release -o ./publish`
   - **Start Command**: `dotnet ./publish/MaiAmTinhThuong.dll`

### Bước 3: Thêm PostgreSQL Database
1. Click "New" → "PostgreSQL"
2. Render sẽ cung cấp connection string

### Bước 4: Cấu hình Environment Variables
Thêm các biến như Railway (xem trên)

### Bước 5: Deploy
Render sẽ tự động build và deploy

---

## 🎯 Hướng dẫn Deploy trên Azure App Service

### Bước 1: Đăng ký Azure
- Truy cập: https://azure.microsoft.com/free/
- Đăng ký tài khoản (cần thẻ tín dụng, nhưng free tier không tính phí)

### Bước 2: Tạo App Service
1. Vào Azure Portal
2. Tạo "App Service" mới
3. Chọn:
   - Runtime stack: .NET 9
   - Operating System: Linux (rẻ hơn) hoặc Windows

### Bước 3: Tạo SQL Database
1. Tạo "SQL Database" (có free tier)
2. Lấy connection string

### Bước 4: Deploy từ GitHub
1. Vào App Service → Deployment Center
2. Chọn GitHub và repository
3. Azure sẽ tự động deploy

### Bước 5: Cấu hình Connection String
1. Vào App Service → Configuration
2. Thêm connection string và app settings

---

## ✅ Checklist trước khi Deploy

- [ ] Code đã được commit và push lên GitHub
- [ ] Database connection string đã được cấu hình
- [ ] PayOS keys đã được thêm vào environment variables
- [ ] PayOS BaseUrl đã được cấu hình (URL của website sau khi deploy)
- [ ] Migration đã được chạy hoặc có script tự động
- [ ] Webhook PayOS đã được cấu hình với URL mới
- [ ] Đã test local trước khi deploy

---

## 🐛 Xử lý lỗi thường gặp

### Lỗi: "Database connection failed"
- ✅ Kiểm tra connection string trong environment variables
- ✅ Đảm bảo database đã được tạo
- ✅ Kiểm tra firewall rules của database

### Lỗi: "Migration failed"
- ✅ Chạy migration thủ công trong shell
- ✅ Kiểm tra xem có migration nào chưa được apply

### Lỗi: "PayOS webhook không hoạt động"
- ✅ Kiểm tra PayOS BaseUrl đã đúng chưa
- ✅ Kiểm tra webhook URL trong PayOS dashboard
- ✅ Xem logs trong Railway/Render để debug

### Lỗi: "Static files không load"
- ✅ Kiểm tra `wwwroot` folder đã được include trong build
- ✅ Kiểm tra đường dẫn trong code

---

## 📝 Lưu ý quan trọng

1. **Database**: Nếu dùng PostgreSQL, cần migrate từ SQL Server. Có thể dùng tool như `pgloader` hoặc export/import thủ công.

2. **Environment Variables**: Không commit các key nhạy cảm vào Git. Dùng environment variables.

3. **HTTPS**: Railway và Render tự động cung cấp HTTPS. Azure cũng có HTTPS mặc định.

4. **Custom Domain**: Có thể thêm custom domain miễn phí trên Railway và Render.

5. **Backup**: Nên backup database thường xuyên.

---

## 🎉 Sau khi Deploy thành công

1. ✅ Test website: Truy cập URL được cung cấp
2. ✅ Test thanh toán: Thử tạo payment link
3. ✅ Test webhook: Kiểm tra PayOS có gửi webhook không
4. ✅ Test database: Kiểm tra data có được lưu không

**Chúc bạn deploy thành công! 🚀**

