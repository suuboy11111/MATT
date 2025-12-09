# 🔧 Hướng dẫn sửa lỗi redirect_uri_mismatch

## ❌ Lỗi hiện tại

```
Error 400: redirect_uri_mismatch
redirect_uri=http://matt-production.up.railway.app/Account/GoogleCallback
```

## ✅ Giải pháp

### Bước 1: Kiểm tra Google Cloud Console

1. Vào [Google Cloud Console](https://console.cloud.google.com/)
2. Chọn project của bạn
3. **APIs & Services** → **Credentials**
4. Click vào OAuth 2.0 Client ID của bạn
5. Kiểm tra **Authorized redirect URIs**

### Bước 2: Thêm cả HTTP và HTTPS (Tạm thời)

**QUAN TRỌNG:** Thêm CẢ HAI redirect URIs vào Google Cloud Console:

```
https://matt-production.up.railway.app/Account/GoogleCallback
http://matt-production.up.railway.app/Account/GoogleCallback
```

**Lý do:** 
- Code đã được sửa để luôn dùng HTTPS trong production
- Nhưng nếu Google Cloud Console chỉ có HTTPS, và code vẫn gửi HTTP (do bug), sẽ bị mismatch
- Thêm cả hai để đảm bảo không bị lỗi trong quá trình chuyển đổi

### Bước 3: Sau khi code đã deploy và hoạt động

Sau khi đảm bảo code luôn dùng HTTPS (kiểm tra logs), bạn có thể:
1. Xóa redirect URI HTTP khỏi Google Cloud Console
2. Chỉ giữ lại HTTPS: `https://matt-production.up.railway.app/Account/GoogleCallback`

### Bước 4: Kiểm tra logs sau khi deploy

Sau khi deploy code mới, kiểm tra Railway logs:

```
🔐 Google OAuth redirect URI: https://matt-production.up.railway.app/Account/GoogleCallback
   - Scheme: https (Request.Scheme: http)
   - Host: matt-production.up.railway.app
   - IsProduction: True
```

**Phải thấy:**
- ✅ Redirect URI bắt đầu bằng `https://`
- ✅ `IsProduction: True`
- ✅ `Scheme: https`

## 🚨 Lưu ý quan trọng

1. **Google chỉ chấp nhận HTTPS cho production domains**
   - `railway.app` domains PHẢI dùng HTTPS
   - HTTP chỉ dùng được cho `localhost` trong development

2. **Thời gian cập nhật**
   - Sau khi thêm redirect URI trong Google Cloud Console
   - Đợi **5-10 phút** để Google cập nhật
   - Sau đó thử đăng nhập lại

3. **Format redirect URI**
   - ✅ Đúng: `https://matt-production.up.railway.app/Account/GoogleCallback`
   - ❌ Sai: `https://matt-production.up.railway.app/Account/GoogleCallback/` (có trailing slash)
   - ❌ Sai: `http://matt-production.up.railway.app/Account/GoogleCallback` (HTTP cho production)

## 📝 Checklist

- [ ] Đã thêm `https://matt-production.up.railway.app/Account/GoogleCallback` vào Google Cloud Console
- [ ] Đã thêm `http://matt-production.up.railway.app/Account/GoogleCallback` vào Google Cloud Console (tạm thời)
- [ ] Đã click **Save** trong Google Cloud Console
- [ ] Đã đợi 5-10 phút
- [ ] Đã deploy code mới lên Railway
- [ ] Đã kiểm tra logs và thấy redirect URI là HTTPS
- [ ] Đã test đăng nhập bằng Google
- [ ] Nếu thành công, đã xóa redirect URI HTTP khỏi Google Cloud Console






