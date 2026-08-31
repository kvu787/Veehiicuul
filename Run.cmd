@echo off
setlocal
cd /d "%~dp0"

set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
    echo Visual Studio Installer's vswhere.exe was not found.
    echo Install Visual Studio with the Desktop development with C++ workload.
    goto :failure
)

set "VS_INSTALL="
for /f "usebackq tokens=*" %%I in (`"%VSWHERE%" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 Microsoft.VisualStudio.Component.VC.CMake.Project -property installationPath`) do set "VS_INSTALL=%%I"

if not defined VS_INSTALL (
    echo A Visual Studio installation with the C++ desktop and CMake tools was not found.
    goto :failure
)

set "CMAKE_EXE=%VS_INSTALL%\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
set "NINJA_EXE=%VS_INSTALL%\Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja\ninja.exe"

if not exist "%CMAKE_EXE%" (
    echo Visual Studio's bundled CMake was not found at:
    echo %CMAKE_EXE%
    goto :failure
)

if not exist "%NINJA_EXE%" (
    echo Visual Studio's bundled Ninja was not found at:
    echo %NINJA_EXE%
    goto :failure
)

call "%VS_INSTALL%\Common7\Tools\VsDevCmd.bat" -arch=x64 -host_arch=x64 >nul
if errorlevel 1 goto :failure

"%CMAKE_EXE%" -S . -B build\release -G Ninja -DCMAKE_BUILD_TYPE=Release "-DCMAKE_MAKE_PROGRAM=%NINJA_EXE%"
if errorlevel 1 goto :failure

"%CMAKE_EXE%" --build build\release --parallel
if errorlevel 1 goto :failure

"%~dp0build\release\SimpleDirectX12Game.exe"
if errorlevel 1 goto :failure

endlocal
exit /b 0

:failure
echo.
echo Build or launch failed. Review the messages above.
pause
endlocal
exit /b 1
