Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ProcessName = "Veehiicuul_Godot_CSharp.exe"
$ProcessPath = Join-Path $PSScriptRoot 'MyBuildOutput\Veehiicuul_Godot_CSharp.exe'
$PresentMonPath = "$env:UserProfile\Program\PresentMon-2.6.0-x64.exe"

$LogFolderPath = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
$PresentMonLogFilePath = "$LogFolderPath\PresentMon.csv"
$GodotLogFilePath = "$LogFolderPath\Godot.log"
$GarbageCollectionSessionName = 'VeehiicuulGarbageCollection_' + [Guid]::NewGuid().ToString('N')
$GarbageCollectionProcess = $null
$ApplicationProcess = $null
$TranscriptStarted = $false
$ResultCode = 0

try {
    if (-not (Test-Path -LiteralPath $ProcessPath -PathType Leaf)) {
        throw "Application build is missing: $ProcessPath"
    }
    New-Item -ItemType Directory -Path $LogFolderPath | Out-Null
    Start-Transcript -LiteralPath (Join-Path $LogFolderPath 'Launcher.log') | Out-Null
    $TranscriptStarted = $true
    Write-Host "Session logs: $LogFolderPath"

    if ($args -notcontains 'NoGarbageCollection') {
        $CaptureScriptPath = Join-Path $PSScriptRoot 'CaptureGarbageCollection.ps1'
        $CaptureArguments = '-NoLogo -NoProfile -ExecutionPolicy Bypass -File "{0}" -SessionName "{1}" -LogFolderPath "{2}" -ParentProcessId {3}' -f $CaptureScriptPath, $GarbageCollectionSessionName, $LogFolderPath, $PID
        $CaptureStartParameters = @{
            FilePath = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
            ArgumentList = $CaptureArguments
            WindowStyle = 'Hidden'
            PassThru = $true
        }
        $Principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
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

    if (($args -notcontains 'NoPresentMon') -and (Test-Path -LiteralPath $PresentMonPath)) {
        # This PresentMon process must be manually closed by the user. This is intentional.
        # "--terminate_on_proc_exit" isn't used because I've observed issues with it.
        Start-Process `
            -FilePath $PresentMonPath `
            -ArgumentList "--process_name `"$($ProcessName)`" --output_file `"$($PresentMonLogFilePath)`" --set_circular_buffer_size 65536 --no_console_stats --qpc_time" `
            -Verb 'RunAs'
    }

    $ApplicationProcess = Start-Process -FilePath $ProcessPath -ArgumentList "--log-file `"$GodotLogFilePath`"" -PassThru
    if (-not $ApplicationProcess.HasExited) {
        $ApplicationProcess.PriorityClass = [Diagnostics.ProcessPriorityClass]::High
    }
    Write-Host "Launched with PID=$($ApplicationProcess.Id)"
    [ordered]@{
        ProcessId = $ApplicationProcess.Id
        QpcFrequency = [Diagnostics.Stopwatch]::Frequency
        GarbageCollectionSessionName = $GarbageCollectionSessionName
        GarbageCollectionEnabled = ($null -ne $GarbageCollectionProcess)
        GarbageCollectionProvider = 'Microsoft-Windows-DotNETRuntime'
        GarbageCollectionKeywords = '0x1'
        GarbageCollectionLevel = 4
        GarbageCollectionClock = 'QPC'
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $LogFolderPath 'CaptureMetadata.json') -Encoding UTF8
    $ApplicationProcess.WaitForExit()
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    $ResultCode = 1
} finally {
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
            Write-Host $_.Exception.Message -ForegroundColor Red
            $ResultCode = 1
        } finally {
            $GarbageCollectionProcess.Dispose()
        }
    }
    if ($null -ne $ApplicationProcess) { $ApplicationProcess.Dispose() }
    if ($TranscriptStarted) { Stop-Transcript | Out-Null }
}

exit $ResultCode
