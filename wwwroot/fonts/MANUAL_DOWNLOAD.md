# Hướng dẫn tải Font thủ công

Nếu script tự động không hoạt động, vui lòng tải font thủ công:

## Cách 1: Tải từ Google Fonts (Khuyến nghị)

1. Truy cập: https://fonts.google.com/noto/specimen/Noto+Sans
2. Click nút **"Download family"** (góc trên bên phải)
3. Giải nén file ZIP
4. Copy 2 file sau vào thư mục `wwwroot/fonts/`:
   - `NotoSans-Regular.ttf`
   - `NotoSans-Bold.ttf`

## Cách 2: Tải từ GitHub (Direct Download)

1. **NotoSans-Regular.ttf:**
   - URL: https://github.com/google/fonts/raw/main/ofl/notosans/NotoSans-Regular.ttf
   - Click "Download" hoặc "Raw"
   - Lưu vào `wwwroot/fonts/NotoSans-Regular.ttf`

2. **NotoSans-Bold.ttf:**
   - URL: https://github.com/google/fonts/raw/main/ofl/notosans/NotoSans-Bold.ttf
   - Click "Download" hoặc "Raw"
   - Lưu vào `wwwroot/fonts/NotoSans-Bold.ttf`

## Cách 3: Dùng font khác hỗ trợ tiếng Việt

Bạn có thể dùng bất kỳ font nào hỗ trợ Unicode:
- **Arial Unicode MS** (nếu có trên Windows)
- **Times New Roman** (thường có sẵn)
- **Roboto** (từ Google Fonts)
- **Open Sans** (từ Google Fonts)

Đặt tên file là `arial.ttf` hoặc `times.ttf` và đặt vào `wwwroot/fonts/`

## Kiểm tra sau khi tải

Sau khi tải, thư mục `wwwroot/fonts/` nên có:
- ✅ `NotoSans-Regular.ttf` (khoảng 500KB - 1MB)
- ✅ `NotoSans-Bold.ttf` (khoảng 500KB - 1MB)

## Sau khi có font

1. Restart ứng dụng
2. Test lại chức năng tạo chứng nhận
3. Font tiếng Việt sẽ hiển thị đúng (không còn mất dấu)
