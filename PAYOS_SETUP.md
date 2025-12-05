# Hướng dẫn cấu hình PayOS

## Bước 1: Đăng ký tài khoản PayOS
1. Truy cập https://my.payos.vn/login
2. Đăng ký tài khoản mới (nếu chưa có)
3. Đăng nhập vào dashboard

## Bước 2: Tạo kênh thanh toán và lấy các Key

### 2.1. Tạo kênh thanh toán
1. Sau khi đăng nhập, vào mục **"Kênh thanh toán"** (Payment Channels) hoặc **"Tích hợp"** (Integration)
2. Nhấn nút **"Tạo kênh thanh toán"** hoặc **"Tạo mới"**
3. Điền thông tin:
   - **Tên kênh**: Ví dụ "Mái Ấm Tình Thương"
   - **Logo**: Tải lên logo (tùy chọn)
   - **Ngân hàng chính**: Chọn ngân hàng bạn muốn sử dụng
4. Nhấn **"Tạo kênh thanh toán và tích hợp"**

### 2.2. Lấy thông tin xác thực
Sau khi tạo kênh thành công, bạn sẽ thấy các thông tin sau (thường hiển thị ngay sau khi tạo hoặc trong phần **"Thông tin tích hợp"**):

1. **Client ID** (hoặc Client ID)
   - Ví dụ: `12345678-1234-1234-1234-123456789012`

2. **API Key** (hoặc Secret Key)
   - Ví dụ: `a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6`

3. **Checksum Key** (hoặc Webhook Secret)
   - Ví dụ: `q1w2e3r4t5y6u7i8o9p0a1s2d3f4g5h6`

### 2.3. Nếu không thấy các Key ngay
- Vào **"Cài đặt"** (Settings) → **"Tích hợp"** (Integration)
- Hoặc vào **"API Keys"** / **"Thông tin API"**
- Tìm phần **"Thông tin xác thực"** hoặc **"Credentials"**

### 2.4. Lưu ý quan trọng
- ⚠️ **KHÔNG chia sẻ** các key này với ai
- ⚠️ **Lưu lại** các key này ở nơi an toàn
- ⚠️ Nếu mất key, bạn có thể tạo lại trong dashboard (key cũ sẽ bị vô hiệu)

## Bước 3: Cấu hình trong appsettings.json
Mở file `appsettings.json` và cập nhật thông tin PayOS:

```json
{
  "PayOS": {
    "ClientId": "YOUR_CLIENT_ID",
    "ApiKey": "YOUR_API_KEY",
    "ChecksumKey": "YOUR_CHECKSUM_KEY"
  }
}
```

## Bước 4: Cấu hình Webhook (Quan trọng)
1. Trong dashboard PayOS, vào phần **Webhook**
2. Thêm webhook URL: `https://yourdomain.com/Payment/Webhook`
3. Chọn các sự kiện: `payment.paid`, `payment.cancelled`

## Bước 5: Cập nhật appsettings.json với thông tin thực tế

Sau khi lấy được các key, mở file `appsettings.json` và thay thế:

```json
{
  "PayOS": {
    "ClientId": "12345678-1234-1234-1234-123456789012",  // Thay bằng Client ID thực tế
    "ApiKey": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6",        // Thay bằng API Key thực tế
    "ChecksumKey": "q1w2e3r4t5y6u7i8o9p0a1s2d3f4g5h6"    // Thay bằng Checksum Key thực tế
  }
}
```

## Bước 6: Kiểm tra
1. Chạy ứng dụng
2. Truy cập `/Payment/Donate` hoặc chọn "Tài chính" trong form đăng ký người hỗ trợ
3. Nhập thông tin và thử thanh toán

## Lưu ý:
- Trong môi trường development, có thể sử dụng webhook local với công cụ như ngrok
- Đảm bảo HTTPS được bật cho webhook trong production
- Kiểm tra logs nếu có lỗi xảy ra

