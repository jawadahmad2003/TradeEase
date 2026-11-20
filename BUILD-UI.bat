@echo off
set NOPAUSE=%1
echo === TradeEase: Building React UI (green theme) ===
cd /d "%~dp0tradeease-client"
if not exist package.json (
  echo ERROR: tradeease-client folder not found.
  exit /b 1
)
call npm.cmd install
if errorlevel 1 exit /b 1
call npm.cmd run build
if errorlevel 1 exit /b 1
if not exist "..\Trade Ease\wwwroot\spa\index.html" (
  echo ERROR: Build completed but wwwroot\spa\index.html is missing.
  exit /b 1
)
echo.
echo SUCCESS: React UI built to Trade Ease\wwwroot\spa\
echo Open Visual Studio and press F5 - you will see the green TradeEase login.
if /I not "%NOPAUSE%"=="NOPAUSE" pause
