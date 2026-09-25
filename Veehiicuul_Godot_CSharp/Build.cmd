@echo off
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Veehiicuul\Build.ps1" %*
exit /b %errorlevel%
