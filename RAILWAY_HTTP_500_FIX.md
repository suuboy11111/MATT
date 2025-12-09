# 🔧 Hướng dẫn Fix HTTP 500 Error trên Railway

## ⚠️ Lỗi hiện tại:
Server "Online" nhưng website trả về **HTTP 500 Internal Server Error**

## 🔍 Bước 1: Kiểm tra Logs (QUAN TRỌNG NHẤT)

1. Vào **MATT service** trên Railway
2. Click tab **"Logs"** hoặc **"Deployments"**
3. Click vào deployment mới nhất
4. Xem logs để tìm lỗi cụ thể

### Các lỗi thường gặp:

#### ❌ Lỗi 1: Database Connection
```
System.InvalidOperationException: DATABASE_URL environment variable is required
```
**Fix:** Xem phần "Fix Database Connection" bên dưới

#### ❌ Lỗi 2: PayOS Configuration
```
System.InvalidOperationException: PayOS configuration is missing
```
**Fix:** Xem phần "Fix PayOS Configuration" bên dưới

#### ❌ Lỗi 3: Migration Error
```
An error occurred while migrating the database
```
**Fix:** Xem phần "Fix Migration Error" bên dưới

#### ❌ Lỗi 4: Port Configuration
```
Address already in use
```
**Fix:** Railway tự động quản lý PORT, không cần fix

---

## ✅ Fix Database Connection

### Kiểm tra:
1. Vào **MATT service** → tab **"Variables"**
2. Kiểm tra có biến `DATABASE_URL` không
3. Kiểm tra giá trị có đúng format `postgresql://...` không

### Nếu chưa có hoặc sai:

1. Vào **MATT service** → tab **"Variables"**
2. Click **"+ New Variable"**
3. Name: `DATABASE_URL`
4. Value: `${{Postgres.DATABASE_URL}}`
   - Thay `Postgres` bằng tên PostgreSQL service của bạn
5. Click **"Add"**
6. Railway sẽ tự động redeploy

### Kiểm tra lại:
- Vào **Logs** → tìm dòng: `✅ PostgreSQL connection configured: Host=..., Database=...`

---

## ✅ Fix PayOS Configuration

Code yêu cầu PayOS keys, nhưng có thể làm cho nó optional trong production.

### Cách 1: Thêm PayOS Keys (Nếu cần dùng PayOS)

1. Vào **MATT service** → tab **"Variables"**
2. Thêm 3 biến:
   ```
   PayOS__ClientId=9ca8c566-b2e8-4497-88fc-a5ad18f477f8
   PayOS__ApiKey=4209e4e9-a757-4104-ad73-d21d18e9037a
   PayOS__ChecksumKey=05a4aafcabab2416009875d0b95b999f5faa6827a08562b2fa2972ef3b5b55ab
   ```
3. Railway sẽ tự động redeploy

### Cách 2: Làm PayOS Optional (Nếu không cần PayOS ngay)

Cần sửa code để PayOS không bắt buộc (sẽ hướng dẫn sau nếu cần)

---

## ✅ Fix Migration Error

### Nếu lỗi migration:

1. Kiểm tra logs để xem lỗi cụ thể
2. Thường là do:
   - Database chưa sẵn sàng
   - Connection string sai
   - Schema conflict

### Code đã có retry logic:
- Tự động retry 5 lần
- Delay tăng dần (2s, 4s, 8s, 16s, 32s)
- Không block startup nếu migration fail

### Nếu vẫn lỗi:
- Kiểm tra `DATABASE_URL` đã đúng chưa
- Kiểm tra PostgreSQL service đang "Online"

---

## 🔍 Checklist Debug

Kiểm tra từng mục:

- [ ] **DATABASE_URL** đã được set trong Variables
- [ ] **DATABASE_URL** có giá trị đúng format `postgresql://...`
- [ ] **PostgreSQL service** đang "Online"
- [ ] **MATT service** đang "Online"
- [ ] **Logs** không có exception nào
- [ ] **Port** được config đúng (8080 trong Settings)

---

## 📋 Các bước kiểm tra nhanh:

1. **Vào Logs:**
   - MATT service → Logs tab
   - Xem dòng cuối cùng có lỗi gì

2. **Kiểm tra Variables:**
   - MATT service → Variables tab
   - Đảm bảo có `DATABASE_URL`

3. **Kiểm tra Services:**
   - Cả Postgres và MATT đều "Online"

4. **Redeploy:**
   - Nếu đã sửa Variables, đợi redeploy xong
   - Hoặc click "Redeploy" thủ công

---

## 🚨 Nếu vẫn lỗi:

1. **Copy toàn bộ logs** từ Railway
2. **Gửi cho tôi** để phân tích
3. Hoặc **screenshot** phần lỗi trong logs

---

## 💡 Lưu ý:

- HTTP 500 = Server Error = Code/Config có vấn đề
- Server "Online" chỉ có nghĩa là container đang chạy
- Nhưng application bên trong có thể crash khi startup
- **Logs là nguồn thông tin quan trọng nhất!**







