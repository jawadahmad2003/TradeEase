@echo off
echo === TradeEase: Full setup ===
echo.
echo Step 1: Database...
powershell -ExecutionPolicy Bypass -File "%~dp0database\setup-database.ps1"
if errorlevel 1 (
  echo.
  echo Database setup failed — see options above.
  echo You can still build the UI and fix SQL Server later.
  echo.
  choice /C YN /M "Continue with React UI build anyway"
  if errorlevel 2 exit /b 1
)
echo.
echo Step 2: React UI build...
call "%~dp0BUILD-UI.bat" NOPAUSE
echo.
echo Step 3: Open Trade Ease.slnx in Visual Studio and press F5
echo    URL: https://localhost:7126/spa/index.html
echo    Login: admin@tradeease.pk / any password
pause
