param([ValidateSet('Release', 'Debug')][string] $Configuration = 'Release')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$applicationDirectory = Split-Path $PSScriptRoot
$executable = Join-Path $applicationDirectory "BuildOutput\$Configuration\InputLatency.exe"
if (-not (Test-Path -LiteralPath $executable)) { throw 'Build the application before verification.' }
$logDirectory = Join-Path $applicationDirectory ('LogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null

# Only move the pointer while our test application is foreground; no clicks or
# keyboard input are generated. Restore its original position afterward.
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class DisplayVerificationInput
{
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] public struct MouseInput
    { public int X, Y; public uint Data, Flags, Time; public UIntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] public struct Input
    { public uint Type; public MouseInput Mouse; }
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint SendInput(uint count, Input[] inputs, int size);
    public static bool Move(int x)
    {
        var input = new Input { Mouse = new MouseInput { X = x, Flags = 1 } };
        return SendInput(1, new[] { input }, Marshal.SizeOf<Input>()) == 1;
    }
}
'@
$originalPosition = [DisplayVerificationInput+Point]::new()
[DisplayVerificationInput]::GetCursorPos([ref]$originalPosition) | Out-Null
$application = $null
try {
    $arguments = @('--duration', '8', '--capture-frame', '--log-directory', ('"' + $logDirectory + '"'))
    $application = Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
    for ($attempt = 0; $attempt -lt 50; $attempt++) {
        $application.Refresh()
        if ($application.HasExited) { throw 'Application exited before input verification.' }
        if ($application.MainWindowHandle -ne 0) { break }
        Start-Sleep -Milliseconds 100
    }
    [DisplayVerificationInput]::SetForegroundWindow($application.MainWindowHandle) | Out-Null
    for ($index = 0; $index -lt 24; $index++) {
        if ([DisplayVerificationInput]::GetForegroundWindow() -ne $application.MainWindowHandle) { break }
        $movement = if ($index % 2 -eq 0) { 2 } else { -2 }
        if (-not [DisplayVerificationInput]::Move($movement)) { throw 'Windows rejected synthetic mouse movement.' }
        Start-Sleep -Milliseconds 150
    }
    if (-not $application.WaitForExit(15000)) { throw 'Application failed to finish verification.' }
    if ($application.ExitCode -ne 0) { throw "Application exited with $($application.ExitCode)." }
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
    if ($measurements.Count -eq 0) { Write-Host 'This GameInput path did not expose synthetic input. Physical input remains a manual verification.' }
    Write-Host "Verification logs: $logDirectory"
}
finally {
    [DisplayVerificationInput]::SetCursorPos($originalPosition.X, $originalPosition.Y) | Out-Null
    if ($null -ne $application) {
        $application.Refresh()
        if (-not $application.HasExited) { $application.Kill(); $application.WaitForExit() }
    }
}
