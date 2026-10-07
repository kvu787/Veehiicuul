param([ValidateSet('Release', 'Debug')][string] $Configuration = 'Release')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$applicationDirectory = Split-Path $PSScriptRoot
$executable = Join-Path $applicationDirectory "BuildOutput\$Configuration\InputLatency.exe"
if (-not (Test-Path -LiteralPath $executable)) { throw 'Build the application before verification.' }
$logDirectory = Join-Path $applicationDirectory ('LogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class DisplayVerificationWindow
{
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
}
'@
$application = $null
try {
    $arguments = @('--duration', '8', '--capture-frame', '--log-directory', ('"' + $logDirectory + '"'))
    $application = Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
    for ($attempt = 0; $attempt -lt 50; $attempt++) {
        $application.Refresh()
        if ($application.HasExited) { throw 'Application exited before display verification.' }
        if ($application.MainWindowHandle -ne 0) { break }
        Start-Sleep -Milliseconds 100
    }
    [DisplayVerificationWindow]::SetForegroundWindow($application.MainWindowHandle) | Out-Null
    Write-Host 'Use gamepad buttons, sticks, and triggers in the application during this eight-second session to verify input latency.'
    if (-not $application.WaitForExit(15000)) { throw 'Application failed to finish verification.' }
    if ($application.ExitCode -ne 0) { throw "Application exited with $($application.ExitCode)." }
    $devices = @(Import-Csv -LiteralPath (Join-Path $logDirectory 'Devices.csv'))
    if (@($devices | Where-Object Kind -NE 'Gamepad').Count -ne 0) { throw 'A non-gamepad device was measured.' }
    $summary = @(Import-Csv -LiteralPath (Join-Path $logDirectory 'Summary.csv'))
    if (@($summary | Where-Object Kind -NE 'Gamepad').Count -ne 0) { throw 'Non-gamepad statistics were recorded.' }
    $diagnostics = Get-Content -LiteralPath (Join-Path $logDirectory 'DisplayDiagnostics.txt')
    foreach ($name in 'StartOrProcessingError','EtwEventsLost','EtwBuffersLost','DroppedSubmissions','DroppedCompletions','DroppedMeasurements','InvalidClocks','DecoderOverflows') {
        if ($diagnostics -notcontains ($name + '=0')) { throw "Display diagnostics reported $name." }
    }
    $shown = @(Import-Csv -LiteralPath (Join-Path $logDirectory 'DisplayFrames.csv') | Where-Object Status -EQ 'Displayed')
    if ($shown.Count -eq 0) { throw 'No display events matched the application.' }
    $measurements = @(Import-Csv -LiteralPath (Join-Path $logDirectory 'DisplayReadings.csv') | Where-Object { $_.Valid -eq '1' -and $_.FirstDisplayForReading -eq '1' })
    foreach ($measurement in $measurements) {
        if ([long]$measurement.DisplayTimestampUs - [long]$measurement.ReadingTimestampUs -ne [long]$measurement.ReadingToDisplayUs) {
            throw 'A recorded latency differs from display timestamp minus reading timestamp.'
        }
    }
    Write-Host "Matched displayed frames: $($shown.Count). First displayed readings: $($measurements.Count)."
    if ($measurements.Count -eq 0) { Write-Host 'No fresh gamepad reading reached a displayed frame. Physical gamepad input remains a manual verification.' }
    Write-Host "Verification logs: $logDirectory"
}
finally {
    if ($null -ne $application) {
        $application.Refresh()
        if (-not $application.HasExited) { $application.Kill(); $application.WaitForExit() }
    }
}
