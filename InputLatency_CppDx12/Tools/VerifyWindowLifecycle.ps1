param(
    [ValidateSet('Release', 'Debug')][string] $Configuration = 'Debug',
    [switch] $AllowRejectedClockFrames
)

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
public static class WindowLifecycleVerification
{
    [StructLayout(LayoutKind.Sequential)] public struct Rectangle { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct MonitorInformation { public uint Size; public Rectangle Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr window);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool PostMessageW(IntPtr window, uint message, UIntPtr first, IntPtr second);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr window, out Rectangle rectangle);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out Rectangle rectangle);
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll")] public static extern bool GetMonitorInfoW(IntPtr monitor, ref MonitorInformation information);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] public static extern IntPtr GetWindowLongPtrW(IntPtr window, int index);
    [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [DllImport("kernel32.dll")] public static extern bool QueryPerformanceCounter(out long counter);
    public static long Timestamp() { QueryPerformanceCounter(out long counter); return counter; }
}
'@

$application = $null
$previousDpiContext = [WindowLifecycleVerification]::SetThreadDpiAwarenessContext([IntPtr](-4))
$actions = [Collections.Generic.List[object]]::new()
function Record-Action {
    param([string] $Name)
    $actions.Add([pscustomobject]@{ Action = $Name; Qpc = [WindowLifecycleVerification]::Timestamp() })
    Write-Host $Name
}
try {
    $arguments = @('--duration', '35', '--capture-frame', '--log-directory', ('"' + $logDirectory + '"'))
    $application = Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Normal -PassThru
    for ($attempt = 0; $attempt -lt 50; $attempt++) {
        $application.Refresh()
        if ($application.HasExited) { throw 'Application exited before window verification.' }
        if ($application.MainWindowHandle -ne 0) { break }
        Start-Sleep -Milliseconds 100
    }
    $window = $application.MainWindowHandle
    if ($window -eq 0) { throw 'Application window did not appear.' }
    [WindowLifecycleVerification]::SetForegroundWindow($window) | Out-Null
    Start-Sleep -Seconds 2
    if (([WindowLifecycleVerification]::GetWindowLongPtrW($window, -16).ToInt64() -band 0x00CF0000) -ne 0) { throw 'Application did not start in borderless fullscreen.' }
    $bounds = [WindowLifecycleVerification+Rectangle]::new()
    $monitor = [WindowLifecycleVerification+MonitorInformation]::new()
    $monitor.Size = [Runtime.InteropServices.Marshal]::SizeOf($monitor)
    if (-not [WindowLifecycleVerification]::GetWindowRect($window, [ref]$bounds) -or
        -not [WindowLifecycleVerification]::GetMonitorInfoW([WindowLifecycleVerification]::MonitorFromWindow($window, 2), [ref]$monitor)) { throw 'Cannot inspect startup window and monitor bounds.' }
    if ($bounds.Left -ne $monitor.Monitor.Left -or $bounds.Top -ne $monitor.Monitor.Top -or
        $bounds.Right -ne $monitor.Monitor.Right -or $bounds.Bottom -ne $monitor.Monitor.Bottom) { throw 'Startup window does not cover its complete monitor.' }
    Record-Action 'StartupFullscreen'
    [WindowLifecycleVerification]::PostMessageW($window, 0x100, [UIntPtr]0x7A, [IntPtr]0) | Out-Null
    Start-Sleep -Seconds 2
    if (([WindowLifecycleVerification]::GetWindowLongPtrW($window, -16).ToInt64() -band 0x00CF0000) -ne 0x00CF0000) { throw 'F11 did not restore startup windowed mode.' }
    $client = [WindowLifecycleVerification+Rectangle]::new()
    if (-not [WindowLifecycleVerification]::GetClientRect($window, [ref]$client) -or
        $client.Right -ne 1280 -or $client.Bottom -ne 860) { throw 'F11 did not restore the original windowed client size.' }
    Record-Action 'StartupWindowed'
    Record-Action 'BeforeMinimize'
    [WindowLifecycleVerification]::ShowWindow($window, 6) | Out-Null
    Start-Sleep -Milliseconds 300
    if (-not [WindowLifecycleVerification]::IsIconic($window)) { throw 'Minimize did not take effect.' }
    Record-Action 'Minimized'
    Start-Sleep -Seconds 3
    Record-Action 'BeforeRestore'
    [WindowLifecycleVerification]::ShowWindow($window, 9) | Out-Null
    [WindowLifecycleVerification]::SetForegroundWindow($window) | Out-Null
    Start-Sleep -Seconds 2
    if ([WindowLifecycleVerification]::IsIconic($window)) { throw 'Restore did not take effect.' }
    for ($attempt = 0; $attempt -lt 50 -and [WindowLifecycleVerification]::GetForegroundWindow() -ne $window; $attempt++) {
        Start-Sleep -Milliseconds 100
    }
    if ([WindowLifecycleVerification]::GetForegroundWindow() -ne $window) { throw 'The restored application did not regain focus.' }
    Record-Action 'RestoredAndFocused'
    [WindowLifecycleVerification]::ShowWindow($window, 3) | Out-Null
    Start-Sleep -Seconds 2
    if (-not [WindowLifecycleVerification]::IsZoomed($window)) { throw 'Maximize did not take effect.' }
    Record-Action 'Maximized'
    [WindowLifecycleVerification]::ShowWindow($window, 9) | Out-Null
    Start-Sleep -Seconds 2
    [WindowLifecycleVerification]::PostMessageW($window, 0x100, [UIntPtr]0x7A, [IntPtr]0) | Out-Null
    Start-Sleep -Seconds 2
    if (([WindowLifecycleVerification]::GetWindowLongPtrW($window, -16).ToInt64() -band 0x00CF0000) -ne 0) { throw 'F11 did not enter borderless fullscreen.' }
    Record-Action 'Fullscreen'
    [WindowLifecycleVerification]::PostMessageW($window, 0x100, [UIntPtr]0x7A, [IntPtr]0) | Out-Null
    Start-Sleep -Seconds 2
    if (([WindowLifecycleVerification]::GetWindowLongPtrW($window, -16).ToInt64() -band 0x00CF0000) -ne 0x00CF0000) { throw 'F11 did not restore windowed mode.' }
    Record-Action 'Windowed'
    $client = [WindowLifecycleVerification+Rectangle]::new()
    $bounds = [WindowLifecycleVerification+Rectangle]::new()
    [WindowLifecycleVerification]::GetClientRect($window, [ref]$client) | Out-Null
    [WindowLifecycleVerification]::GetWindowRect($window, [ref]$bounds) | Out-Null
    $width = 1024 + ($bounds.Right - $bounds.Left) - $client.Right
    $height = 720 + ($bounds.Bottom - $bounds.Top) - $client.Bottom
    if (-not [WindowLifecycleVerification]::SetWindowPos($window, [IntPtr]::Zero, 0, 0, $width, $height, 0x16)) { throw 'Resize failed.' }
    Start-Sleep -Seconds 2
    [WindowLifecycleVerification]::GetClientRect($window, [ref]$client) | Out-Null
    if ($client.Right -ne 1024 -or $client.Bottom -ne 720) { throw 'Minimum client-size verification failed.' }
    Record-Action 'MinimumClientSize'
    if (-not $application.WaitForExit(20000)) { throw 'Application did not finish window verification.' }
    if ($application.ExitCode -ne 0) { throw "Application exited with $($application.ExitCode)." }
    $actions | Export-Csv -LiteralPath (Join-Path $logDirectory 'WindowActions.csv') -NoTypeInformation -Encoding utf8
    $frames = @(Import-Csv -LiteralPath (Join-Path $logDirectory 'DisplayFrames.csv'))
    $frequency = [long]((Get-Content -LiteralPath (Join-Path $logDirectory 'DisplayDiagnostics.txt') | Where-Object { $_ -match '^QpcFrequency=' }) -replace '^QpcFrequency=', '')
    $minimizedAt = [long]($actions | Where-Object Action -EQ 'Minimized').Qpc
    $restoreAt = [long]($actions | Where-Object Action -EQ 'BeforeRestore').Qpc
    $unexpected = @($frames | Where-Object { $_.PresentStartQpc -ne '' -and [long]$_.PresentStartQpc -gt $minimizedAt + $frequency / 20 -and [long]$_.PresentStartQpc -lt $restoreAt })
    if ($unexpected.Count -ne 0) { throw 'Frames were presented while the window was minimized.' }
    if (@($frames | Where-Object { $_.PresentStartQpc -ne '' -and [long]$_.PresentStartQpc -gt $restoreAt }).Count -eq 0) { throw 'Rendering did not resume after restore.' }
    & (Join-Path $PSScriptRoot 'ValidateSession.ps1') -LogDirectory $logDirectory -RequireGamepad -AllowRejectedClockFrames:$AllowRejectedClockFrames
    Write-Host "Window lifecycle verification logs: $logDirectory"
}
finally {
    [WindowLifecycleVerification]::SetThreadDpiAwarenessContext($previousDpiContext) | Out-Null
    if ($null -ne $application) {
        $application.Refresh()
        if (-not $application.HasExited) {
            if ($application.MainWindowHandle -ne 0) { [WindowLifecycleVerification]::PostMessageW($application.MainWindowHandle, 0x10, [UIntPtr]0, [IntPtr]0) | Out-Null }
            if (-not $application.WaitForExit(40000)) { $application.Kill(); $application.WaitForExit() }
        }
    }
}
