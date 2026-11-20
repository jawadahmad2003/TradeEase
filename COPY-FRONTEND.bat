@echo off
echo Copying TradeEase React UI to tradeease-client...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0copy-frontend.ps1"
echo.
echo Done. Run: cd tradeease-client ^&^& npm.cmd install ^&^& npm.cmd run dev
pause
