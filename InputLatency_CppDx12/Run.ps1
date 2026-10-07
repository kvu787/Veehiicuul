param(
    [ValidateSet('Release', 'Debug')][string] $Configuration = 'Release',
    [ValidateRange(0, 86400)][int] $DurationSeconds = 0,
    [switch] $SoftwareAdapter,
    [switch] $Hidden,
    [switch] $CaptureFrame
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$transcriptStarted = $false
try {
    $executable = Join-Path $PSScriptRoot "BuildOutput\$Configuration\InputLatency.exe"
    if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw "No build at '$executable'. Run Build.cmd first." }
    $logDirectory = Join-Path $PSScriptRoot ('LogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
    Start-Transcript -LiteralPath (Join-Path $logDirectory 'Launcher.log') | Out-Null
    $transcriptStarted = $true
    Write-Host "Session logs: $logDirectory"
    $applicationArguments = @('--duration', $DurationSeconds.ToString(), '--log-directory', ('"' + $logDirectory + '"'))
    if ($SoftwareAdapter) { $applicationArguments += '--software-adapter' }
    if ($Hidden) { $applicationArguments += '--hidden' }
    if ($CaptureFrame) { $applicationArguments += '--capture-frame' }
    $windowStyle = if ($Hidden) { 'Hidden' } else { 'Normal' }
    $applicationProcess = Start-Process -FilePath $executable -WorkingDirectory $PSScriptRoot -ArgumentList $applicationArguments -WindowStyle $windowStyle -Wait -PassThru
    if ($applicationProcess.ExitCode -ne 0) { throw "Application exited with code $($applicationProcess.ExitCode). See Application.log." }
}
catch { Write-Host $_.Exception.Message -ForegroundColor White; exit 1 }
finally { if ($transcriptStarted) { Stop-Transcript | Out-Null } }
