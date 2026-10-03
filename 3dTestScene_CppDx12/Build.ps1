param(
    [switch] $Test,
    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release'
)

Set-StrictMode -Version Latest

$ErrorActionPreference = 'Stop'

Set-Location -LiteralPath $PSScriptRoot

function Invoke-Checked {
    param(
        [Parameter(Mandatory)]
        [string] $FilePath,

        [Parameter(ValueFromRemainingArguments)]
        [string[]] $ArgumentList
    )

    # Route native output through PowerShell so Start-Transcript records it.
    & $FilePath @ArgumentList | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "'$FilePath' exited with code $LASTEXITCODE."
    }
}

$transcriptStarted = $false
try {
    $logFolderPath = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
    New-Item -ItemType Directory -Path $logFolderPath -ErrorAction Stop | Out-Null
    Start-Transcript -LiteralPath (Join-Path $logFolderPath 'Build.log') | Out-Null
    $transcriptStarted = $true
    Write-Host "Build logs: $logFolderPath"
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

    # Keep the developer environment's first value if casing duplicates occur.
    $importedVariables = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($line in $environmentLines) {
        if ($line -match '^([^=]+)=(.*)$' -and $importedVariables.Add($Matches[1])) {
            [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2], 'Process')
        }
    }

    $compiler = (Get-Command cl.exe -CommandType Application -ErrorAction Stop).Source
    $presetName = if ($Configuration -ieq 'Debug') { 'RunDebug' } else { 'RunRelease' }
    $presets = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'CMakePresets.json') -Raw | ConvertFrom-Json
    $configurePreset = $presets.configurePresets | Where-Object { $_.name -ceq $presetName }
    # Launcher presets declare their output path directly, using only ${sourceDir}.
    $buildDirectory = $configurePreset.binaryDir.Replace('${sourceDir}', $PSScriptRoot)
    $cachePath = Join-Path $buildDirectory 'CMakeCache.txt'
    $configureOptions = @()

    if (Test-Path -LiteralPath $cachePath -PathType Leaf) {
        $expectedPaths = @{
            CMAKE_HOME_DIRECTORY = $PSScriptRoot
            CMAKE_CACHEFILE_DIR = $buildDirectory
            CMAKE_CXX_COMPILER = $compiler
        }

        foreach ($line in Get-Content -LiteralPath $cachePath) {
            if ($line -match '^(CMAKE_HOME_DIRECTORY|CMAKE_CACHEFILE_DIR|CMAKE_CXX_COMPILER):[^=]+=(.*)$') {
                # CMake caches absolute source, build, and compiler paths.
                $cachedPath = $Matches[2].Replace('/', '\').TrimEnd('\')
                $expectedPath = $expectedPaths[$Matches[1]].Replace('/', '\').TrimEnd('\')
                if ($cachedPath -ine $expectedPath) {
                    Write-Host 'Repository, build folder, or compiler changed. Refreshing CMake configuration.'
                    $configureOptions += '--fresh'
                    break
                }
            }
        }
    }

    Invoke-Checked $cmake --preset $presetName @configureOptions "-DCMAKE_MAKE_PROGRAM=$ninja" "-DCMAKE_CXX_COMPILER=$compiler"

    Invoke-Checked $cmake --build --preset $presetName --parallel
    if ($Test) {
        Invoke-Checked (Join-Path (Split-Path $cmake) 'ctest.exe') --preset $presetName
    }
}
catch {
    Write-Host
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host 'Build failed. Review the messages above.' -ForegroundColor Red
    exit 1
}
finally {
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
