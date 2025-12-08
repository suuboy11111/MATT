# ✅ Đã sửa lỗi PayOS Signature!

## 🐛 Nguyên nhân lỗi

Lỗi `{"code":"201","desc":"Mã kiểm tra(signature) không hợp lệ"}` xảy ra vì **code tính signature SAI**.

### Vấn đề cụ thể:

Theo tài liệu PayOS v2 chính thức, khi tính signature cho payment request, chỉ cần các tham số sau (sắp xếp alphabetical):

1. ✅ `amount`
2. ✅ `cancelUrl`
3. ✅ `description`
4. ✅ `returnUrl`

**❌ `orderCode` KHÔNG được include trong signature calculation!**

### Code cũ (SAI):
```csharp
var signDict = new Dictionary<string, string>
{
    { "amount", ... },
    { "cancelUrl", ... },
    { "description", ... },
    { "orderCode", orderCode.ToString() },  // ← SAI! Không nên có đây!
    { "returnUrl", ... }
};
```

### Code mới (ĐÚNG):
```csharp
var signDict = new Dictionary<string, string>
{
    { "amount", ... },
    { "cancelUrl", ... },
    { "description", ... },
    { "returnUrl", ... }
    // orderCode KHÔNG được thêm vào!
};
```

## ✅ Giải pháp đã áp dụng

1. ✅ Loại bỏ `orderCode` khỏi signature calculation
2. ✅ Thêm logging để debug signature string
3. ✅ Đảm bảo BaseUrl luôn dùng HTTPS

## 📋 Các bước kiểm tra

1. **Deploy code mới lên Railway**
2. **Xem logs** để verify signature string:
   - Tìm dòng: `PayOS Signature String: ...`
   - String này sẽ không còn chứa `orderCode=...`
3. **Test lại** chức năng thanh toán

## 🔍 Logs để kiểm tra

Sau khi deploy, trong logs bạn sẽ thấy:
```
warn: PayOS Signature String: amount=20000&cancelUrl=https://...&description=...&returnUrl=https://...
```

Lưu ý: String này **KHÔNG** còn chứa `orderCode` nữa!

## ✅ Kết quả mong đợi

Sau khi sửa và deploy:
- PayOS sẽ accept signature
- Payment link sẽ được tạo thành công
- User có thể thanh toán được

## 📚 Tham khảo

- PayOS v2 API Documentation: https://payos.vn/docs/api/
- Signature chỉ include: `amount`, `cancelUrl`, `description`, `returnUrl`
- `orderCode` chỉ được gửi trong request body, KHÔNG trong signature
