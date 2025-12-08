# 🔧 Sửa lỗi PayOS Signature - "Mã kiểm tra không hợp lệ"

## 🐛 Vấn đề

Lỗi `{"code":"201","desc":"Mã kiểm tra(signature) không hợp lệ"}` khi tạo payment link.

**Log lỗi:**
```
PayOS Signature String: amount=20000&cancelUrl=https://matt-production.up.railway.app/Payment/Cancel&description=Ủng hộ tài chính - Người hỗ trợ&items=[{"name":"Ủnghộtàichính","quantity":1,"price":20000}]&returnUrl=https://matt-production.up.railway.app/Payment/Success?orderCode=1765200467
PayOS Response: {"code":"201","desc":"Mã kiểm tra(signature) không hợp lệ","data":null}
```

## 🔍 Nguyên nhân

1. **Items JSON keys không được sort**: PayOS yêu cầu keys trong items object phải được sort alphabetical (name, price, quantity)
2. **Thiếu URL encoding**: PayOS v2 yêu cầu URL encode các giá trị trong signature string (trừ items JSON)

## ✅ Giải pháp đã áp dụng

### 1. Sử dụng SortedDictionary cho items
```csharp
var itemsArray = new[]
{
    new SortedDictionary<string, object>
    {
        { "name", "Ủng hộ tài chính" },
        { "price", (int)request.Amount },
        { "quantity", 1 }
    }
};
```

**Lợi ích**: Đảm bảo keys luôn được sort alphabetical: `name`, `price`, `quantity`

### 2. URL encode các giá trị trong signature string
```csharp
var encodedCancelUrl = Uri.EscapeDataString(cancelUrl);
var encodedDescription = Uri.EscapeDataString(paymentDescription);
var encodedReturnUrl = Uri.EscapeDataString(returnUrl);

var signatureString = $"amount={amountStr}&cancelUrl={encodedCancelUrl}&description={encodedDescription}&items={itemsJson}&returnUrl={encodedReturnUrl}";
```

**Lưu ý**: 
- Items JSON **KHÔNG** được URL encode (vì đã là JSON string)
- Các giá trị khác (cancelUrl, description, returnUrl) **PHẢI** URL encode

## 📋 Checklist kiểm tra

### Trên Railway:
- [x] `PayOS__ClientId` (2 gạch dưới) = `9ca8c566-b2e8-4497-88fc-a5ad18f477f8`
- [x] `PayOS__ApiKey` (2 gạch dưới) = `4209e4e9-a757-4104-ad73-d21d18e9037a`
- [x] `PayOS__ChecksumKey` (2 gạch dưới) = `05a4aafcabab2416009875d0b95b999f5faa6827a08562b2fa2972ef3b5b55ab`

### Code đã được sửa:
- [x] Items sử dụng SortedDictionary để sort keys
- [x] URL encode cancelUrl, description, returnUrl trong signature string
- [x] Items JSON không được URL encode

## 🧪 Test lại

1. **Deploy code mới** lên Railway
2. **Test** tại: `https://matt-production.up.railway.app/Payment/TestPayOS`
3. **Kiểm tra logs** để xem signature string mới:
   ```
   PayOS Signature String: amount=20000&cancelUrl=https%3A%2F%2F...&description=%E1%BB%A6ng%20h%E1%BB%99%20t%C3%A0i%20ch%C3%ADnh...&items=[{"name":"Ủng hộ tài chính","price":20000,"quantity":1}]&returnUrl=https%3A%2F%2F...
   ```
4. **Kỳ vọng**: Response phải là `{"code":"00","data":{"checkoutUrl":"..."}}` thay vì `{"code":"201",...}`

## 🔍 Debug nếu vẫn lỗi

### Kiểm tra signature string trong logs:
1. Items JSON phải có keys sort: `name`, `price`, `quantity` (alphabetical)
2. cancelUrl, description, returnUrl phải được URL encode
3. Items JSON **KHÔNG** được URL encode

### Kiểm tra ChecksumKey:
1. Đảm bảo ChecksumKey trên Railway đầy đủ và chính xác
2. Không có space ở đầu/cuối
3. Copy đầy đủ từ PayOS Dashboard (click icon 👁️ để xem)

## 📝 Thay đổi code

**File**: `Controllers/PaymentController.cs`

**Thay đổi chính**:
1. Sử dụng `SortedDictionary<string, object>` thay vì anonymous object cho items
2. Thêm URL encoding cho cancelUrl, description, returnUrl trong signature string
3. Giữ nguyên items JSON không encode

## ✅ Kết quả mong đợi

Sau khi deploy code mới:
- ✅ Signature string đúng format PayOS v2 yêu cầu
- ✅ Items JSON có keys sort alphabetical
- ✅ Các giá trị được URL encode đúng cách
- ✅ Payment link được tạo thành công
- ✅ Redirect đến PayOS checkout page

## 🚀 Next Steps

1. **Deploy code** lên Railway
2. **Test** tại `/Payment/TestPayOS`
3. **Kiểm tra logs** để verify signature string
4. **Test thanh toán** với số tiền nhỏ (10,000 VNĐ)
5. **Verify** transaction được lưu vào database

Nếu vẫn lỗi, hãy gửi log mới để tiếp tục debug! 🔍
