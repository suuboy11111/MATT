# 🔧 Hướng dẫn sửa lỗi PayOS: "Mã kiểm tra (signature) không hợp lệ"

## 🐛 Vấn đề

Lỗi `{"code":"201","desc":"Mã kiểm tra(signature) không hợp lệ"}` xảy ra do các khóa PayOS trên Railway bị **cắt ngắn hoặc sai**.

## ✅ Giải pháp

### Bước 1: Kiểm tra và cập nhật các biến môi trường trên Railway

Vào Railway Dashboard → Project "MATT" → Tab **Variables**, cập nhật các giá trị sau:

#### ❌ Giá trị SAI hiện tại (từ ảnh bạn gửi):
```
PayOS_ClientId=9ca8c566-b2e8-4497-88fc-a5ad1          ← BỊ CẮT
PayOS_ApiKey=4209e4e9-a757-4104-ad73-d21d1            ← BỊ CẮT
PayOS_ChecksumKey=05a4aafcabab2416009875d0b95b92972ef3b5b55ab  ← SAI GIÁ TRỊ
```

#### ⚠️ Lưu ý về Tên Biến:

.NET Configuration đọc biến môi trường với **double underscore `__`** thành dấu `:`.

- Đúng: `PayOS__ClientId` (2 dấu gạch dưới) → Code đọc là `PayOS:ClientId` ✅
- Sai: `PayOS_ClientId` (1 dấu gạch dưới) → Code không đọc được ❌

**Kiểm tra trên Railway:**
- Nếu thấy `PayOS_ClientId` (1 dấu gạch) → Xóa và tạo lại với tên `PayOS__ClientId` (2 dấu gạch)
- Tương tự với `PayOS__ApiKey` và `PayOS__ChecksumKey`

#### ✅ Tên biến và giá trị ĐÚNG:

**Tên biến trên Railway (PHẢI có 2 dấu gạch dưới `__`):**
```
PayOS__ClientId=9ca8c566-b2e8-4497-88fc-a5ad18f477f8
PayOS__ApiKey=4209e4e9-a757-4104-ad73-d21d18e9037a
PayOS__ChecksumKey=05a4aafcabab2416009875d0b95b999f5faa6827a085
```

**⚠️ QUAN TRỌNG:** Phải dùng `PayOS__ClientId` (2 gạch dưới), KHÔNG phải `PayOS_ClientId` (1 gạch dưới)!

**⚠️ Lưu ý về ChecksumKey:**
- PayOS Dashboard có thể hiển thị ChecksumKey bị cắt
- Click vào icon **mắt** 👁️ bên cạnh ChecksumKey để xem đầy đủ
- Copy **TOÀN BỘ** giá trị, đảm bảo không bị cắt

### Bước 2: Cập nhật trên Railway

1. Mở Railway Dashboard: https://railway.app
2. Chọn project **MATT**
3. Vào tab **Variables**

4. **Kiểm tra tên biến:**
   - Tìm các biến `PayOS_ClientId`, `PayOS_ApiKey`, `PayOS_ChecksumKey` (1 gạch dưới)
   - Nếu có → **XÓA HẾT** các biến này (vì tên sai)
   - Đảm bảo có các biến với tên đúng: `PayOS__ClientId`, `PayOS__ApiKey`, `PayOS__ChecksumKey` (2 gạch dưới)

5. **Cập nhật/Create các biến:**

   **PayOS__ClientId (2 gạch dưới):**
   - Nếu chưa có → Click **"+ New Variable"** → Tên: `PayOS__ClientId`
   - Nếu đã có → Click vào biến `PayOS__ClientId`
   - Xóa giá trị cũ: `9ca8c566-b2e8-4497-88fc-a5ad1`
   - Nhập giá trị mới: `9ca8c566-b2e8-4497-88fc-a5ad18f477f8`
   - Lưu

   **PayOS__ApiKey (2 gạch dưới):**
   - Nếu chưa có → Click **"+ New Variable"** → Tên: `PayOS__ApiKey`
   - Nếu đã có → Click vào biến `PayOS__ApiKey`
   - Xóa giá trị cũ: `4209e4e9-a757-4104-ad73-d21d1`
   - Nhập giá trị mới: `4209e4e9-a757-4104-ad73-d21d18e9037a`
   - Lưu

   **PayOS__ChecksumKey (2 gạch dưới):**
   - Vào PayOS Dashboard → Click icon 👁️ để **hiển thị đầy đủ** ChecksumKey
   - Copy **TOÀN BỘ** giá trị ChecksumKey (có thể rất dài)
   - Nếu chưa có → Click **"+ New Variable"** → Tên: `PayOS__ChecksumKey`
   - Nếu đã có → Click vào biến `PayOS__ChecksumKey`
   - Xóa giá trị cũ
   - Paste giá trị đầy đủ từ PayOS Dashboard
   - Lưu

6. Railway sẽ tự động redeploy ứng dụng với các giá trị mới

### Bước 3: Kiểm tra lại

1. Đợi deployment hoàn tất (thường mất 1-2 phút)
2. Thử tạo thanh toán lại trên website
3. Lỗi signature sẽ được giải quyết

## 📋 Checklist

- [ ] Đã xóa các biến sai: `PayOS_ClientId`, `PayOS_ApiKey`, `PayOS_ChecksumKey` (1 gạch dưới)
- [ ] Đã tạo/cập nhật `PayOS__ClientId` (2 gạch dưới) với đầy đủ giá trị từ PayOS Dashboard
- [ ] Đã tạo/cập nhật `PayOS__ApiKey` (2 gạch dưới) với đầy đủ giá trị từ PayOS Dashboard  
- [ ] Đã tạo/cập nhật `PayOS__ChecksumKey` (2 gạch dưới) với giá trị chính xác từ PayOS Dashboard
- [ ] Đã verify giá trị không bị cắt (copy đầy đủ)
- [ ] Railway đã redeploy thành công
- [ ] Đã test lại chức năng thanh toán

## 🔍 Giải thích kỹ thuật

### Vì sao lỗi xảy ra?

1. **Client ID bị cắt**: `9ca8c566-b2e8-4497-88fc-a5ad1` (thiếu `8f477f8`)
2. **Api Key bị cắt**: `4209e4e9-a757-4104-ad73-d21d1` (thiếu `8e9037a`)
3. **Checksum Key sai**: Giá trị khác với PayOS Dashboard

Khi các khóa bị sai, PayOS không thể verify signature → trả về lỗi `code: 201`.

### Cách PayOS verify signature

1. PayOS nhận request với signature từ client
2. PayOS tự tính signature bằng cách:
   - Sắp xếp các tham số: `amount`, `cancelUrl`, `description`, `orderCode`, `returnUrl` (alphabetical)
   - URL-encode từng giá trị
   - Nối thành chuỗi: `amount=20000&cancelUrl=...&description=...&orderCode=...&returnUrl=...`
   - Tính HMAC-SHA256 với Checksum Key
3. So sánh signature client gửi với signature PayOS tính
4. Nếu khác → lỗi `code: 201`

### Code hiện tại đã đúng

Code trong `PaymentController.cs` đã implement đúng cách tính signature:
- Sắp xếp theo alphabet ✅
- URL-encode giá trị ✅
- HMAC-SHA256 với ChecksumKey ✅

**Vấn đề chỉ là khóa bị sai trên Railway!**

## 💡 Tips

1. **Copy đầy đủ**: Khi copy các khóa từ PayOS Dashboard, đảm bảo copy đầy đủ, không bị cắt
2. **Không có khoảng trắng**: Đảm bảo không có space ở đầu/cuối khi paste vào Railway
3. **Case sensitive**: Các khóa phân biệt hoa thường
4. **Webhook URL**: Nhớ cấu hình Webhook URL trong PayOS Dashboard nếu cần nhận thông báo thanh toán

## 📞 Nếu vẫn lỗi

Nếu sau khi cập nhật đúng các khóa mà vẫn lỗi:

1. Kiểm tra logs trên Railway để xem giá trị config có đúng không
2. Kiểm tra PayOS Dashboard xem có thay đổi gì không
3. Test với Postman/curl để verify signature calculation
4. Liên hệ PayOS support nếu cần

## 🔗 Tài liệu tham khảo

- PayOS API Documentation: https://payos.vn/docs/api/
- PayOS Dashboard: https://my.payos.vn
