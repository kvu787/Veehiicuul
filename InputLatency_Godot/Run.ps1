param(
    [switch] $BuildOnly,
    [switch] $Test,
    [switch] $VisualTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

function Invoke-Checked {
    param([string] $Program, [string[]] $Arguments)
    & $Program @Arguments
    if ($LASTEXITCODE -ne 0) { throw "'$Program' exited with code $LASTEXITCODE." }
}

$transcriptStarted = $false
$previousLogDirectory = $env:INPUT_LATENCY_LOG_DIRECTORY
try {
    $logFolderPath = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
    New-Item -ItemType Directory -Path $logFolderPath -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $PSScriptRoot 'MyLogOutput\.gdignore') -Value ''
    Start-Transcript -LiteralPath (Join-Path $logFolderPath 'Launcher.log') | Out-Null
    $transcriptStarted = $true
    $env:INPUT_LATENCY_LOG_DIRECTORY = $logFolderPath

    $godotDirectory = Join-Path $env:UserProfile 'Program\Godot_v4.7.2-stable_mono_win64'
    $godot = Join-Path $godotDirectory 'Godot_v4.7.2-stable_mono_win64_console.exe'
    if (-not (Test-Path -LiteralPath $godot)) { throw "Godot 4.7.2 .NET was not found at $godot" }
    $version = & $godot --version
    if ($LASTEXITCODE -ne 0 -or $version -notlike '4.7.2.stable.mono.*') {
        throw "Expected Godot 4.7.2 .NET; found '$version'."
    }
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install the .NET 10 SDK.' }
    $template = Join-Path $godotDirectory 'editor_data\export_templates\4.7.2.stable.mono\windows_release_x86_64.exe'
    if (-not (Test-Path -LiteralPath $template)) { throw "Matching .NET export templates are missing: $template" }

    New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'Build') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $PSScriptRoot 'Build\.gdignore') -Value ''
    Write-Host "Session logs: $logFolderPath"
    # Import first so the exporter knows about C# scripts and scene resources.
    Invoke-Checked $godot @('--headless', '--editor', '--path', $PSScriptRoot, '--import', '--log-file', (Join-Path $logFolderPath 'Import.log'))
    Invoke-Checked 'dotnet' @('build', 'InputLatencyGodot.slnx', '--configuration', 'ExportRelease', '--nologo')
    Invoke-Checked $godot @('--headless', '--path', $PSScriptRoot, '--export-release', 'Windows Desktop', '--log-file', (Join-Path $logFolderPath 'Export.log'))
    # Some export-plugin failures are logged even when Godot returns success.
    if (Select-String -LiteralPath (Join-Path $logFolderPath 'Export.log') -Pattern '^ERROR:' -Quiet) {
        throw 'Godot reported an export error. See Export.log.'
    }

    $executable = Join-Path $PSScriptRoot 'Build\InputLatencyGodot.exe'
    if ($Test -or $VisualTest) {
        $arguments = @('--log-file', ('"' + (Join-Path $logFolderPath 'Verification.log') + '"'), '--', '--self-test')
        if (-not $VisualTest) { $arguments = @('--headless') + $arguments }
    }
    elseif (-not $BuildOnly) {
        $arguments = @('--rendering-driver', 'd3d12', '--log-file', ('"' + (Join-Path $logFolderPath 'Godot.log') + '"'))
    }
    else { return }

    $windowStyle = if ($Test -and -not $VisualTest) { 'Hidden' } else { 'Normal' }
    $applicationProcess = Start-Process -FilePath $executable -ArgumentList $arguments -WorkingDirectory (Split-Path $executable) -WindowStyle $windowStyle -PassThru
    Write-Host "Game process ID: $($applicationProcess.Id)"
    Write-Host "Game executable: $executable"
    if ($Test -or $VisualTest) {
        if (-not $applicationProcess.WaitForExit(30000)) {
            $applicationProcess.Kill()
            throw 'Verification timed out. See Verification.log.'
        }
        Get-Content -LiteralPath (Join-Path $logFolderPath 'Verification.log')
    }
    else { $applicationProcess.WaitForExit() }
    if ($applicationProcess.ExitCode -ne 0) { throw "Application exited with code $($applicationProcess.ExitCode)." }
}
catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    if (-not ($BuildOnly -or $Test -or $VisualTest)) { Read-Host 'Press Enter to close' | Out-Null }
    exit 1
}
finally {
    $env:INPUT_LATENCY_LOG_DIRECTORY = $previousLogDirectory
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
