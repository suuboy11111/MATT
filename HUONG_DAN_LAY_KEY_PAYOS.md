# Hướng dẫn chi tiết: Lấy Client ID, API Key và Checksum Key từ PayOS

## 📋 Tổng quan
Sau khi đã tạo tài khoản PayOS, bạn cần lấy 3 thông tin quan trọng để tích hợp:
- **Client ID**
- **API Key** 
- **Checksum Key**

## 🔍 Các bước chi tiết

### Bước 1: Đăng nhập vào PayOS Dashboard
1. Truy cập: https://my.payos.vn/login
2. Đăng nhập bằng tài khoản bạn vừa tạo

### Bước 2: Tạo kênh thanh toán (nếu chưa có)

#### Cách 1: Từ trang chủ
1. Sau khi đăng nhập, bạn sẽ thấy dashboard
2. Tìm và click vào **"Kênh thanh toán"** hoặc **"Payment Channels"**
3. Click nút **"Tạo kênh thanh toán"** hoặc **"Tạo mới"** (+)
4. Điền form:
   - **Tên kênh**: Ví dụ "Mái Ấm Tình Thương"
   - **Mô tả**: (tùy chọn)
   - **Logo**: Upload logo (tùy chọn)
   - **Ngân hàng**: Chọn ngân hàng bạn muốn
5. Click **"Tạo"** hoặc **"Lưu"**

#### Cách 2: Từ menu
1. Click vào menu **"Tích hợp"** (Integration) hoặc **"API"**
2. Làm theo hướng dẫn tạo kênh mới

### Bước 3: Lấy các Key

Sau khi tạo kênh thành công, bạn sẽ thấy các thông tin sau:

#### Vị trí 1: Trang chi tiết kênh thanh toán
- Sau khi tạo xong, bạn sẽ được chuyển đến trang chi tiết kênh
- Tìm phần **"Thông tin tích hợp"** hoặc **"Integration Info"**
- Hoặc tab **"API"** / **"Tích hợp"**

#### Vị trí 2: Trang Settings/API Keys
1. Vào **"Cài đặt"** (Settings) → **"API"** hoặc **"Tích hợp"**
2. Hoặc vào **"API Keys"** từ menu
3. Tìm phần hiển thị các key

#### Vị trí 3: Trang Integration/Developer
1. Vào **"Tích hợp"** (Integration) hoặc **"Developer"**
2. Chọn kênh thanh toán bạn vừa tạo
3. Xem thông tin xác thực

### Bước 4: Copy các Key

Bạn sẽ thấy 3 thông tin sau (có thể tên hơi khác một chút):

1. **Client ID** 
   - Có thể gọi là: "Client ID", "App ID", "Application ID"
   - Format: Thường là chuỗi số hoặc UUID
   - Ví dụ: `12345678` hoặc `12345678-1234-1234-1234-123456789012`

2. **API Key**
   - Có thể gọi là: "API Key", "Secret Key", "API Secret"
   - Format: Chuỗi ký tự dài (thường 32-64 ký tự)
   - Ví dụ: `a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6`

3. **Checksum Key**
   - Có thể gọi là: "Checksum Key", "Webhook Secret", "Checksum Secret"
   - Format: Chuỗi ký tự dài tương tự API Key
   - Ví dụ: `q1w2e3r4t5y6u7i8o9p0a1s2d3f4g5h6`

### Bước 5: Lưu các Key an toàn

⚠️ **QUAN TRỌNG:**
- Copy và lưu các key này vào file text an toàn
- **KHÔNG** chia sẻ với ai
- **KHÔNG** commit vào Git (đã có trong .gitignore)
- Nếu mất, có thể tạo lại trong dashboard (key cũ sẽ bị vô hiệu)

## 🔧 Nếu không tìm thấy Key

### Trường hợp 1: Chưa tạo kênh thanh toán
- Bạn phải tạo kênh thanh toán trước
- Key chỉ xuất hiện sau khi tạo kênh thành công

### Trường hợp 2: Đang ở chế độ Test/Sandbox
- Một số tài khoản mới có thể cần xác thực trước
- Kiểm tra email xác thực từ PayOS

### Trường hợp 3: Giao diện khác
- PayOS có thể cập nhật giao diện
- Thử tìm trong: Settings → Integration → API Keys
- Hoặc liên hệ support PayOS

## 📞 Hỗ trợ

Nếu vẫn không tìm thấy:
- Email: support@payos.vn
- Hotline: (nếu có)
- Tài liệu: https://payos.vn/docs

## ✅ Sau khi có Key

Sau khi lấy được 3 key, cập nhật vào file `appsettings.json`:

```json
{
  "PayOS": {
    "ClientId": "PASTE_CLIENT_ID_HERE",
    "ApiKey": "PASTE_API_KEY_HERE",
    "ChecksumKey": "PASTE_CHECKSUM_KEY_HERE"
  }
}
```

Sau đó chạy lại ứng dụng và test thanh toán!


