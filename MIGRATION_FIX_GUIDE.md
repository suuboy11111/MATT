# Hướng dẫn sửa lỗi Migration: SQL Server → PostgreSQL

## Vấn đề
Tất cả migrations đã được tạo cho SQL Server (`nvarchar`, `datetime2`, `bit`, `SqlServer:Identity`) nhưng Railway đang dùng PostgreSQL.

## Giải pháp

### Option 1: Xóa và tạo lại migrations (KHUYẾN NGHỊ nếu database trống hoặc có thể reset)

**Bước 1: Xóa tất cả migrations**
```bash
cd "d:\Study\MaiAmTinhThuong\MaiAmTinhThuong\MaiAmTinhThuong"
Remove-Item -Recurse -Force Migrations\*
```

**Bước 2: Xóa database trên Railway**
- Vào Railway Dashboard
- Vào PostgreSQL service
- Settings → Delete Database (hoặc Reset)

**Bước 3: Tạo lại migrations với PostgreSQL**
```bash
# Đảm bảo DATABASE_URL đang trỏ đến PostgreSQL
$env:DATABASE_URL = "postgresql://user:pass@host:port/db"

# Tạo migration mới
dotnet ef migrations add InitialCreatePostgreSQL --context ApplicationDbContext
```

**Bước 4: Deploy lại**

---

### Option 2: Sửa migrations thủ công (Nếu có dữ liệu quan trọng)

Cần sửa các file migration để thay thế:
- `nvarchar` → `varchar` hoặc `text`
- `datetime2` → `timestamp without time zone`
- `bit` → `boolean`
- `int` với `SqlServer:Identity` → `SERIAL` hoặc `integer` với `NpgsqlValueGenerationStrategy.SerialColumn`
- `datetimeoffset` → `timestamp with time zone`

**File cần sửa:**
- `20250418105325_InitialCreate.cs` (quan trọng nhất)
- Tất cả migrations khác có dùng SQL Server types

---

## Kiểm tra thủ công

Sau khi sửa, bạn cần kiểm tra:

1. **Database trên Railway:**
   - Vào Railway Dashboard → PostgreSQL service
   - Kiểm tra xem có dữ liệu quan trọng không
   - Nếu có, backup trước khi reset

2. **Connection String:**
   - Vào Railway → Web service → Variables
   - Kiểm tra `DATABASE_URL` có đúng format PostgreSQL không
   - Format: `postgresql://user:password@host:port/database`

3. **Migrations History:**
   - Nếu database đã có migrations history, cần xóa bảng `__EFMigrationsHistory` trước khi chạy lại

---

## Lệnh kiểm tra database trên Railway

Nếu bạn có quyền truy cập database, chạy:
```sql
-- Kiểm tra migrations đã chạy
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";

-- Kiểm tra tables
SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';
```

---

## Khuyến nghị

**Nếu database trống hoặc có thể reset:** Dùng Option 1 (xóa và tạo lại)

**Nếu có dữ liệu quan trọng:** Dùng Option 2 (sửa thủ công) hoặc backup → reset → restore







