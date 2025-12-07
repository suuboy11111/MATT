# 🔧 Hướng dẫn Setup Database trên Railway

## ⚠️ Lỗi hiện tại:
Web đang cố kết nối SQL Server thay vì PostgreSQL vì `DATABASE_URL` chưa được set.

## ✅ Cách fix nhanh:

### Bước 1: Vào Web Service trên Railway
1. Mở project trên Railway
2. Click vào **Web service** (service chạy ứng dụng của bạn)
3. Vào tab **"Variables"**

### Bước 2: Thêm DATABASE_URL

**Cách A: Sử dụng Railway Reference (Khuyến nghị)**
1. Click **"New Variable"**
2. Name: `DATABASE_URL`
3. Value: `${{Postgres.DATABASE_URL}}`
   - Railway sẽ tự động thay thế bằng connection string từ PostgreSQL service
4. Click **"Add"**

**Cách B: Copy thủ công**
1. Vào **PostgreSQL service** → tab **"Variables"**
2. Copy giá trị của `DATABASE_URL` (dùng internal URL, không dùng PUBLIC_URL)
3. Vào **Web service** → tab **"Variables"**
4. Click **"New Variable"**
5. Name: `DATABASE_URL`
6. Value: Paste connection string đã copy
7. Click **"Add"**

### Bước 3: Redeploy
- Railway sẽ tự động redeploy khi bạn thêm biến môi trường
- Hoặc click **"Redeploy"** trong Web service

### Bước 4: Kiểm tra Logs
1. Vào Web service → tab **"Deployments"**
2. Click vào deployment mới nhất
3. Xem logs, bạn sẽ thấy:
   - ✅ `"DATABASE_URL found, converting to Npgsql format..."`
   - ✅ `"PostgreSQL connection configured: Host=..., Database=..."`
   - ✅ `"Database migration completed successfully."`

## 🔍 Kiểm tra biến môi trường:

Sau khi thêm, trong tab **"Variables"** của Web service, bạn sẽ thấy:
- `DATABASE_URL` = `postgresql://postgres:...@postgres.railway.internal:5432/railway`

## ⚠️ Lưu ý:

- **KHÔNG** dùng `DATABASE_PUBLIC_URL` cho Web service
- Chỉ dùng `DATABASE_URL` (internal URL)
- `DATABASE_PUBLIC_URL` chỉ dùng để kết nối từ bên ngoài (local dev, database tools)

## 🐛 Nếu vẫn lỗi:

1. Kiểm tra logs để xem có thông báo gì
2. Đảm bảo PostgreSQL service đã được tạo
3. Đảm bảo `DATABASE_URL` đã được set trong Web service
4. Kiểm tra format connection string (phải bắt đầu bằng `postgresql://`)



