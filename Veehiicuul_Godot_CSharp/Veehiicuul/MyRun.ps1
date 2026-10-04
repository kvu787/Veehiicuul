Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ProcessName = "Veehiicuul_Godot_CSharp.exe"
$ProcessPath = Join-Path $PSScriptRoot 'MyBuildOutput\Veehiicuul_Godot_CSharp.exe'
$PresentMonPath = "$env:UserProfile\Program\PresentMon-2.6.0-x64.exe"

$LogFolderPath = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
$GodotLogFilePath = "$LogFolderPath\Godot.log"
$GarbageCollectionSessionName = 'VeehiicuulGarbageCollection_' + [Guid]::NewGuid().ToString('N')
$GarbageCollectionProcess = $null
$PresentMonProcess = $null
$ApplicationCapture = $null
$ApplicationProcess = $null
$ApplicationExitTimer = $null
$TranscriptStarted = $false
$ResultCode = 0

try {
    New-Item -ItemType Directory -Path $LogFolderPath | Out-Null
    Set-Content -LiteralPath (Join-Path $PSScriptRoot 'MyLogOutput\.gdignore') -Value ''
    Start-Transcript -LiteralPath (Join-Path $LogFolderPath 'MyRun.log') | Out-Null
    $TranscriptStarted = $true
    Write-Host "Session logs: $LogFolderPath"
    . (Join-Path $PSScriptRoot 'ConsoleProcess.ps1')
    if (-not (Test-Path -LiteralPath $ProcessPath -PathType Leaf)) {
        throw "Application build is missing: $ProcessPath"
    }
    $Principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())

    if ($args -notcontains 'NoGarbageCollection') {
        $CaptureScriptPath = Join-Path $PSScriptRoot 'CaptureGarbageCollection.ps1'
        $CaptureArguments = '-NoLogo -NoProfile -ExecutionPolicy Bypass -File "{0}" -SessionName "{1}" -LogFolderPath "{2}" -ParentProcessId {3}' -f $CaptureScriptPath, $GarbageCollectionSessionName, $LogFolderPath, $PID
        $CaptureStartParameters = @{
            FilePath = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
            ArgumentList = $CaptureArguments
            WindowStyle = 'Hidden'
            PassThru = $true
        }
        if (-not $Principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
            $CaptureStartParameters.Verb = 'RunAs'
        }
        $GarbageCollectionProcess = Start-Process @CaptureStartParameters
        $StartupTimer = [Diagnostics.Stopwatch]::StartNew()
        $ReadyPath = Join-Path $LogFolderPath 'GarbageCollectionReady.signal'
        while (-not (Test-Path -LiteralPath $ReadyPath)) {
            if ($GarbageCollectionProcess.WaitForExit(100)) {
                throw 'The GC capture helper exited before capture started. See GarbageCollectionCapture.log.'
            }
            if ($StartupTimer.Elapsed.TotalSeconds -ge 30) {
                throw 'Timed out starting GC capture. See GarbageCollectionCapture.log.'
            }
        }
        Write-Host "GC capture started: $GarbageCollectionSessionName"
    }

    if ($args -notcontains 'NoPresentMon') {
        if (-not (Test-Path -LiteralPath $PresentMonPath -PathType Leaf)) {
            throw "PresentMon is missing: $PresentMonPath"
        }
        # The elevated helper owns a separate, hidden console for a safe Ctrl+C broadcast.
        # Do not use --terminate_on_proc_exit because it has caused capture issues.
        $CaptureScriptPath = Join-Path $PSScriptRoot 'CapturePresentMon.ps1'
        $CaptureArguments = '-NoLogo -NoProfile -ExecutionPolicy Bypass -File "{0}" -PresentMonPath "{1}" -ProcessName "{2}" -LogFolderPath "{3}" -ParentProcessId {4}' -f $CaptureScriptPath, $PresentMonPath, $ProcessName, $LogFolderPath, $PID
        $CaptureStartParameters = @{
            FilePath = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
            ArgumentList = $CaptureArguments
            WindowStyle = 'Hidden'
            PassThru = $true
        }
        if (-not $Principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
            $CaptureStartParameters.Verb = 'RunAs'
        }
        $PresentMonProcess = Start-Process @CaptureStartParameters
        $StartupTimer = [Diagnostics.Stopwatch]::StartNew()
        $ReadyPath = Join-Path $LogFolderPath 'PresentMonReady.signal'
        while (-not (Test-Path -LiteralPath $ReadyPath)) {
            if ($PresentMonProcess.WaitForExit(100)) {
                throw 'The PresentMon helper exited before capture started. See PresentMonCapture.log and PresentMon.log.'
            }
            if ($StartupTimer.Elapsed.TotalSeconds -ge 30) {
                throw 'Timed out starting PresentMon. See PresentMonCapture.log and PresentMon.log.'
            }
        }
        Write-Host "PresentMon capture started with PID=$(Get-Content -LiteralPath $ReadyPath)."
    }

    $ApplicationCapture = [VeehiicuulWorkflow.ConsoleProcess]::new($ProcessPath, ('--log-file "{0}"' -f $GodotLogFilePath), (Join-Path $LogFolderPath 'Console.log'), $true)
    $ApplicationProcess = $ApplicationCapture.Process
    if (-not $ApplicationProcess.HasExited) {
        $ApplicationProcess.PriorityClass = [Diagnostics.ProcessPriorityClass]::High
    }
    Write-Host "Launched with PID=$($ApplicationProcess.Id)"
    [ordered]@{
        ProcessId = $ApplicationProcess.Id
        PresentMonEnabled = ($null -ne $PresentMonProcess)
        PresentMonShutdownDelaySeconds = 5
        QpcFrequency = [Diagnostics.Stopwatch]::Frequency
        GarbageCollectionSessionName = $GarbageCollectionSessionName
        GarbageCollectionEnabled = ($null -ne $GarbageCollectionProcess)
        GarbageCollectionProvider = 'Microsoft-Windows-DotNETRuntime'
        GarbageCollectionKeywords = '0x1'
        GarbageCollectionLevel = 4
        GarbageCollectionClock = 'QPC'
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $LogFolderPath 'CaptureMetadata.json') -Encoding UTF8
    # The timed overload returns when the process exits, without waiting for pipe callbacks.
    while (-not $ApplicationProcess.WaitForExit(250)) { }
    $ApplicationExitTimer = [Diagnostics.Stopwatch]::StartNew()
    $ApplicationCapture.FlushOutput()
    $ResultCode = $ApplicationProcess.ExitCode
    Write-Host "Application exited with code $ResultCode."
} catch {
    Write-Host ($_ | Out-String) -ForegroundColor Red
    $ResultCode = 1
} finally {
    if ($null -ne $PresentMonProcess) {
        try {
            if ($null -ne $ApplicationExitTimer) {
                Write-Host 'Waiting until five seconds after the application exited before sending Ctrl+C to PresentMon.'
                $RemainingMilliseconds = [Math]::Max(0, 5000 - [int]$ApplicationExitTimer.ElapsedMilliseconds)
                if ($RemainingMilliseconds -gt 0) { Start-Sleep -Milliseconds $RemainingMilliseconds }
            }
            Set-Content -LiteralPath (Join-Path $LogFolderPath 'PresentMonStop.signal') -Value ''
            if (-not $PresentMonProcess.WaitForExit(35000)) {
                throw 'PresentMon has not finished shutting down. See PresentMonCapture.log and PresentMon.log.'
            }
            if (-not (Test-Path -LiteralPath (Join-Path $LogFolderPath 'PresentMonStopped.signal'))) {
                throw 'PresentMon capture did not stop successfully. See PresentMonCapture.log and PresentMon.log.'
            }
            Write-Host 'PresentMon stopped with Ctrl+C and flushed its output.'
        } catch {
            Write-Host ($_ | Out-String) -ForegroundColor Red
            $ResultCode = 1
        } finally {
            $PresentMonProcess.Dispose()
        }
    }
    if ($null -ne $GarbageCollectionProcess) {
        try {
            # Signal the already-elevated helper so stopping ETW requires no second UAC prompt.
            Set-Content -LiteralPath (Join-Path $LogFolderPath 'GarbageCollectionStop.signal') -Value ''
            if (-not $GarbageCollectionProcess.WaitForExit(30000)) {
                throw "GC trace has not finished flushing. See GarbageCollectionCapture.log. Session: $GarbageCollectionSessionName"
            }
            if (-not (Test-Path -LiteralPath (Join-Path $LogFolderPath 'GarbageCollectionStopped.signal'))) {
                throw "GC trace did not stop successfully. See GarbageCollectionCapture.log. If still active, stop it with: logman stop $GarbageCollectionSessionName -ets"
            }
            Write-Host 'GC capture stopped and flushed to GarbageCollection.etl.'
        } catch {
            Write-Host ($_ | Out-String) -ForegroundColor Red
            $ResultCode = 1
        } finally {
            $GarbageCollectionProcess.Dispose()
        }
    }
    if ($null -ne $ApplicationCapture) { $ApplicationCapture.Dispose() }
    if ($TranscriptStarted) { Stop-Transcript | Out-Null }
}

exit $ResultCode
