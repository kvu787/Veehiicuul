param(
    [Parameter(Mandatory = $true)]
    [string] $PresentMonPath,

    [Parameter(Mandatory = $true)]
    [string] $ProcessName,

    [Parameter(Mandatory = $true)]
    [string] $LogFolderPath,

    [Parameter(Mandatory = $true)]
    [int] $ParentProcessId
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$TranscriptStarted = $false
$ParentProcess = $null
$PresentMonCapture = $null
$ResultCode = 0

try {
    Start-Transcript -LiteralPath (Join-Path $LogFolderPath 'PresentMonCapture.log') | Out-Null
    $TranscriptStarted = $true
    . (Join-Path $PSScriptRoot 'ConsoleProcess.ps1')
    [VeehiicuulWorkflow.PresentMonConsole]::Prepare()
    $ParentProcess = [Diagnostics.Process]::GetProcessById($ParentProcessId)
    $null = $ParentProcess.Handle
    $PresentMonArguments = '--process_name "{0}" --output_file "{1}" --set_circular_buffer_size 65536 --no_console_stats --qpc_time --track_etw_status' -f $ProcessName, (Join-Path $LogFolderPath 'PresentMon.csv')
    Write-Host "PresentMon command: $PresentMonPath $PresentMonArguments"
    $PresentMonCapture = [VeehiicuulWorkflow.ConsoleProcess]::new($PresentMonPath, $PresentMonArguments, (Join-Path $LogFolderPath 'PresentMon.log'), $false)
    Write-Host "PresentMon started with PID=$($PresentMonCapture.Process.Id)."
    Set-Content -LiteralPath (Join-Path $LogFolderPath 'PresentMonReady.signal') -Value $PresentMonCapture.Process.Id

    $StopPath = Join-Path $LogFolderPath 'PresentMonStop.signal'
    while (-not (Test-Path -LiteralPath $StopPath)) {
        if ($PresentMonCapture.Process.WaitForExit(100)) {
            throw "PresentMon exited before shutdown was requested (exit code $($PresentMonCapture.Process.ExitCode)). See PresentMon.log."
        }
        # Clean up if the launcher exits without running its finally block.
        if ($ParentProcess.HasExited) { break }
    }
} catch {
    Write-Host ($_ | Out-String) -ForegroundColor Red
    $ResultCode = 1
} finally {
    if ($null -ne $PresentMonCapture) {
        try {
            if (-not $PresentMonCapture.Process.HasExited) {
                Write-Host 'Sending Ctrl+C to PresentMon.'
                [VeehiicuulWorkflow.PresentMonConsole]::SendControlC($PresentMonCapture.Process.Id)
                if (-not $PresentMonCapture.Process.WaitForExit(30000)) {
                    throw 'PresentMon did not finish flushing within 30 seconds after Ctrl+C.'
                }
            }
            $PresentMonCapture.FlushOutput()
            Write-Host "PresentMon exited with code $($PresentMonCapture.Process.ExitCode); console output flushed to PresentMon.log."
            if ($PresentMonCapture.Process.ExitCode -ne 0) {
                throw "PresentMon failed with exit code $($PresentMonCapture.Process.ExitCode). See PresentMon.log."
            }
            if ($ResultCode -eq 0) {
                Set-Content -LiteralPath (Join-Path $LogFolderPath 'PresentMonStopped.signal') -Value ''
            }
        } catch {
            Write-Host ($_ | Out-String) -ForegroundColor Red
            $ResultCode = 1
        } finally {
            $PresentMonCapture.Dispose()
        }
    }
    if ($null -ne $ParentProcess) { $ParentProcess.Dispose() }
    if ($TranscriptStarted) { Stop-Transcript | Out-Null }
}

exit $ResultCode
