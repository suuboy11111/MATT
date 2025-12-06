# 📧 Hướng dẫn cấu hình SendGrid

## Bước 1: Tạo tài khoản SendGrid

1. Truy cập [SendGrid](https://sendgrid.com/)
2. Click **"Start for free"** hoặc **"Sign Up"**
3. Điền thông tin:
   - Email
   - Password
   - Company name (tùy chọn)
4. Xác nhận email

## Bước 2: Xác thực Sender Identity

### Option A: Single Sender Verification (Dễ nhất - Khuyến nghị)

1. Vào **Settings** → **Sender Authentication**
2. Click **"Verify a Single Sender"**
3. Điền thông tin:
   - **From Email**: `maiamtinhthuongverify@gmail.com` (hoặc email bạn muốn dùng)
   - **From Name**: `Mái Ấm Tình Thương`
   - **Reply To**: Cùng email với From Email
   - **Address**: Địa chỉ của bạn
   - **City**: Thành phố
   - **State**: Tỉnh/Thành phố
   - **Country**: Vietnam
   - **Zip Code**: Mã bưu điện
4. Click **"Create"**
5. **QUAN TRỌNG**: Kiểm tra email và click vào link xác nhận trong email từ SendGrid
6. Sau khi xác nhận, sender sẽ có trạng thái **"Verified"**

### Option B: Domain Authentication (Nâng cao - Cần domain riêng)

Nếu bạn có domain riêng (ví dụ: `maiamtinhthuong.vn`), có thể dùng Domain Authentication để gửi từ bất kỳ email nào trong domain đó.

## Bước 3: Tạo API Key

1. Vào **Settings** → **API Keys**
2. Click **"Create API Key"**
3. Đặt tên: `MaiAmTinhThuong Production`
4. Chọn **"Full Access"** hoặc **"Restricted Access"** (nếu chọn Restricted, cần chọn quyền **Mail Send**)
5. Click **"Create & View"**
6. **QUAN TRỌNG**: Copy API Key ngay lập tức (chỉ hiển thị 1 lần!)
   - Format: `SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`
   - Lưu vào nơi an toàn

## Bước 4: Cấu hình trong Railway

1. Vào Railway → Service của bạn → **Variables** tab
2. Thêm các biến môi trường sau:

### Biến bắt buộc:
```
Email__SendGridApiKey=SG.your-api-key-here
Email__FromEmail=maiamtinhthuongverify@gmail.com
Email__FromName=Mái Ấm Tình Thương
```

### Biến tùy chọn (có thể xóa các biến SMTP cũ):
```
Email__SmtpHost (có thể xóa)
Email__SmtpPort (có thể xóa)
Email__SmtpUser (có thể xóa)
Email__SmtpPassword (có thể xóa)
```

## Bước 5: Deploy và Test

1. Commit và push code lên repository
2. Railway sẽ tự động deploy
3. Thử đăng ký tài khoản mới
4. Kiểm tra email inbox (có thể trong Spam folder)

## ⚠️ Lưu ý quan trọng

1. **Free Tier**: SendGrid free tier cho phép gửi **100 emails/ngày**
   - Đủ cho hầu hết các ứng dụng nhỏ
   - Nếu cần nhiều hơn, có thể upgrade

2. **API Key Security**:
   - KHÔNG commit API key vào Git
   - Chỉ lưu trong Railway Variables
   - Nếu API key bị lộ, xóa ngay và tạo key mới

3. **Sender Verification**:
   - Phải verify sender email trước khi gửi được
   - Nếu không verify, email sẽ bị reject

4. **Rate Limits**:
   - Free tier: 100 emails/ngày
   - Nếu vượt quá, sẽ nhận lỗi 429 (Too Many Requests)

## 🔍 Troubleshooting

### Email không được gửi:
1. Kiểm tra API key có đúng không
2. Kiểm tra sender email đã verify chưa
3. Kiểm tra Railway logs để xem lỗi cụ thể
4. Kiểm tra SendGrid Activity Feed để xem email có được gửi không

### Email vào Spam:
- SendGrid có reputation tốt, nhưng một số email vẫn có thể vào Spam
- Đảm bảo sender email đã được verify
- Cân nhắc dùng Domain Authentication nếu có domain riêng

## 📊 Kiểm tra Email Status

1. Vào SendGrid Dashboard → **Activity**
2. Xem tất cả emails đã gửi
3. Xem status: Delivered, Bounced, Blocked, etc.

## ✅ Checklist

- [ ] Đã tạo tài khoản SendGrid
- [ ] Đã verify sender email
- [ ] Đã tạo API Key
- [ ] Đã thêm `Email__SendGridApiKey` vào Railway Variables
- [ ] Đã thêm `Email__FromEmail` vào Railway Variables
- [ ] Đã thêm `Email__FromName` vào Railway Variables
- [ ] Đã deploy code mới
- [ ] Đã test đăng ký và nhận được email

