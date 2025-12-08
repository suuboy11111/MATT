# 🔄 Cập nhật lại Transaction cũ

## ❓ Câu hỏi

**Các transaction cũ (đã thanh toán trước đó) có được tự động cập nhật quỹ tài trợ và VinhDanh không?**

## 📋 Trả lời

### Logic hiện tại:
- ✅ **Transaction mới**: Tự động cập nhật khi status chuyển từ "Pending" → "Success"
- ❌ **Transaction cũ**: KHÔNG tự động cập nhật (vì đã có status = "Success" rồi)

### Giải pháp:
Đã tạo endpoint admin để **sync lại tất cả transaction cũ một lần**:

**Endpoint**: `POST /Admin/SyncOldTransactions`

## 🔧 Cách sử dụng

### Option 1: Gọi trực tiếp qua URL (sau khi đăng nhập admin)
```
https://your-domain.railway.app/Admin/SyncOldTransactions
```

**Lưu ý**: Cần đăng nhập với role Admin và có CSRF token.

### Option 2: Tạo button trong Admin Dashboard (khuyến nghị)

Tôi có thể tạo một view với button để admin click và sync.

## 📊 Logic Sync

### 1. Tính lại Quỹ tài trợ (Fund)
- Tính tổng tất cả transaction có `Status = "Success"` cho mỗi MaiAm
- Set `MaiAm.Fund = Tổng transaction Success`

**Ví dụ:**
- MaiAm có 5 transaction Success: 100k, 200k, 150k, 300k, 250k
- Fund mới = 1,000,000 VNĐ

### 2. Tạo/Cập nhật VinhDanh
- Duyệt qua tất cả transaction Success
- Tạo/cập nhật VinhDanh cho mỗi transaction
- Tránh duplicate bằng cách check ngày và tên

## ⚠️ Lưu ý quan trọng

### Vấn đề Duplicate:
- Nếu Fund đã được tính thủ công trước đó → Sync sẽ tính lại từ đầu
- **Giải pháp**: Sync sẽ **tính lại từ đầu** dựa trên tất cả transaction Success

### Cách hoạt động:
1. **Reset Fund** về 0 (hoặc giá trị ban đầu)
2. **Tính lại** từ tất cả transaction Success
3. **Tạo/cập nhật VinhDanh** cho tất cả transaction

## 🧪 Test

### Trước khi sync:
- Quỹ tài trợ: 20,000,000 VNĐ (có thể đã tính thủ công)
- Transaction cũ: 3 transaction Success (200k, 150k, 100k) = 450k
- VinhDanh: Chưa có hoặc thiếu

### Sau khi sync:
- Quỹ tài trợ: 20,450,000 VNĐ (tính lại từ transaction)
- VinhDanh: Đã tạo/cập nhật cho 3 transaction

## 🚀 Cách chạy Sync

### Bước 1: Đăng nhập với role Admin

### Bước 2: Gọi endpoint
Có thể:
1. Tạo button trong Admin Dashboard (tôi có thể làm)
2. Hoặc gọi trực tiếp qua browser console:
```javascript
fetch('/Admin/SyncOldTransactions', {
    method: 'POST',
    headers: {
        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
    }
})
```

### Bước 3: Kiểm tra kết quả
- Xem TempData message
- Kiểm tra Fund trong database
- Kiểm tra VinhDanh trong database

## 💡 Khuyến nghị

**Nên chạy sync một lần** sau khi deploy code mới để:
- ✅ Cập nhật quỹ tài trợ cho transaction cũ
- ✅ Tạo VinhDanh cho transaction cũ
- ✅ Đảm bảo dữ liệu đồng bộ

**Sau đó**: Transaction mới sẽ tự động cập nhật khi thanh toán thành công.

## 🔄 Tự động vs Thủ công

### Tự động (Transaction mới):
- ✅ Khi thanh toán thành công → Tự động cập nhật Fund và VinhDanh

### Thủ công (Transaction cũ):
- ✅ Chạy sync một lần để cập nhật lại tất cả transaction cũ

Bạn muốn tôi tạo button trong Admin Dashboard để dễ sync không? 🚀
