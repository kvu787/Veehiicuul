@echo off
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Run.ps1" -WithPresentMon
exit /b %errorlevel%
