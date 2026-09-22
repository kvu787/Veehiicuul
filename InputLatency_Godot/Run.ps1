param(
    [switch] $Test,
    [switch] $VisualTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

$transcriptStarted = $false
$previousLogDirectory = $env:INPUT_LATENCY_LOG_DIRECTORY
try {
    $logFolderPath = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
    New-Item -ItemType Directory -Path $logFolderPath -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $PSScriptRoot 'MyLogOutput\.gdignore') -Value ''
    Start-Transcript -LiteralPath (Join-Path $logFolderPath 'Launcher.log') | Out-Null
    $transcriptStarted = $true
    $env:INPUT_LATENCY_LOG_DIRECTORY = $logFolderPath

    Write-Host "Session logs: $logFolderPath"
    $executable = Join-Path $PSScriptRoot 'Build\InputLatencyGodot.exe'
    foreach ($relativePath in @('Build\InputLatencyGodot.exe', 'Build\InputLatencyGodot.pck', 'Build\data_InputLatencyGodot_windows_x86_64\InputLatencyGodot.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $relativePath) -PathType Leaf)) {
            throw "Application build is missing or incomplete ($relativePath). Run Build.cmd first."
        }
    }
    if ($Test -or $VisualTest) {
        $arguments = @('--log-file', ('"' + (Join-Path $logFolderPath 'Verification.log') + '"'), '--', '--self-test')
        if (-not $VisualTest) { $arguments = @('--headless') + $arguments }
    }
    else {
        $arguments = @('--rendering-driver', 'd3d12', '--log-file', ('"' + (Join-Path $logFolderPath 'Godot.log') + '"'))
    }

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
    exit 1
}
finally {
    $env:INPUT_LATENCY_LOG_DIRECTORY = $previousLogDirectory
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
