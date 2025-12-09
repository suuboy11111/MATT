# 🔗 Hướng dẫn Link Services trên Railway

## ⚠️ Lỗi hiện tại:
`Name or service not known` khi kết nối `postgres.railway.internal`

Nguyên nhân: **Web service và PostgreSQL service chưa được link với nhau**

## ✅ Cách fix:

### Bước 1: Kiểm tra Services trong Project
1. Vào Railway dashboard
2. Mở project của bạn
3. Kiểm tra xem có **2 services**:
   - **Web service** (MATT - chạy ứng dụng)
   - **PostgreSQL service** (database)

### Bước 2: Link Services

**Cách A: Sử dụng Service Reference (Khuyến nghị)**

1. Vào **Web service** (MATT) → tab **"Variables"**
2. Xóa biến `DATABASE_URL` hiện tại (nếu đã set thủ công)
3. Click **"+ New Variable"**
4. Name: `DATABASE_URL`
5. Value: `${{Postgres.DATABASE_URL}}`
   - Thay `Postgres` bằng tên service PostgreSQL của bạn (thường là `Postgres` hoặc tên bạn đặt)
6. Click **"Add"**

**Cách B: Link Services trong Settings**

1. Vào **Web service** → tab **"Settings"**
2. Scroll xuống phần **"Service Dependencies"** hoặc **"Connected Services"**
3. Click **"Connect Service"** hoặc **"Add Dependency"**
4. Chọn **PostgreSQL service** của bạn
5. Railway sẽ tự động tạo biến môi trường

### Bước 3: Kiểm tra Service Name

Nếu dùng `${{Postgres.DATABASE_URL}}` không hoạt động:

1. Vào **PostgreSQL service** → tab **"Settings"**
2. Xem **"Service Name"** (ví dụ: `Postgres`, `PostgreSQL`, `db`, v.v.)
3. Dùng tên đó trong reference: `${{ServiceName.DATABASE_URL}}`

### Bước 4: Redeploy

- Railway sẽ tự động redeploy khi bạn thêm/link services
- Hoặc click **"Redeploy"** trong Web service

### Bước 5: Kiểm tra Logs

Sau khi redeploy, kiểm tra logs:
- ✅ `DATABASE_URL found: postgresql://...`
- ✅ `PostgreSQL connection configured: Host=..., Database=...`
- ✅ `Attempting database connection (attempt 1/5)...`
- ✅ `Database migration completed successfully.`

## 🔍 Troubleshooting:

### Nếu vẫn lỗi "Name or service not known":

1. **Kiểm tra service name:**
   - Đảm bảo tên service trong reference đúng
   - Ví dụ: Nếu PostgreSQL service tên là `db`, dùng `${{db.DATABASE_URL}}`

2. **Kiểm tra services đã link:**
   - Vào Web service → Settings
   - Xem có "Connected Services" hoặc "Dependencies" không

3. **Thử dùng PUBLIC_URL (tạm thời):**
   - Vào PostgreSQL service → Variables
   - Copy `DATABASE_PUBLIC_URL`
   - Paste vào Web service → Variables → `DATABASE_URL`
   - ⚠️ Chỉ dùng tạm thời để test, sau đó chuyển về internal URL

4. **Kiểm tra cả 2 services đều đang chạy:**
   - PostgreSQL service phải "Active"
   - Web service phải "Active"

## 📝 Lưu ý:

- **Internal URL** (`postgres.railway.internal`) chỉ hoạt động khi services được link
- **Public URL** có thể dùng từ bên ngoài nhưng chậm hơn và tốn băng thông
- Code đã được cập nhật với retry logic (5 lần thử, mỗi lần tăng delay)
- Migration sẽ chạy trong background, không block startup






