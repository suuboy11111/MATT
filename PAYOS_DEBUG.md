# 🔍 Hướng dẫn Debug PayOS Signature Error

## ⚠️ Vấn đề hiện tại

Bạn đã cập nhật đúng các giá trị nhưng vẫn gặp lỗi `{"code":"201","desc":"Mã kiểm tra(signature) không hợp lệ"}`.

## 🔍 Các nguyên nhân có thể

### 1. **Tên biến môi trường trên Railway**

**QUAN TRỌNG:** Kiểm tra lại tên biến trên Railway có đúng **2 gạch dưới `__`** không:

- ✅ ĐÚNG: `PayOS__ClientId` (2 gạch dưới)
- ❌ SAI: `PayOS_ClientId` (1 gạch dưới)

**Cách kiểm tra:**
1. Vào Railway Dashboard → Project "MATT" → Tab **Variables**
2. Tìm các biến có tên `PayOS_...` (1 gạch)
3. Nếu có → **XÓA** và tạo lại với tên `PayOS__...` (2 gạch)

### 2. **ChecksumKey không đúng**

Từ ảnh PayOS Dashboard bạn gửi, ChecksumKey hiển thị là:
```
05a4aafcabab2416009875d0b95b999f5faa6827a085
```
(44 ký tự)

Nhưng bạn đang dùng:
```
05a4aafcabab2416009875d0b95b999f5faa6827a08562b2fa2972ef3b5b55ab
```
(dài hơn nhiều)

**Vấn đề:** PayOS Dashboard có thể chỉ hiển thị một phần ChecksumKey. Bạn cần:
1. Vào PayOS Dashboard: https://my.payos.vn
2. Vào phần **"Thông tin tích hợp"** (Integration Information)
3. Click vào icon **👁️** (mắt) bên cạnh **Checksum Key** để hiển thị đầy đủ
4. Copy **TOÀN BỘ** giá trị (không bị cắt)
5. Update lại trên Railway

### 3. **Railway chưa redeploy**

Sau khi cập nhật biến môi trường:
1. Railway sẽ tự động redeploy
2. Đợi deployment hoàn tất (thường 1-2 phút)
3. Kiểm tra logs để xem có lỗi không

### 4. **Kiểm tra logs trên Railway**

1. Vào Railway Dashboard → Project "MATT"
2. Click vào service **web**
3. Vào tab **"Logs"** hoặc **"Deployments"**
4. Tìm các dòng log có chứa:
   - `PayOS Config Check` - Xem độ dài các keys
   - `PayOS Debug` - Xem thông tin signature

**Log mẫu sẽ hiển thị:**
```
warn: PayOS Config Check - ClientId: 36 chars, ApiKey: 36 chars, ChecksumKey: 64 chars
```

Nếu thấy `0 chars` → Biến môi trường chưa được đọc đúng (kiểm tra tên biến).

## ✅ Các bước kiểm tra chi tiết

### Bước 1: Xác nhận ChecksumKey từ PayOS Dashboard

1. Đăng nhập PayOS Dashboard: https://my.payos.vn
2. Chọn project của bạn
3. Vào **"Kênh thanh toán"** → **"Thông tin tích hợp"**
4. Tìm **"Checksum Key"**
5. Click icon **👁️** để hiển thị đầy đủ
6. Copy **TOÀN BỘ** giá trị (không có khoảng trắng ở đầu/cuối)

**Lưu ý:** 
- ChecksumKey thường là chuỗi hex dài (64-128 ký tự)
- Đảm bảo copy đầy đủ, không bị cắt

### Bước 2: Verify trên Railway

1. Vào Railway → Variables
2. Kiểm tra các biến:

```
PayOS__ClientId=9ca8c566-b2e8-4497-88fc-a5ad18f477f8
PayOS__ApiKey=4209e4e9-a757-4104-ad73-d21d18e9037a
PayOS__ChecksumKey=<giá trị đầy đủ từ PayOS Dashboard>
```

**Kiểm tra:**
- Tên biến có **2 gạch dưới** (`__`) không?
- Giá trị không có khoảng trắng ở đầu/cuối
- Giá trị không bị cắt

### Bước 3: Xem logs sau khi deploy

1. Sau khi cập nhật biến, đợi Railway redeploy
2. Vào **Logs** và tìm dòng:
   ```
   warn: PayOS Config Check - ClientId: XX chars, ApiKey: XX chars, ChecksumKey: XX chars
   ```

3. **Nếu thấy:**
   - `ClientId: 0 chars` → Biến `PayOS__ClientId` chưa được đọc
   - `ChecksumKey: 44 chars` → Có thể bị cắt (thường phải > 64)
   - `ChecksumKey: 0 chars` → Biến chưa được set

### Bước 4: Test lại

1. Sau khi deployment xong
2. Thử tạo thanh toán lại trên website
3. Xem có còn lỗi signature không

## 🔧 Nếu vẫn lỗi

Nếu sau khi kiểm tra tất cả các bước trên mà vẫn lỗi:

### Option 1: Kiểm tra signature calculation

Code hiện tại tính signature đúng theo PayOS v2:
- Sắp xếp: `amount`, `cancelUrl`, `description`, `orderCode`, `returnUrl` (alphabetical)
- URL-encode từng giá trị
- HMAC-SHA256 với ChecksumKey

Nhưng có thể PayOS yêu cầu format khác. Thử:

1. Vào PayOS Dashboard → **"Tích hợp"** → **"Tài liệu"**
2. Xem ví dụ signature calculation
3. So sánh với code hiện tại

### Option 2: Sử dụng PayOS SDK

Code đang tự implement signature. Có thể thử dùng PayOS SDK chính thức:

```csharp
// Đã có PayOSClient trong Program.cs nhưng chưa dùng
// Có thể thử dùng SDK thay vì tự tính signature
```

### Option 3: Liên hệ PayOS Support

Nếu tất cả đều đúng mà vẫn lỗi:
1. Chụp ảnh PayOS Dashboard (đầy đủ ChecksumKey)
2. Chụp ảnh Railway Variables
3. Chụp logs từ Railway
4. Liên hệ PayOS Support: https://payos.vn/contact

## 📝 Checklist cuối cùng

- [ ] Đã xác nhận ChecksumKey đầy đủ từ PayOS Dashboard (click icon 👁️)
- [ ] Đã kiểm tra tên biến trên Railway là `PayOS__ClientId` (2 gạch dưới)
- [ ] Đã kiểm tra tên biến trên Railway là `PayOS__ApiKey` (2 gạch dưới)
- [ ] Đã kiểm tra tên biến trên Railway là `PayOS__ChecksumKey` (2 gạch dưới)
- [ ] Đã verify giá trị không có khoảng trắng ở đầu/cuối
- [ ] Đã verify giá trị không bị cắt
- [ ] Railway đã redeploy xong
- [ ] Đã xem logs và thấy độ dài các keys đúng (> 0)
- [ ] Đã test lại và vẫn lỗi → Cần liên hệ PayOS Support

## 🎯 Các giá trị chuẩn từ PayOS Dashboard

Từ ảnh bạn gửi, các giá trị **CHUẨN** là:

```
Client ID: 9ca8c566-b2e8-4497-88fc-a5ad18f477f8 (36 ký tự)
Api Key: 4209e4e9-a757-4104-ad73-d21d18e9037a (36 ký tự)
Checksum Key: 05a4aafcabab2416009875d0b95b999f5faa6827a085 (44 ký tự - CÓ THỂ CHỈ LÀ PHẦN HIỂN THỊ)
```

**⚠️ Lưu ý:** ChecksumKey trong ảnh chỉ hiển thị 44 ký tự, nhưng thực tế có thể dài hơn. Phải click icon 👁️ để xem đầy đủ!
