# ✅ Kiểm tra phần thanh toán cho Demo Local

## 📋 Tình trạng hiện tại

### ✅ Đã hoàn thành:
1. **PaymentController** - Đã có đầy đủ chức năng:
   - ✅ Tạo payment link (`CreatePaymentLink`)
   - ✅ Xử lý thanh toán thành công (`Success`)
   - ✅ Xử lý hủy thanh toán (`Cancel`)
   - ✅ Xử lý webhook từ PayOS (`Webhook`)

2. **Giao diện**:
   - ✅ Trang form ủng hộ (`/Payment/Donate`)
   - ✅ Trang kết quả thanh toán (`/Payment/Success`)
   - ✅ Form validation và UI đẹp

3. **Database**:
   - ✅ Model `TransactionHistory` đã có
   - ✅ Lưu transaction vào database

4. **Cấu hình PayOS**:
   - ✅ Client ID, API Key, Checksum Key đã có trong `appsettings.json`

### ⚠️ Vấn đề cần xử lý cho Demo Local:

**PayOS yêu cầu URL công khai (HTTPS)** cho:
- ReturnUrl (sau khi thanh toán thành công)
- CancelUrl (khi hủy thanh toán)
- Webhook URL (để PayOS gửi thông báo)

**Localhost không phải URL công khai**, nên cần dùng **ngrok** hoặc **localtunnel**.

---

## 🚀 Hướng dẫn Demo Local

### Bước 1: Cài đặt và chạy ngrok

1. **Tải ngrok**: https://ngrok.com/download
2. **Đăng ký tài khoản miễn phí** tại https://ngrok.com (để lấy auth token)
3. **Cấu hình ngrok** (chạy 1 lần):
   ```bash
   ngrok config add-authtoken YOUR_AUTH_TOKEN
   ```
4. **Chạy ngrok** (mở terminal mới):
   ```bash
   ngrok http 5000
   # Hoặc port khác nếu app chạy ở port khác (5001, 7000, etc.)
   ```

5. **Copy URL HTTPS** từ ngrok, ví dụ:
   ```
   Forwarding: https://abc123.ngrok-free.app -> http://localhost:5000
   ```
   → URL cần dùng: `https://abc123.ngrok-free.app`

### Bước 2: Cập nhật cấu hình

Mở `appsettings.Development.json` và thêm:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "PayOS": {
    "BaseUrl": "https://abc123.ngrok-free.app"
  }
}
```

**Lưu ý**: Thay `abc123.ngrok-free.app` bằng URL ngrok thực tế của bạn.

### Bước 3: Cấu hình Webhook trong PayOS Dashboard

1. Đăng nhập vào **PayOS Dashboard**: https://pay.payos.vn/
2. Vào phần **Webhook**
3. Thêm webhook URL: `https://abc123.ngrok-free.app/Payment/Webhook`
4. Chọn các sự kiện:
   - ✅ `payment.paid`
   - ✅ `payment.cancelled`

### Bước 4: Chạy ứng dụng

```bash
dotnet run
```

Ứng dụng sẽ chạy tại: `http://localhost:5000`

### Bước 5: Test thanh toán

1. **Truy cập**: `http://localhost:5000/Payment/Donate`
   (Hoặc qua ngrok: `https://abc123.ngrok-free.app/Payment/Donate`)

2. **Điền form**:
   - Họ tên: Test User
   - Số tiền: 10000 (tối thiểu)
   - Chọn mái ấm (tùy chọn)

3. **Click "Thanh toán ngay"**
   - Sẽ chuyển đến trang thanh toán PayOS

4. **Test thanh toán**:
   - ✅ **Thanh toán thành công**: Sẽ quay về `/Payment/Success`
   - ❌ **Hủy thanh toán**: Sẽ quay về `/Payment/Cancel`

---

## ✅ Checklist Demo

Trước khi demo, kiểm tra:

- [ ] ngrok đang chạy và có URL HTTPS
- [ ] `appsettings.Development.json` có `PayOS:BaseUrl` với URL ngrok
- [ ] Webhook đã cấu hình trong PayOS dashboard
- [ ] Ứng dụng đang chạy (`dotnet run`)
- [ ] Có thể truy cập `/Payment/Donate`
- [ ] Form thanh toán hiển thị đúng
- [ ] Click "Thanh toán ngay" → Chuyển đến PayOS
- [ ] Sau khi thanh toán → Quay về trang Success

---

## 🔍 Kiểm tra nhanh

### Test 1: Kiểm tra cấu hình
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

### Test 2: Test kết nối PayOS
POST: `http://localhost:5000/api/PaymentTest/test-connection`

Kết quả thành công sẽ trả về `checkoutUrl`.

---

## ⚠️ Lưu ý quan trọng

1. **URL ngrok thay đổi**: Mỗi lần khởi động lại ngrok, URL sẽ thay đổi (trừ bản trả phí). Cần:
   - Cập nhật lại `appsettings.Development.json`
   - Cập nhật lại webhook trong PayOS dashboard

2. **Chỉ dùng cho Development**: ngrok chỉ dùng cho demo/test, không dùng production.

3. **Production**: Khi deploy lên server thật, chỉ cần:
   - Xóa `PayOS:BaseUrl` trong config (hoặc set thành domain thật)
   - Cập nhật webhook URL trong PayOS dashboard

---

## 🐛 Xử lý lỗi thường gặp

### Lỗi: "Invalid return URL"
- ✅ Kiểm tra `PayOS:BaseUrl` trong `appsettings.Development.json`
- ✅ Đảm bảo URL là HTTPS (ngrok tự động cung cấp HTTPS)

### Lỗi: "Webhook không hoạt động"
- ✅ Kiểm tra webhook URL trong PayOS dashboard
- ✅ Đảm bảo ngrok đang chạy
- ✅ Kiểm tra log trong terminal để xem webhook có được gọi không

### Lỗi: "Không tạo được payment link"
- ✅ Kiểm tra Client ID, API Key trong `appsettings.json`
- ✅ Test kết nối: `/api/PaymentTest/test-connection`
- ✅ Kiểm tra console log để xem lỗi chi tiết

---

## 📝 Tóm tắt

**Phần thanh toán đã sẵn sàng về mặt code**, nhưng để demo local cần:

1. ✅ Cài ngrok
2. ✅ Cấu hình `PayOS:BaseUrl` trong `appsettings.Development.json`
3. ✅ Cấu hình webhook trong PayOS dashboard
4. ✅ Chạy ngrok và ứng dụng cùng lúc

Sau đó có thể demo đầy đủ luồng thanh toán! 🎉

