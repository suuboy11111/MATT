# Hướng dẫn tải Font hỗ trợ tiếng Việt

## Vấn đề
PDF chứng nhận bị lỗi font tiếng Việt vì thiếu font file hỗ trợ Unicode.

## Giải pháp

### Cách 1: Tải tự động (Khuyến nghị)
Chạy các lệnh sau trong terminal:

**Windows (PowerShell):**
```powershell
Invoke-WebRequest -Uri "https://github.com/google/fonts/raw/main/ofl/notosans/NotoSans-Regular.ttf" -OutFile "wwwroot\fonts\NotoSans-Regular.ttf"
Invoke-WebRequest -Uri "https://github.com/google/fonts/raw/main/ofl/notosans/NotoSans-Bold.ttf" -OutFile "wwwroot\fonts\NotoSans-Bold.ttf"
```

**Linux/Mac:**
```bash
curl -L -o wwwroot/fonts/NotoSans-Regular.ttf "https://github.com/google/fonts/raw/main/ofl/notosans/NotoSans-Regular.ttf"
curl -L -o wwwroot/fonts/NotoSans-Bold.ttf "https://github.com/google/fonts/raw/main/ofl/notosans/NotoSans-Bold.ttf"
```

### Cách 2: Tải thủ công
1. Truy cập: https://fonts.google.com/noto/specimen/Noto+Sans
2. Click "Download family"
3. Giải nén file
4. Copy 2 file sau vào thư mục `wwwroot/fonts/`:
   - `NotoSans-Regular.ttf`
   - `NotoSans-Bold.ttf`

### Cách 3: Dùng font khác
Bạn có thể dùng bất kỳ font nào hỗ trợ tiếng Việt:
- Arial Unicode MS
- Times New Roman
- Roboto
- Open Sans

Đặt tên file là `arial.ttf` hoặc `times.ttf` và đặt vào `wwwroot/fonts/`

## Sau khi tải xong
1. Đảm bảo có 2 file: `NotoSans-Regular.ttf` và `NotoSans-Bold.ttf` trong `wwwroot/fonts/`
2. Restart ứng dụng
3. Test lại chức năng tạo chứng nhận

## Kiểm tra
Sau khi tải, thư mục `wwwroot/fonts/` nên có:
- ✅ NotoSans-Regular.ttf
- ✅ NotoSans-Bold.ttf
- README.md
- DOWNLOAD_FONTS.md
