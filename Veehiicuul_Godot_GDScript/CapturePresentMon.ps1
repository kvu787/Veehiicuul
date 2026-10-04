[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $SessionFolder,
    [Parameter(Mandatory = $true)] [string] $SessionName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$capture = $null
$transcriptStarted = $false
$resultCode = 0
$presentMonPath = Join-Path $env:UserProfile 'Program\PresentMon-2.6.0-x64.exe'
try {
    Start-Transcript -LiteralPath (Join-Path $SessionFolder 'CaptureLauncher.log') | Out-Null
    $transcriptStarted = $true
    $arguments = '--session_name "{0}" --process_name "Veehiicuul_Godot_GDScript.exe" --output_file "{1}" --set_circular_buffer_size 65536 --no_console_stats --qpc_time' -f $SessionName, (Join-Path $SessionFolder 'PresentMon.csv')
    $capture = Start-Process -FilePath $presentMonPath -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $SessionFolder 'PresentMonConsole.log') -RedirectStandardError (Join-Path $SessionFolder 'PresentMonErrors.log')
    Start-Sleep -Milliseconds 1000
    if ($capture.HasExited) { throw 'PresentMon exited during capture initialization.' }
    Set-Content -LiteralPath (Join-Path $SessionFolder 'CaptureReady') -Value $capture.Id
    while (-not (Test-Path -LiteralPath (Join-Path $SessionFolder 'StopCapture') -PathType Leaf)) {
        if ($capture.HasExited) { throw 'PresentMon exited before the launcher stopped the capture.' }
        Start-Sleep -Milliseconds 100
    }
} catch {
    Set-Content -LiteralPath (Join-Path $SessionFolder 'CaptureFailure.log') -Value $_.Exception.Message
    Write-Host $_.Exception.Message -ForegroundColor Red
    $resultCode = 1
} finally {
    if ($null -ne $capture) {
        if (-not $capture.HasExited) {
            $stop = Start-Process -FilePath $presentMonPath -ArgumentList ('--session_name "{0}" --terminate_existing_session' -f $SessionName) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $SessionFolder 'PresentMonStop.log') -RedirectStandardError (Join-Path $SessionFolder 'PresentMonStopErrors.log')
            if (-not $stop.WaitForExit(10000)) { $stop.Kill(); $resultCode = 1 }
            $stop.Dispose()
            if (-not $capture.WaitForExit(10000)) { $capture.Kill(); $capture.WaitForExit(); $resultCode = 1 }
        }
        $capture.Dispose()
    }
    if ($resultCode -eq 0) { Set-Content -LiteralPath (Join-Path $SessionFolder 'CaptureComplete') -Value 'PresentMon session closed.' }
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
exit $resultCode
