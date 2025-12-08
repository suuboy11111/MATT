# ✅ Hướng dẫn hoàn thiện PayOS Integration

## 📋 Checklist cấu hình

### ✅ Bước 1: Kiểm tra Railway Environment Variables

Đảm bảo trên Railway có các biến sau với **2 dấu gạch dưới `__`**:

```
PayOS__ClientId=9ca8c566-b2e8-4497-88fc-a5ad18f477f8
PayOS__ApiKey=4209e4e9-a757-4104-ad73-d21d18e9037a
PayOS__ChecksumKey=05a4aafcabab2416009875d0b95b999f5faa6827a08562b2fa2972ef3b5b55ab
PayOS__Endpoint=https://api-merchant.payos.vn
```

**⚠️ QUAN TRỌNG**: 
- Phải dùng `PayOS__ClientId` (2 gạch dưới), KHÔNG phải `PayOS_ClientId` (1 gạch)
- ChecksumKey phải đầy đủ, không bị cắt

### ✅ Bước 2: Cấu hình Webhook URL trong PayOS Dashboard

1. Vào https://my.payos.vn
2. Vào phần **"Thông tin cấu hình"** (Configuration Information)
3. Tìm mục **"Webhook url"**
4. Nhập URL webhook của bạn:
   ```
   https://your-domain.railway.app/Payment/Webhook
   ```
   Hoặc nếu bạn đã có domain riêng:
   ```
   https://yourdomain.com/Payment/Webhook
   ```
5. Click **"Lưu"** (Save)

**Lưu ý**:
- Webhook URL phải là HTTPS
- Webhook URL phải accessible từ internet (không phải localhost)
- PayOS sẽ gửi webhook khi thanh toán thành công

### ✅ Bước 3: Kiểm tra Code đã được cập nhật

Code đã được cải thiện với:
- ✅ Webhook signature verification đầy đủ
- ✅ Validation cho request
- ✅ Error handling tốt hơn
- ✅ Logging chi tiết

### ✅ Bước 4: Test PayOS Integration

#### Test 1: Tạo Payment Link
1. Truy cập: `https://your-domain.railway.app/Payment/TestPayOS`
2. Hoặc vào trang donate và điền form
3. Kiểm tra xem có tạo được payment link không

#### Test 2: Thanh toán thử
1. Click vào payment link
2. Thanh toán với số tiền nhỏ (ví dụ: 10,000 VNĐ)
3. Kiểm tra xem có redirect về trang Success không

#### Test 3: Kiểm tra Webhook
1. Sau khi thanh toán thành công, kiểm tra logs trên Railway
2. Tìm log: `PayOS Webhook: Signature verified successfully`
3. Kiểm tra database xem transaction có được cập nhật status = "Success" không

## 🔍 Debug nếu có lỗi

### Lỗi: "Mã kiểm tra (signature) không hợp lệ"

**Nguyên nhân**: ChecksumKey sai hoặc bị cắt

**Giải pháp**:
1. Kiểm tra ChecksumKey trên Railway có đầy đủ không
2. Copy lại ChecksumKey từ PayOS Dashboard (click icon 👁️ để xem đầy đủ)
3. Đảm bảo không có space ở đầu/cuối
4. Redeploy trên Railway

### Lỗi: "PayOS chưa được cấu hình"

**Nguyên nhân**: Thiếu biến môi trường hoặc tên biến sai

**Giải pháp**:
1. Kiểm tra Railway Variables có đủ 3 biến không:
   - `PayOS__ClientId` (2 gạch dưới)
   - `PayOS__ApiKey` (2 gạch dưới)
   - `PayOS__ChecksumKey` (2 gạch dưới)
2. Xóa các biến cũ có 1 gạch dưới nếu có
3. Redeploy

### Lỗi: Webhook không nhận được

**Nguyên nhân**: Webhook URL chưa được cấu hình hoặc không accessible

**Giải pháp**:
1. Kiểm tra Webhook URL trong PayOS Dashboard đã được set chưa
2. Đảm bảo URL là HTTPS
3. Test webhook URL bằng cách truy cập trực tiếp (sẽ trả về BadRequest, đó là bình thường)
4. Kiểm tra logs trên Railway để xem có request đến webhook không

### Lỗi: Transaction không được cập nhật

**Nguyên nhân**: Webhook signature verification fail hoặc không tìm thấy transaction

**Giải pháp**:
1. Kiểm tra logs để xem webhook có được gọi không
2. Kiểm tra signature verification có pass không
3. Kiểm tra orderCode trong webhook có match với orderCode trong database không

## 📊 Kiểm tra Logs

Trên Railway, vào tab **Logs** và tìm các log sau:

### Log thành công khi tạo payment link:
```
PayOS - Creating payment link. BaseUrl: https://..., Amount: 20000, OrderCode: 1234567890
PayOS Signature String: amount=20000&cancelUrl=...&description=...&items=...&returnUrl=...
PayOS Response: {"code":"00","data":{"checkoutUrl":"https://..."}}
PayOS - Payment link created successfully. OrderCode: 1234567890, CheckoutUrl: https://...
```

### Log thành công khi nhận webhook:
```
PayOS Webhook received: {"code":"00","data":{...},"signature":"..."}
PayOS Webhook: Signature verified successfully
PayOS Webhook: Processing orderCode 1234567890, status: PAID
✅ Đã cập nhật transaction 123 thành công cho orderCode 1234567890
```

## 🎯 Các tính năng đã được implement

1. ✅ **Create Payment Link**: Tạo link thanh toán PayOS
2. ✅ **Payment Success Handler**: Xử lý khi user quay lại sau thanh toán
3. ✅ **Payment Cancel Handler**: Xử lý khi user hủy thanh toán
4. ✅ **Webhook Handler**: Nhận và xử lý webhook từ PayOS
5. ✅ **Webhook Signature Verification**: Verify signature để đảm bảo webhook hợp lệ
6. ✅ **Transaction Tracking**: Lưu và cập nhật transaction trong database
7. ✅ **Error Handling**: Xử lý lỗi và logging chi tiết
8. ✅ **Validation**: Validate request trước khi tạo payment link

## 🔐 Security Best Practices

1. ✅ **Signature Verification**: Webhook signature được verify để đảm bảo request từ PayOS
2. ✅ **HTTPS Only**: Webhook URL phải là HTTPS
3. ✅ **ChecksumKey**: Được lưu trong environment variables, không hardcode
4. ✅ **Logging**: Log chi tiết để debug nhưng không log sensitive data

## 📞 Support

Nếu vẫn gặp vấn đề:
1. Kiểm tra logs trên Railway
2. Kiểm tra PayOS Dashboard xem có lỗi gì không
3. Test với Postman/curl để verify API calls
4. Liên hệ PayOS support nếu cần

## ✅ Kết luận

PayOS integration đã được hoàn thiện với:
- ✅ Code đầy đủ và robust
- ✅ Webhook verification đầy đủ
- ✅ Error handling tốt
- ✅ Logging chi tiết
- ✅ Validation đầy đủ

Chỉ cần đảm bảo:
1. ✅ Railway environment variables đúng (2 gạch dưới)
2. ✅ Webhook URL được cấu hình trong PayOS Dashboard
3. ✅ ChecksumKey đầy đủ và chính xác

Sau đó test và deploy! 🚀
