$ErrorActionPreference = 'Stop'

Set-Location -LiteralPath $PSScriptRoot

function Invoke-Checked {
    param(
        [Parameter(Mandatory)]
        [string] $FilePath,

        [Parameter(ValueFromRemainingArguments)]
        [string[]] $ArgumentList
    )

    & $FilePath @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "'$FilePath' exited with code $LASTEXITCODE."
    }
}

try {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere -PathType Leaf)) {
        throw 'Visual Studio Installer''s vswhere.exe was not found. Install Visual Studio with the Desktop development with C++ workload.'
    }

    $vsInstall = & $vswhere `
        -latest `
        -products '*' `
        -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 Microsoft.VisualStudio.Component.VC.CMake.Project `
        -property installationPath

    if ($LASTEXITCODE -ne 0 -or -not $vsInstall) {
        throw 'A Visual Studio installation with the C++ desktop and CMake tools was not found.'
    }

    $vsInstall = $vsInstall.Trim()
    $cmake = Join-Path $vsInstall 'Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe'
    $ninja = Join-Path $vsInstall 'Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja\ninja.exe'
    $vsDevCmd = Join-Path $vsInstall 'Common7\Tools\VsDevCmd.bat'

    foreach ($tool in @($cmake, $ninja, $vsDevCmd)) {
        if (-not (Test-Path -LiteralPath $tool -PathType Leaf)) {
            throw "A required Visual Studio build tool was not found at: $tool"
        }
    }

    $devCommand = "call `"$vsDevCmd`" -arch=x64 -host_arch=x64 >nul && set"
    $environmentLines = & $env:ComSpec /d /s /c $devCommand
    if ($LASTEXITCODE -ne 0) {
        throw 'Visual Studio''s x64 developer environment could not be initialized.'
    }

    foreach ($line in $environmentLines) {
        if ($line -match '^([^=]+)=(.*)$') {
            [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2], 'Process')
        }
    }

    Invoke-Checked $cmake `
        -S . `
        -B 'build\release' `
        -G Ninja `
        -DCMAKE_BUILD_TYPE=Release `
        "-DCMAKE_MAKE_PROGRAM=$ninja"

    Invoke-Checked $cmake --build 'build\release' --parallel
    Invoke-Checked (Join-Path $PSScriptRoot 'build\release\SimpleDirectX12Game.exe')
}
catch {
    Write-Host
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host 'Build or launch failed. Review the messages above.' -ForegroundColor Red
    Read-Host 'Press Enter to continue'
    exit 1
}
