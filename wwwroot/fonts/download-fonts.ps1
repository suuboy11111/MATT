# Script tải font Noto Sans hỗ trợ tiếng Việt
# Chạy script này trong PowerShell: .\download-fonts.ps1

Write-Host "Đang tải font Noto Sans..." -ForegroundColor Yellow

# Tạo thư mục nếu chưa có
$fontDir = "wwwroot\fonts"
if (-not (Test-Path $fontDir)) {
    New-Item -ItemType Directory -Path $fontDir -Force
    Write-Host "Đã tạo thư mục $fontDir" -ForegroundColor Green
}

# URLs thay thế để tải font
$fonts = @{
    "NotoSans-Regular.ttf" = @(
        "https://github.com/google/fonts/raw/main/ofl/notosans/NotoSans-Regular.ttf",
        "https://raw.githubusercontent.com/google/fonts/main/ofl/notosans/NotoSans-Regular.ttf",
        "https://fonts.gstatic.com/s/notosans/v36/o-0IIpQlx3QUlC5A4PNb4j5Ba_2c7A.woff2"
    )
    "NotoSans-Bold.ttf" = @(
        "https://github.com/google/fonts/raw/main/ofl/notosans/NotoSans-Bold.ttf",
        "https://raw.githubusercontent.com/google/fonts/main/ofl/notosans/NotoSans-Bold.ttf",
        "https://fonts.gstatic.com/s/notosans/v36/o-0NIpQlx3QUlC5A4PNjXhFlY9aA.woff2"
    )
}

foreach ($fontName in $fonts.Keys) {
    $fontPath = Join-Path $fontDir $fontName
    $urls = $fonts[$fontName]
    $success = $false
    
    Write-Host "`nĐang tải $fontName..." -ForegroundColor Cyan
    
    foreach ($url in $urls) {
        try {
            Write-Host "  Thử URL: $url" -ForegroundColor Gray
            Invoke-WebRequest -Uri $url -OutFile $fontPath -TimeoutSec 30 -ErrorAction Stop
            Write-Host "  ✅ Đã tải thành công $fontName" -ForegroundColor Green
            $success = $true
            break
        }
        catch {
            Write-Host "  ❌ Lỗi: $($_.Exception.Message)" -ForegroundColor Red
            continue
        }
    }
    
    if (-not $success) {
        Write-Host "  ⚠️ Không thể tải $fontName từ bất kỳ URL nào" -ForegroundColor Yellow
        Write-Host "  Vui lòng tải thủ công từ: https://fonts.google.com/noto/specimen/Noto+Sans" -ForegroundColor Yellow
    }
}

Write-Host "`n✅ Hoàn tất!" -ForegroundColor Green
Write-Host "Kiểm tra font đã tải:" -ForegroundColor Cyan
Get-ChildItem $fontDir -Filter "*.ttf" | Select-Object Name, Length
