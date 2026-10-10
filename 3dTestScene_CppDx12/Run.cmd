@echo off
pwsh -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Run.ps1" %*
exit /b %errorlevel%
