# TradeEase — Production build (single server deploy)
# Builds React into wwwroot/app and publishes ASP.NET app
# Run: powershell -ExecutionPolicy Bypass -File build-production.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Backend = Join-Path $Root "Trade Ease"
$Frontend = Join-Path $Root "tradeease-client"
$Publish = Join-Path $Root "publish"

Write-Host ""
Write-Host "=== TradeEase Production Build ===" -ForegroundColor Cyan

Write-Host "Building React app..." -ForegroundColor Yellow
Push-Location $Frontend
npm.cmd install
npm.cmd run build
Pop-Location

Write-Host "Publishing ASP.NET app..." -ForegroundColor Yellow
Push-Location $Backend
dotnet publish -c Release -o $Publish
Pop-Location

Write-Host ""
Write-Host "SUCCESS. Published to: $Publish" -ForegroundColor Green
Write-Host ""
Write-Host "Production environment variables (set before running):" -ForegroundColor Cyan
Write-Host '  $env:ASPNETCORE_ENVIRONMENT = "Production"'
Write-Host '  $env:ConnectionStrings__DefaultConnection = "Server=YOUR_SERVER;Database=TradeEaseDB;..."'
Write-Host '  $env:TRADEEASE_JWT_KEY = "your-secret-min-32-characters-long!"'
Write-Host ""
Write-Host "Run: cd publish; dotnet Trade` Ease.dll"
Write-Host "Open: http://localhost:5003/app/  (React SPA + API on same port)"
