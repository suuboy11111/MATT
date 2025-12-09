# 🐛 Hướng dẫn Debug Chatbox

## Vấn đề hiện tại

1. **Tin nhắn user không hiển thị**
2. **Gemini AI không hoạt động**

## 🔍 Các bước Debug

### Bước 1: Kiểm tra Console (Browser)

1. Mở website trên Railway
2. Nhấn **F12** để mở DevTools
3. Vào tab **Console**
4. Gửi một tin nhắn trong chatbox
5. Kiểm tra có lỗi JavaScript không

**Các lỗi thường gặp:**
- `chatMessages container not found` → JavaScript không tìm thấy element
- `Error adding message to UI` → Lỗi khi render message
- `Error sending message` → Lỗi khi gọi API

### Bước 2: Kiểm tra Network Tab

1. Vào tab **Network** trong DevTools
2. Gửi tin nhắn
3. Tìm request `/api/rulebot/ask`
4. Click vào request để xem:
   - **Status**: Phải là `200 OK`
   - **Response**: Xem JSON response có `reply` không

### Bước 3: Kiểm tra Server Logs (Railway)

1. Vào Railway Dashboard
2. Vào tab **Deployments** → Click vào deployment mới nhất
3. Vào tab **Logs**
4. Tìm các log sau khi gửi tin nhắn:

**Logs cần tìm:**
```
info: GeminiService initialized with model: gemini-1.5-flash-latest
info: GeminiService successfully injected
info: Gemini service available, attempting AI response
info: Calling Gemini API with prompt length: XXX
```

**Nếu không thấy logs này:**
- GeminiService chưa được inject → Kiểm tra `Program.cs`
- API key chưa được cấu hình → Kiểm tra Railway Variables

### Bước 4: Kiểm tra CSS (Elements Tab)

1. Vào tab **Elements** trong DevTools
2. Tìm element có class `message user`
3. Kiểm tra:
   - Element có tồn tại không?
   - CSS có được apply không?
   - Có bị `display: none` không?

**Kiểm tra CSS:**
```css
.message.user .message-content {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white !important;
}
```

## 🔧 Các lỗi thường gặp và cách sửa

### Lỗi 1: Tin nhắn user không hiển thị

**Nguyên nhân:**
- CSS variable `--primary-color` không được định nghĩa
- Element bị ẩn bởi CSS khác

**Đã sửa:**
- Thay `var(--primary-color)` bằng gradient cụ thể
- Thêm `!important` cho color

**Kiểm tra:**
- Mở Elements tab → Tìm `.message.user`
- Xem computed styles

### Lỗi 2: Gemini API trả về 404

**Nguyên nhân:**
- Model name sai: `gemini-1.5-flash` thay vì `gemini-1.5-flash-latest`

**Đã sửa:**
- Đổi model name thành `gemini-1.5-flash-latest`

**Cần làm:**
- Cập nhật Railway Variable: `GeminiApi_Model=gemini-1.5-flash-latest`
- Redeploy

### Lỗi 3: Gemini không được gọi

**Nguyên nhân:**
- API key chưa được cấu hình
- GeminiService không được inject

**Kiểm tra:**
1. Railway Variables có `GeminiApi_ApiKey` không?
2. Logs có hiển thị "GeminiService successfully injected" không?

## 📋 Checklist Debug

- [ ] Console không có lỗi JavaScript
- [ ] Network request `/api/rulebot/ask` trả về 200
- [ ] Response có field `reply`
- [ ] Server logs có "GeminiService initialized"
- [ ] Server logs có "Gemini service available"
- [ ] Element `.message.user` tồn tại trong DOM
- [ ] CSS cho user message được apply
- [ ] Railway Variables có `GeminiApi_ApiKey` và `GeminiApi_Model`

## 🚀 Sau khi sửa

1. **Commit và push code mới**
2. **Cập nhật Railway Variables:**
   ```
   GeminiApi_ApiKey=YOUR_API_KEY
   GeminiApi_Model=gemini-1.5-flash-latest
   ```
3. **Redeploy trên Railway**
4. **Test lại chatbox**
5. **Kiểm tra logs để xác nhận Gemini hoạt động**

## 💡 Tips

- Luôn mở Console khi test để thấy lỗi ngay
- Kiểm tra Network tab để xem API response
- Xem server logs để debug backend
- Dùng Elements tab để inspect CSS

