# Hướng dẫn chuyển sang PostgreSQL - Tạo lại Migrations

## ⚠️ QUAN TRỌNG: Đóng tất cả file migrations trong IDE trước khi xóa!

## Bước 1: Xóa migrations cũ

**Cách 1: Dùng script (Khuyến nghị)**
```powershell
cd "d:\Study\MaiAmTinhThuong\MaiAmTinhThuong\MaiAmTinhThuong"
.\delete-migrations.ps1
```

**Cách 2: Xóa thủ công**
1. Đóng tất cả file trong folder `Migrations` trong IDE
2. Xóa tất cả file `.cs`, `.Designer.cs`, `.sql` trong folder `Migrations`
3. Giữ lại folder `Migrations` (chỉ xóa files bên trong)

---

## Bước 2: Reset Database trên Railway

**Quan trọng:** Bạn cần reset database trên Railway trước khi tạo migration mới.

1. Vào Railway Dashboard: https://railway.app
2. Chọn project của bạn
3. Vào **PostgreSQL service**
4. Vào tab **Settings**
5. Tìm nút **Delete** hoặc **Reset Database**
6. Xác nhận xóa

**HOẶC** nếu không có nút Delete:
- Tạo PostgreSQL service mới
- Xóa service cũ
- Cập nhật `DATABASE_URL` trong Web service variables để trỏ đến service mới

---

## Bước 3: Tạo Migration mới với PostgreSQL

Sau khi reset database, chạy lệnh sau:

```powershell
cd "d:\Study\MaiAmTinhThuong\MaiAmTinhThuong\MaiAmTinhThuong"

# Tạo migration mới (EF Core sẽ tự động detect PostgreSQL từ UseNpgsql())
dotnet ef migrations add InitialCreatePostgreSQL --context ApplicationDbContext
```

**Lưu ý:** 
- Migration sẽ được tạo với PostgreSQL syntax tự động
- EF Core sẽ detect PostgreSQL provider từ `UseNpgsql()` trong `Program.cs`

---

## Bước 4: Kiểm tra Migration

Sau khi tạo, kiểm tra file migration:
- Mở file `Migrations/YYYYMMDDHHMMSS_InitialCreatePostgreSQL.cs`
- Kiểm tra xem có dùng:
  - ✅ `varchar` hoặc `text` (thay vì `nvarchar`)
  - ✅ `timestamp without time zone` (thay vì `datetime2`)
  - ✅ `boolean` (thay vì `bit`)
  - ✅ `SERIAL` hoặc `NpgsqlValueGenerationStrategy.SerialColumn` (thay vì `SqlServer:Identity`)

---

## Bước 5: Commit và Deploy

```bash
git add .
git commit -m "Migrate to PostgreSQL: Remove SQL Server migrations, create new PostgreSQL migrations"
git push origin main
```

Railway sẽ tự động:
1. Build project
2. Chạy migrations mới
3. Tạo database schema với PostgreSQL syntax

---

## Troubleshooting

### Nếu migration vẫn có SQL Server syntax:

1. Kiểm tra `Program.cs` - đảm bảo `UseNpgsql()` được gọi (không phải `UseSqlServer()`)
2. Kiểm tra `DATABASE_URL` - đảm bảo format PostgreSQL
3. Xóa `bin/` và `obj/` folders, rebuild:
   ```powershell
   Remove-Item -Recurse -Force bin, obj
   dotnet clean
   dotnet build
   ```

### Nếu lỗi "Database already exists":

- Reset database trên Railway (xem bước 2)
- Hoặc xóa bảng `__EFMigrationsHistory` trong database:
  ```sql
  DROP TABLE IF EXISTS "__EFMigrationsHistory";
  ```

### Nếu không xóa được file migrations:

- Đóng tất cả file migrations trong IDE (VS Code, Visual Studio)
- Đóng tất cả tab liên quan đến migrations
- Thử lại script hoặc xóa thủ công

---

## Sau khi hoàn thành

Website sẽ hoạt động với PostgreSQL! 🎉




