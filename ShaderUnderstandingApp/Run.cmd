@echo off
pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0Run.ps1" -WaitForKey %*
exit /b %errorlevel%
