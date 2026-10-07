@echo off
pwsh -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build.ps1" %*
exit /b %errorlevel%
