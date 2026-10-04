param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^VeehiicuulGarbageCollection_[0-9a-f]{32}$')]
    [string] $SessionName,

    [Parameter(Mandatory = $true)]
    [string] $LogFolderPath,

    [Parameter(Mandatory = $true)]
    [int] $ParentProcessId
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$TraceStarted = $false
$TranscriptStarted = $false
$ParentProcess = $null
$ResultCode = 0
$LogmanPath = Join-Path $env:SystemRoot 'System32\logman.exe'

try {
    Start-Transcript -LiteralPath (Join-Path $LogFolderPath 'GarbageCollectionCapture.log') | Out-Null
    $TranscriptStarted = $true
    # Keep a handle to this particular launcher, rather than repeatedly looking up a reusable PID.
    $ParentProcess = [Diagnostics.Process]::GetProcessById($ParentProcessId)
    $null = $ParentProcess.Handle
    $TracePath = Join-Path $LogFolderPath 'GarbageCollection.etl'
    & $LogmanPath create trace $SessionName -p Microsoft-Windows-DotNETRuntime 0x1 4 -ct perf -bs 64 -nb 16 128 -o $TracePath -ets 2>&1 | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Starting the GC trace failed (logman exit code $LASTEXITCODE)."
    }
    $TraceStarted = $true
    Set-Content -LiteralPath (Join-Path $LogFolderPath 'GarbageCollectionReady.signal') -Value ''
    Write-Host "GC trace started with QPC timestamps: $TracePath"

    $StopPath = Join-Path $LogFolderPath 'GarbageCollectionStop.signal'
    while (-not (Test-Path -LiteralPath $StopPath)) {
        # Also clean up if the launcher is terminated without executing its finally block.
        if ($ParentProcess.WaitForExit(250)) { break }
    }
} catch {
    Write-Host ($_ | Out-String) -ForegroundColor Red
    $ResultCode = 1
} finally {
    if ($TraceStarted) {
        try {
            & $LogmanPath stop $SessionName -ets 2>&1 | Out-Host
            if ($LASTEXITCODE -ne 0) {
                throw "Stopping the GC trace failed (logman exit code $LASTEXITCODE). Session: $SessionName"
            }
            Set-Content -LiteralPath (Join-Path $LogFolderPath 'GarbageCollectionStopped.signal') -Value ''
            Write-Host 'GC trace stopped and flushed.'
        } catch {
            Write-Host ($_ | Out-String) -ForegroundColor Red
            $ResultCode = 1
        }
    }
    if ($null -ne $ParentProcess) { $ParentProcess.Dispose() }
    if ($TranscriptStarted) { Stop-Transcript | Out-Null }
}

exit $ResultCode
