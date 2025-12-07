# ✅ Checklist Cấu hình Google OAuth

## 📋 Đã cấu hình đúng

### 1. Google Cloud Console - Redirect URIs ✅
- ✅ `https://localhost:5001/Account/GoogleCallback` (Development)
- ✅ `https://matt-production.up.railway.app/Account/GoogleCallback` (Production)
- ✅ Format đúng: `https://` + domain + `/Account/GoogleCallback`
- ✅ Không có trailing slash `/`

### 2. Railway Environment Variables ✅
- ✅ `Authentication__Google__ClientId`: `1001384800442-ehlqsaj4nah5bhf14s6dng0ns0pcrqf4.app.s.googleusercontent.com`
- ✅ `Authentication__Google__ClientSecret`: `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`
- ✅ Format Client ID đúng: `*.app.s.googleusercontent.com`
- ✅ Format Client Secret đúng: `GOCSPX-*`

### 3. Email Configuration ✅
- ✅ `Email__SmtpHost`: `smtp.gmail.com`
- ✅ `Email__SmtpPort`: `587`
- ✅ `Email__SmtpUser`: `maiamtinhthuongverify@gmail.com`
- ✅ `Email__SmtpPassword`: App Password (16 ký tự)
- ✅ `Email__FromEmail`: `maiamtinhthuongverify@gmail.com`
- ✅ `Email__FromName`: `Mái Ấm Tình Thương`

### 4. Admin Account ✅
- ✅ `ADMIN_EMAIL`: `maiamtinhthuongverify@gmail.com`
- ✅ `ADMIN_PASSWORD`: `Aa@123`

---

## ⚠️ Cần kiểm tra thêm

### 1. 🔴 Database Connection (QUAN TRỌNG)
**Vấn đề:** Có warning về `DATABASE_PUBLIC_URL` trong Railway

**Cần làm:**
1. Kiểm tra Railway logs để xem lỗi database cụ thể
2. Đảm bảo PostgreSQL service đã được link đúng với app service
3. Kiểm tra `DATABASE_URL` và `DATABASE_PUBLIC_URL` có đúng không
4. Nếu database không kết nối được, tất cả authentication sẽ fail

**Cách kiểm tra:**
- Vào Railway → Service → Logs
- Tìm dòng: `PostgreSQL database configured` hoặc lỗi database
- Kiểm tra migration có chạy thành công không

---

### 2. 🟡 Google OAuth Consent Screen
**Cần kiểm tra trong Google Cloud Console:**

1. Vào [Google Cloud Console](https://console.cloud.google.com/)
2. APIs & Services → **OAuth consent screen**
3. Kiểm tra các mục sau:

   **a) User Type:**
   - ✅ Nếu test: Chọn "Internal" (chỉ cho users trong organization)
   - ✅ Nếu production: Chọn "External" và cần verify app

   **b) App Information:**
   - ✅ App name: `Mái Ấm Tình Thương` (hoặc tên khác)
   - ✅ User support email: `maiamtinhthuongverify@gmail.com`
   - ✅ App logo (optional)

   **c) App Domain:**
   - ✅ Authorized domains: `up.railway.app` (hoặc domain của bạn)
   - ✅ Application home page: `https://matt-production.up.railway.app`
   - ✅ Privacy policy URL (nếu có)
   - ✅ Terms of service URL (nếu có)

   **d) Scopes:**
   - ✅ Email: `email`
   - ✅ Profile: `profile`
   - ✅ OpenID: `openid`

   **e) Test Users (nếu app chưa verified):**
   - ✅ Thêm email test: `maiamtinhthuongverify@gmail.com`
   - ✅ Thêm các email khác cần test

**Lưu ý:** Nếu app ở chế độ "Testing", chỉ có test users mới đăng nhập được!

---

### 3. 🟡 Authorized JavaScript Origins (Optional nhưng nên có)
**Trong Google Cloud Console → Credentials → OAuth client:**

Thêm vào **Authorized JavaScript origins**:
- `https://matt-production.up.railway.app`
- `https://localhost:5001` (cho development)

**Lưu ý:** Không có trailing slash `/` ở cuối!

---

### 4. 🟡 Thời gian chờ Google cập nhật
**Google note:** "It may take 5 minutes to a few hours for settings to take effect"

**Nếu vừa cấu hình:**
- ⏳ Đợi 5-10 phút
- 🔄 Thử lại đăng nhập bằng Google
- 📝 Kiểm tra Railway logs để xem redirect URI thực tế

---

### 5. 🟡 Kiểm tra Railway Logs
**Sau khi deploy, kiểm tra logs:**

1. Vào Railway → Service → Logs
2. Tìm các dòng sau:
   - ✅ `✅ Google OAuth configured`
   - ✅ `Google OAuth redirect URI: https://matt-production.up.railway.app/Account/GoogleCallback`
   - ✅ `PostgreSQL database configured`
   - ✅ `✅ Admin user created successfully`

3. Nếu có lỗi:
   - ❌ `redirect_uri_mismatch` → Kiểm tra lại redirect URI trong Google Cloud Console
   - ❌ Database connection error → Kiểm tra DATABASE_URL
   - ❌ Email sending failed → Kiểm tra App Password

---

## 🧪 Test Checklist

### Test 1: Database Connection
- [ ] App khởi động thành công
- [ ] Migration chạy thành công
- [ ] Admin account được tạo
- [ ] Có thể query database

### Test 2: Email Service
- [ ] Đăng ký tài khoản mới
- [ ] Nhận email xác nhận
- [ ] Click link xác nhận thành công
- [ ] Có thể đăng nhập sau khi xác nhận

### Test 3: Google OAuth
- [ ] Click "Đăng nhập với Google"
- [ ] Redirect đến Google login page
- [ ] Chọn Google account
- [ ] Redirect về app thành công
- [ ] Đăng nhập thành công
- [ ] User được tạo trong database

### Test 4: Admin Account
- [ ] Đăng nhập với `maiamtinhthuongverify@gmail.com` / `Aa@123`
- [ ] Có quyền Admin
- [ ] Có thể truy cập admin pages

---

## 🔧 Troubleshooting

### Lỗi: `redirect_uri_mismatch`
**Nguyên nhân:** Redirect URI không khớp

**Giải pháp:**
1. Kiểm tra Railway logs để xem redirect URI thực tế
2. So sánh với URI trong Google Cloud Console
3. Đảm bảo khớp chính xác (kể cả `https://` và path)
4. Đợi 5-10 phút sau khi save

### Lỗi: `access_denied` hoặc "This app isn't verified"
**Nguyên nhân:** OAuth consent screen chưa cấu hình hoặc app ở chế độ Testing

**Giải pháp:**
1. Vào OAuth consent screen
2. Thêm email vào Test users
3. Hoặc submit app để verify (cho production)

### Lỗi: Database connection failed
**Nguyên nhân:** DATABASE_URL không đúng hoặc service chưa link

**Giải pháp:**
1. Kiểm tra PostgreSQL service đã được tạo chưa
2. Link PostgreSQL service với app service trong Railway
3. Kiểm tra DATABASE_URL trong environment variables
4. Restart app service

---

## 📝 Ghi chú

1. **Domain Railway:** Nếu domain thay đổi, phải cập nhật lại redirect URI trong Google Cloud Console
2. **App Password:** Phải là App Password (16 ký tự), không phải mật khẩu Gmail thông thường
3. **Test Users:** Nếu app ở chế độ Testing, chỉ test users mới đăng nhập được
4. **Thời gian:** Google có thể mất 5 phút đến vài giờ để cập nhật settings

---

## ✅ Kết luận

**Cấu hình hiện tại:**
- ✅ Google OAuth redirect URIs: **ĐÚNG**
- ✅ Railway environment variables: **ĐÚNG**
- ⚠️ Database connection: **CẦN KIỂM TRA** (có warning)
- ⚠️ OAuth consent screen: **CẦN KIỂM TRA**
- ⚠️ Authorized JavaScript origins: **NÊN THÊM**

**Hành động tiếp theo:**
1. Kiểm tra và sửa database connection warning
2. Kiểm tra OAuth consent screen đã cấu hình đầy đủ chưa
3. Thêm Authorized JavaScript origins (optional)
4. Đợi 5-10 phút sau khi cấu hình
5. Test lại Google OAuth login




