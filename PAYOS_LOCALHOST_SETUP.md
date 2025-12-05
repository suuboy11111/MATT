# Hướng dẫn tích hợp PayOS với Localhost

## Vấn đề
PayOS cần webhook URL và return URL công khai (HTTPS), nhưng website của bạn chỉ chạy localhost. Có 2 giải pháp:

## Giải pháp 1: Sử dụng ngrok (Khuyến nghị cho Development)

### Bước 1: Cài đặt ngrok
1. Tải ngrok từ: https://ngrok.com/download
2. Giải nén và đặt vào thư mục dễ truy cập
3. Đăng ký tài khoản miễn phí tại https://ngrok.com (để lấy auth token)

### Bước 2: Chạy ngrok
```bash
# Mở terminal/PowerShell mới và chạy:
ngrok http 5000
# (Thay 5000 bằng port mà ứng dụng của bạn đang chạy, thường là 5000 hoặc 5001)
```

### Bước 3: Lấy URL công khai
Ngrok sẽ hiển thị URL công khai, ví dụ:
```
Forwarding: https://abc123.ngrok-free.app -> http://localhost:5000
```

### Bước 4: Cấu hình PayOS
1. Trong dashboard PayOS, vào phần **Webhook**
2. Thêm webhook URL: `https://abc123.ngrok-free.app/Payment/Webhook`
3. Cập nhật ReturnUrl và CancelUrl trong code để sử dụng URL ngrok

### Bước 5: Cập nhật code
Trong `PaymentController.cs`, cập nhật ReturnUrl và CancelUrl:
```csharp
ReturnUrl = $"https://abc123.ngrok-free.app/Payment/Success?orderCode={orderCode}",
CancelUrl = $"https://abc123.ngrok-free.app/Payment/Cancel"
```

**Lưu ý:** URL ngrok thay đổi mỗi lần khởi động lại (trừ khi dùng tài khoản trả phí). Bạn cần cập nhật lại trong PayOS dashboard.

## Giải pháp 2: Sử dụng localtunnel (Miễn phí, không cần đăng ký)

### Bước 1: Cài đặt localtunnel
```bash
npm install -g localtunnel
```

### Bước 2: Chạy localtunnel
```bash
lt --port 5000
```

### Bước 3: Sử dụng URL được cung cấp
Localtunnel sẽ cung cấp URL công khai, ví dụ:
```
https://random-name.loca.lt
```

## Giải pháp 3: Test Mode (Nếu PayOS hỗ trợ)

Một số payment gateway có chế độ test cho phép localhost. Kiểm tra trong PayOS dashboard xem có "Test Mode" hoặc "Sandbox Mode" không.

## Lưu ý quan trọng

1. **Bảo mật**: Chỉ sử dụng ngrok/localtunnel trong môi trường development
2. **URL thay đổi**: URL ngrok/localtunnel thay đổi mỗi lần khởi động (trừ bản trả phí)
3. **HTTPS**: Cả ngrok và localtunnel đều cung cấp HTTPS tự động
4. **Webhook**: Đảm bảo cập nhật webhook URL trong PayOS dashboard mỗi khi URL thay đổi

## Cấu hình tự động (Tùy chọn)

Bạn có thể tạo script để tự động lấy URL ngrok và cập nhật vào appsettings.json, nhưng cách đơn giản nhất là cập nhật thủ công khi cần.

## Production

Khi deploy lên production với domain thật, bạn chỉ cần:
1. Cập nhật webhook URL trong PayOS dashboard thành domain thật
2. Cập nhật ReturnUrl và CancelUrl trong code thành domain thật
3. Đảm bảo HTTPS được bật


