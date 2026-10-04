@echo off
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Verify.ps1" %*
exit /b %errorlevel%
