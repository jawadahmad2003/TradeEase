# TradeEase — Start backend + frontend (development)
# Run: powershell -ExecutionPolicy Bypass -File start-dev.ps1

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Backend = Join-Path $Root "Trade Ease"
$Frontend = Join-Path $Root "tradeease-client"

Write-Host ""
Write-Host "=== TradeEase Development ===" -ForegroundColor Cyan
Write-Host ""

# Health check hint
Write-Host "1. Ensure database is set up:  database\setup-database.ps1" -ForegroundColor Yellow
Write-Host "2. Backend will start on http://localhost:5003" -ForegroundColor Yellow
Write-Host "3. Frontend will start on http://localhost:5173" -ForegroundColor Yellow
Write-Host ""

Write-Host "Starting backend in new window..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$Backend'; dotnet run"

Start-Sleep -Seconds 3

Write-Host "Starting frontend in new window..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$Frontend'; npm.cmd install; npm.cmd run dev"

Write-Host ""
Write-Host "Demo login:" -ForegroundColor Cyan
Write-Host "  Admin:  admin@tradeease.pk  (any password)"
Write-Host "  Trader: ahmed.raza@razaexports.pk  (any password)"
Write-Host ""
Write-Host "Health check: http://localhost:5003/api/Health" -ForegroundColor Gray
