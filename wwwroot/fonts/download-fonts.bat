@echo off
echo ========================================
echo Tải Font Noto Sans hỗ trợ tiếng Việt
echo ========================================
echo.

REM Kiểm tra PowerShell
where powershell >nul 2>&1
if %errorlevel% neq 0 (
    echo Lỗi: Không tìm thấy PowerShell!
    pause
    exit /b 1
)

REM Chạy script PowerShell
powershell -ExecutionPolicy Bypass -File "%~dp0download-fonts.ps1"

pause
