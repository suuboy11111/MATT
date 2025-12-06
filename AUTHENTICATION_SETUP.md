# Hướng dẫn cấu hình Authentication

## ✅ Đã hoàn thành

1. **Email Verification** - Gửi email xác nhận khi đăng ký
2. **Google OAuth** - Đăng nhập/đăng ký bằng Google
3. **Admin Account** - Tự động tạo tài khoản admin khi khởi động
4. **UI/UX** - Giao diện đăng nhập/đăng ký hiện đại

## 📧 Cấu hình Email (Gmail)

### Bước 1: Tạo App Password cho Gmail

1. Vào [Google Account](https://myaccount.google.com/)
2. Security → 2-Step Verification (bật nếu chưa có)
3. Security → App passwords
4. Tạo App Password mới cho "Mail"
5. Copy password (16 ký tự)

### Bước 2: Cấu hình trong Railway/Environment Variables

Thêm các biến môi trường sau:

```
Email__SmtpHost=smtp.gmail.com
Email__SmtpPort=587
Email__SmtpUser=your-email@gmail.com
Email__SmtpPassword=your-app-password-16-chars
Email__FromEmail=your-email@gmail.com
Email__FromName=Mái Ấm Tình Thương
```

**Lưu ý:** Dùng App Password, KHÔNG dùng mật khẩu Gmail thông thường!

## 🔐 Cấu hình Google OAuth

### Bước 1: Tạo Google OAuth Credentials

1. Vào [Google Cloud Console](https://console.cloud.google.com/)
2. Tạo project mới hoặc chọn project có sẵn
3. APIs & Services → Credentials
4. Create Credentials → OAuth client ID
5. Application type: Web application
6. Authorized redirect URIs:
   - Development: `https://localhost:5001/Account/GoogleCallback`
   - Production: `https://your-domain.com/Account/GoogleCallback`
7. Copy Client ID và Client Secret

### Bước 2: Cấu hình trong Railway/Environment Variables

Thêm các biến môi trường sau:

```
Authentication__Google__ClientId=your-google-client-id
Authentication__Google__ClientSecret=your-google-client-secret
```

## 👤 Tài khoản Admin

Tài khoản admin được tự động tạo khi ứng dụng khởi động lần đầu.

### Mặc định:
- **Email:** `admin@maiamtinhthuong.vn`
- **Password:** `Admin@123456`

### Tùy chỉnh (Optional):

Có thể override bằng environment variables:

```
ADMIN_EMAIL=your-admin@email.com
ADMIN_PASSWORD=YourSecurePassword123!
```

## 🚀 Tính năng

### Email Verification
- Khi đăng ký, user sẽ nhận email xác nhận
- Phải click link trong email để kích hoạt tài khoản
- Link có thời hạn 24 giờ
- Có thể gửi lại email xác nhận

### Google OAuth
- Đăng nhập/đăng ký nhanh bằng Google
- Tự động lấy thông tin từ Google (email, tên, avatar)
- Email tự động được xác nhận khi dùng Google

### Security
- Yêu cầu xác nhận email trước khi đăng nhập
- Password requirements: tối thiểu 6 ký tự, có chữ hoa, chữ thường, số
- Account lockout sau nhiều lần đăng nhập sai

## 📝 Lưu ý

1. **Email Service:** Nếu không cấu hình email, ứng dụng vẫn chạy nhưng không gửi được email xác nhận. User sẽ không thể đăng nhập.

2. **Google OAuth:** Nếu không cấu hình Google OAuth, nút "Đăng nhập với Google" sẽ không hoạt động.

3. **Development:** Trong môi trường development, có thể tạm thời set `RequireConfirmedAccount = false` trong `Program.cs` để test mà không cần email.

4. **Production:** Đảm bảo đã cấu hình đầy đủ email và Google OAuth trước khi deploy.

## 🔧 Troubleshooting

### Email không gửi được
- Kiểm tra App Password đã đúng chưa
- Kiểm tra 2-Step Verification đã bật chưa
- Kiểm tra firewall/network có chặn port 587 không

### Google OAuth không hoạt động
- Kiểm tra redirect URI đã đúng domain chưa
- Kiểm tra Client ID và Client Secret đã đúng chưa
- Kiểm tra Google OAuth consent screen đã cấu hình chưa

### Admin account không tạo được
- Kiểm tra database connection
- Kiểm tra logs để xem lỗi cụ thể
- Đảm bảo migration đã chạy thành công
