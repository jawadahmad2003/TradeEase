@echo off
echo === TradeEase: Rebuild React UI + clear stale MSBuild SPA cache ===
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0FIX-BUILD.ps1"
pause
