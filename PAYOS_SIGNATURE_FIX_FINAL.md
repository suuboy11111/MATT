# ✅ Sửa lỗi PayOS Signature - BỎ items khỏi signature string

## 🎯 Vấn đề chính

**PayOS v2 KHÔNG bao gồm `items` trong signature string!**

Signature chỉ bao gồm 5 fields:
- `amount`
- `cancelUrl`
- `description`
- `orderCode`
- `returnUrl`

**Items chỉ được gửi trong request body, KHÔNG có trong signature calculation.**

## ✅ Giải pháp đã áp dụng

### Trước (SAI):
```csharp
var signatureString = $"amount={amountStr}&cancelUrl={cancelUrl}&description={paymentDescription}&items={itemsJson}&returnUrl={returnUrl}";
```

### Sau (ĐÚNG):
```csharp
var signatureString = $"amount={amountStr}&cancelUrl={cancelUrl}&description={paymentDescription}&orderCode={orderCode}&returnUrl={returnUrl}";
```

**Thay đổi**:
- ❌ Bỏ `items` khỏi signature string
- ✅ Thêm `orderCode` vào signature string
- ✅ Items vẫn được gửi trong request body (nhưng không tính vào signature)

## 📋 Signature String Format

**Đúng:**
```
amount=20000&cancelUrl=https://matt-production.up.railway.app/Payment/Cancel&description=Ủng hộ tài chính - Người hỗ trợ&orderCode=1765201242&returnUrl=https://matt-production.up.railway.app/Payment/Success?orderCode=1765201242
```

**Sai (trước đây):**
```
amount=20000&cancelUrl=...&description=...&items=[{"name":"Ủng hộ tài chính","price":20000,"quantity":1}]&returnUrl=...
```

## 🔍 Debug Logs

Code đã được thêm logging để debug:
- ChecksumKey length (không log full vì bảo mật)
- Signature string đầy đủ
- Calculated signature (20 ký tự đầu)

## ✅ Checklist

- [x] Bỏ `items` khỏi signature string
- [x] Thêm `orderCode` vào signature string
- [x] Items vẫn được gửi trong request body
- [x] Không URL encode trong signature string (raw values)
- [x] Thêm logging để debug

## 🧪 Test lại

1. **Deploy code mới** lên Railway
2. **Test** tại: `https://matt-production.up.railway.app/Payment/TestPayOS`
3. **Kiểm tra logs**:
   ```
   PayOS Signature String: amount=20000&cancelUrl=https://...&description=Ủng hộ tài chính...&orderCode=...&returnUrl=https://...
   ```
   - ✅ Không có `items` trong signature string
   - ✅ Có `orderCode` trong signature string
4. **Kỳ vọng**: Response sẽ là `{"code":"00","data":{"checkoutUrl":"..."}}` ✅

## 📝 Tham khảo

Theo PayOS v2 API documentation:
- Items array được gửi trong request body
- Items **KHÔNG** được tính vào signature
- Signature chỉ bao gồm: amount, cancelUrl, description, orderCode, returnUrl

## 🚀 Kết quả mong đợi

Sau khi deploy:
- ✅ Signature string đúng format PayOS v2
- ✅ Không có items trong signature
- ✅ Có orderCode trong signature
- ✅ Payment link được tạo thành công
- ✅ Redirect đến PayOS checkout page

Deploy và test ngay! 🎉
