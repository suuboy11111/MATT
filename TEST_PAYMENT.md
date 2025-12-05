# Hướng dẫn Test Thanh toán PayOS

## ✅ Cấu hình đã hoàn tất
Các key PayOS đã được cập nhật vào `appsettings.json`:
- ✅ Client ID: `9ca8c566-b2e8-4497-88fc-a5ad18f477f8`
- ✅ Api Key: `4209e4e9-a757-4104-ad73-d21d18e9037a`
- ✅ Checksum Key: `05a4aafcabab2416009875d0b95b999f5faa6827a085...`

## 🧪 Các bước test

### Bước 1: Chạy ứng dụng
```bash
dotnet run
```

### Bước 2: Kiểm tra cấu hình (Không tạo thanh toán)
Truy cập: `http://localhost:5000/api/PaymentTest/check-config`

Kết quả mong đợi:
```json
{
  "hasClientId": true,
  "hasApiKey": true,
  "hasChecksumKey": true,
  "isPlaceholder": false,
  "isValid": true
}
```

### Bước 3: Test kết nối PayOS (Tạo thanh toán thử)
**Cách 1: Dùng Postman/Thunder Client**
- Method: POST
- URL: `http://localhost:5000/api/PaymentTest/test-connection`
- Headers: `Content-Type: application/json`

**Cách 2: Dùng curl**
```bash
curl -X POST http://localhost:5000/api/PaymentTest/test-connection
```

Kết quả thành công sẽ trả về `checkoutUrl` để bạn test thanh toán.

### Bước 4: Test giao diện thanh toán
1. Truy cập: `http://localhost:5000/Payment/Donate`
2. Hoặc vào form đăng ký người hỗ trợ → Chọn "Tài chính"
3. Điền thông tin:
   - Họ tên: Test User
   - Số tiền: 10000 (tối thiểu)
   - Chọn mái ấm (tùy chọn)
4. Click "Thanh toán ngay"
5. Sẽ chuyển đến trang thanh toán PayOS

### Bước 5: Test thanh toán
- **Trong môi trường test**: PayOS có thể có tài khoản test
- **Thanh toán thành công**: Sẽ quay về `/Payment/Success`
- **Hủy thanh toán**: Sẽ quay về `/Payment/Cancel`

## ⚠️ Lưu ý quan trọng

### Cấu hình Webhook
Bạn cần cấu hình webhook trong PayOS dashboard:
1. Vào phần **Webhook** trong PayOS dashboard
2. Thêm webhook URL: 
   - **Development**: `https://your-ngrok-url.ngrok.io/Payment/Webhook`
   - **Production**: `https://yourdomain.com/Payment/Webhook`
3. Chọn các sự kiện: `payment.paid`, `payment.cancelled`

### Sử dụng ngrok cho development
```bash
# Cài đặt ngrok (nếu chưa có)
# Sau đó chạy:
ngrok http 5000

# Copy URL HTTPS từ ngrok và cấu hình vào PayOS webhook
```

## 🔍 Kiểm tra lỗi

Nếu gặp lỗi, kiểm tra:
1. **Console logs**: Xem lỗi chi tiết trong terminal
2. **Browser console**: F12 → Console tab
3. **Network tab**: Xem request/response

### Lỗi thường gặp:
- ❌ "Cấu hình PayOS chưa được thiết lập" → Kiểm tra appsettings.json
- ❌ "Invalid credentials" → Kiểm tra lại các key
- ❌ "Connection timeout" → Kiểm tra kết nối internet

## ✅ Khi test thành công
- Bạn sẽ thấy trang thanh toán PayOS
- Sau khi thanh toán, quay về trang success
- Transaction được lưu vào database trong bảng `TransactionHistories`


