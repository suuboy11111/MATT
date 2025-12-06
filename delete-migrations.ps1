# Script để xóa tất cả migrations cũ
Write-Host "Deleting old migrations..." -ForegroundColor Yellow

$migrationsPath = ".\Migrations"

if (Test-Path $migrationsPath) {
    Get-ChildItem -Path $migrationsPath -File | ForEach-Object {
        try {
            Remove-Item $_.FullName -Force
            Write-Host "Deleted: $($_.Name)" -ForegroundColor Green
        } catch {
            Write-Host "Failed to delete: $($_.Name) - $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "Please close the file in your IDE and try again." -ForegroundColor Yellow
        }
    }
    
    Write-Host "`nDone! All migration files deleted." -ForegroundColor Green
    Write-Host "Now you can create new migrations with PostgreSQL." -ForegroundColor Cyan
} else {
    Write-Host "Migrations folder not found!" -ForegroundColor Red
}
