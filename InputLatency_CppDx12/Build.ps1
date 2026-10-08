param([switch] $Test, [ValidateSet('Release', 'Debug')][string] $Configuration = 'Release')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

function Invoke-Checked {
    param([string] $FilePath, [Parameter(ValueFromRemainingArguments)][string[]] $ArgumentList)
    & $FilePath @ArgumentList | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "'$FilePath' exited with code $LASTEXITCODE." }
}

$transcriptStarted = $false
try {
    $logDirectory = Join-Path $PSScriptRoot ('LogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
    Start-Transcript -LiteralPath (Join-Path $logDirectory 'Build.log') | Out-Null
    $transcriptStarted = $true
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $installation = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 Microsoft.VisualStudio.Component.VC.CMake.Project -property installationPath
    if ($LASTEXITCODE -ne 0 -or -not $installation) { throw 'Install Visual Studio C++ desktop tools, CMake tools, and a current Windows SDK.' }
    $installation = $installation.Trim()
    $cmake = Join-Path $installation 'Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe'
    $ninja = Join-Path $installation 'Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja\ninja.exe'
    $developerCommand = Join-Path $installation 'Common7\Tools\VsDevCmd.bat'
    $environmentLines = & $env:ComSpec /d /s /c "call `"$developerCommand`" -arch=x64 -host_arch=x64 >nul && set"
    if ($LASTEXITCODE -ne 0) { throw 'Could not initialize the x64 compiler environment.' }
    $importedVariables = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($line in $environmentLines) {
        if ($line -match '^([^=]+)=(.*)$' -and $importedVariables.Add($Matches[1])) {
            [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2], 'Process')
        }
    }
    $compiler = (Get-Command cl.exe -CommandType Application).Source
    $configureOptions = @()
    $buildDirectory = Join-Path $PSScriptRoot "BuildOutput\$Configuration"
    $cachePath = Join-Path $buildDirectory 'CMakeCache.txt'
    if (Test-Path -LiteralPath $cachePath) {
        $expectedPaths = @{ CMAKE_HOME_DIRECTORY = $PSScriptRoot; CMAKE_CACHEFILE_DIR = $buildDirectory; CMAKE_CXX_COMPILER = $compiler }
        foreach ($line in Get-Content -LiteralPath $cachePath) {
            if ($line -match '^(CMAKE_HOME_DIRECTORY|CMAKE_CACHEFILE_DIR|CMAKE_CXX_COMPILER):[^=]+=(.*)$') {
                if ($Matches[2].Replace('/', '\').TrimEnd('\') -ine $expectedPaths[$Matches[1]].Replace('/', '\').TrimEnd('\')) {
                    $configureOptions += '--fresh'
                    break
                }
            }
        }
    }
    Invoke-Checked $cmake --preset $Configuration @configureOptions "-DCMAKE_MAKE_PROGRAM=$ninja" "-DCMAKE_CXX_COMPILER=$compiler"
    Invoke-Checked $cmake --build --preset $Configuration --parallel
    if ($Test) { Invoke-Checked (Join-Path (Split-Path $cmake) 'ctest.exe') --preset $Configuration }
    Write-Host "Executable: $PSScriptRoot\BuildOutput\$Configuration\InputLatency.exe"
}
catch { Write-Host $_.Exception.Message -ForegroundColor Red; exit 1 }
finally { if ($transcriptStarted) { Stop-Transcript | Out-Null } }
