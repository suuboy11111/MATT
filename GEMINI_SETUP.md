# 🤖 Hướng dẫn Cấu hình Google Gemini API

## 📋 Tổng quan

Google Gemini API có **tier miễn phí** với giới hạn:
- **Gemini 1.5 Flash**: 15 requests/phút, 1 triệu tokens/ngày (MIỄN PHÍ)
- **Gemini 1.5 Pro**: 2 requests/phút, 50 requests/ngày (MIỄN PHÍ)

Đủ dùng cho chatbox của bạn! 🎉

## 🔑 Bước 1: Lấy API Key từ Google AI Studio

1. Truy cập [Google AI Studio](https://aistudio.google.com/)
2. Đăng nhập bằng tài khoản Google của bạn (tài khoản Gemini Pro)
3. Click vào **"Get API Key"** hoặc vào [API Keys page](https://aistudio.google.com/app/apikey)
4. Click **"Create API Key"**
5. Chọn project hoặc tạo project mới
6. **Copy API Key** (sẽ hiển thị dạng: `AIzaSy...`)

⚠️ **Lưu ý**: Giữ bí mật API key, không commit vào Git!

## ⚙️ Bước 2: Cấu hình trong dự án

### Cách 1: Cấu hình Local (Development)

Mở file `appsettings.Development.json` và thêm:

```json
{
  "GeminiApi": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "Model": "gemini-1.5-flash-latest"
  }
}
```

**Hoặc** dùng `gemini-1.5-pro-latest` nếu bạn muốn dùng Pro:

```json
{
  "GeminiApi": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "Model": "gemini-1.5-pro-latest"
  }
}
```

⚠️ **Lưu ý**: Phải dùng `-latest` suffix cho model name (ví dụ: `gemini-1.5-flash-latest` thay vì `gemini-1.5-flash`)

### Cách 2: Cấu hình Production (Railway)

1. Vào Railway Dashboard → Project → Web Service
2. Vào tab **"Variables"**
3. Thêm các biến sau:

```
GeminiApi_ApiKey=YOUR_API_KEY_HERE
GeminiApi_Model=gemini-1.5-flash-latest
```

**Lưu ý**: 
- Railway hỗ trợ cả `_` (single underscore) và `__` (double underscore)
- Cả hai đều được ASP.NET Core tự động convert thành `GeminiApi:ApiKey`
- Format hiển thị trong Railway: `GeminiApi_ApiKey` hoặc `GeminiApi__ApiKey` đều OK

## 🎯 Bước 3: Chọn Model

### Gemini 1.5 Flash (Khuyến nghị)
- ✅ **Nhanh hơn** (phản hồi trong 1-2 giây)
- ✅ **Miễn phí tốt** (15 req/phút, 1M tokens/ngày)
- ✅ **Đủ dùng** cho chatbox thông thường
- Cấu hình: `"Model": "gemini-1.5-flash-latest"`

### Gemini 1.5 Pro
- ✅ **Thông minh hơn** (xử lý câu hỏi phức tạp tốt hơn)
- ⚠️ **Chậm hơn** (3-5 giây)
- ⚠️ **Giới hạn thấp hơn** (2 req/phút, 50 req/ngày)
- Cấu hình: `"Model": "gemini-1.5-pro-latest"`

**Khuyến nghị**: Dùng **Flash** cho chatbox vì nhanh và đủ dùng.

## ✅ Bước 4: Kiểm tra

1. Chạy ứng dụng: `dotnet run`
2. Mở chatbox trên website
3. Gửi một câu hỏi không có trong rules (ví dụ: "Bạn có thể giới thiệu về tổ chức không?")
4. Nếu Gemini hoạt động, bạn sẽ nhận được câu trả lời thông minh từ AI!

## 🔍 Troubleshooting

### Lỗi: "Gemini API key not configured"
- Kiểm tra lại API key trong `appsettings.Development.json`
- Đảm bảo không có khoảng trắng thừa

### Lỗi: "API error: 400"
- Kiểm tra format API key (phải bắt đầu bằng `AIzaSy`)
- Kiểm tra model name (phải là `gemini-1.5-flash` hoặc `gemini-1.5-pro`)

### Lỗi: "API error: 429" (Rate limit)
- Bạn đã vượt quá giới hạn miễn phí
- Đợi một chút rồi thử lại
- Hoặc nâng cấp lên paid tier

### Chatbox không dùng Gemini
- Kiểm tra logs trong console
- Nếu không có API key, chatbox sẽ tự động fallback về rule-based (vẫn hoạt động bình thường)

## 💡 Tips

1. **Bảo mật**: Không commit API key vào Git. Thêm vào `.gitignore` nếu cần.
2. **Testing**: Test với câu hỏi không có trong rules để kiểm tra Gemini
3. **Monitoring**: Xem logs để theo dõi việc sử dụng API
4. **Fallback**: Nếu Gemini lỗi, chatbox tự động dùng rule-based system

## 📚 Tài liệu tham khảo

- [Google Gemini API Documentation](https://ai.google.dev/docs)
- [Google AI Studio](https://aistudio.google.com/)
- [Pricing & Limits](https://ai.google.dev/pricing)

---

**Chúc bạn tích hợp thành công! 🚀**

