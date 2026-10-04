[CmdletBinding()]
param(
    [switch] $PresentMon,
    [switch] $Headless,
    [switch] $Windowed,
    [ValidateRange(0, 2147483647)] [int] $QuitAfter = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$transcriptStarted = $false
$applicationProcess = $null
$captureProcess = $null
$resultCode = 0
try {
    $buildPath = Join-Path $PSScriptRoot 'Build'
    foreach ($name in @('Veehiicuul_Godot_GDScript.exe', 'Veehiicuul_Godot_GDScript.pck')) {
        if (-not (Test-Path -LiteralPath (Join-Path $buildPath $name) -PathType Leaf)) { throw "Application build is missing or incomplete ($name). Run Build.cmd first." }
    }
    $logFolderPath = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
    New-Item -ItemType Directory -Path $logFolderPath -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $PSScriptRoot 'MyLogOutput\.gdignore') -Value ''
    Start-Transcript -LiteralPath (Join-Path $logFolderPath 'Launcher.log') | Out-Null
    $transcriptStarted = $true
    Write-Host "Session logs: $logFolderPath"

    if ($PresentMon) {
        $presentMonPath = Join-Path $env:UserProfile 'Program\PresentMon-2.6.0-x64.exe'
        if (-not (Test-Path -LiteralPath $presentMonPath -PathType Leaf)) { throw "PresentMon is missing: $presentMonPath" }
        $captureSessionName = 'VeehiicuulGDScript_' + $PID + '_' + (Get-Date -Format 'yyyyMMddHHmmss')
        $captureArguments = '-NoLogo -NoProfile -ExecutionPolicy Bypass -File "{0}" -SessionFolder "{1}" -SessionName "{2}"' -f (Join-Path $PSScriptRoot 'CapturePresentMon.ps1'), $logFolderPath, $captureSessionName
        $captureProcess = Start-Process -FilePath 'powershell.exe' -ArgumentList $captureArguments -Verb RunAs -WindowStyle Hidden -PassThru
        $captureStartup = [System.Diagnostics.Stopwatch]::StartNew()
        while (-not (Test-Path -LiteralPath (Join-Path $logFolderPath 'CaptureReady') -PathType Leaf)) {
            if ($captureProcess.HasExited -or $captureStartup.Elapsed.TotalSeconds -gt 30) { throw 'PresentMon capture initialization failed or timed out. See CaptureLauncher.log.' }
            Start-Sleep -Milliseconds 100
        }
    }

    # A UTC bridge gives pure GDScript an estimated QPC epoch for player markers.
    $qpcBefore = [System.Diagnostics.Stopwatch]::GetTimestamp()
    $utcTicks = [DateTime]::UtcNow.Ticks
    $qpcAfter = [System.Diagnostics.Stopwatch]::GetTimestamp()
    $qpcAnchor = $qpcBefore + [long](($qpcAfter - $qpcBefore) / 2)
    $unixSeconds = ($utcTicks - 621355968000000000L) / 10000000.0
    $frequency = [System.Diagnostics.Stopwatch]::Frequency
    $culture = [System.Globalization.CultureInfo]::InvariantCulture
    $clock = [ordered]@{ QpcAnchor = $qpcAnchor; QpcFrequency = $frequency; UnixSeconds = $unixSeconds; SamplingBracketQpc = $qpcAfter - $qpcBefore; Method = 'UTC bridge; application QPC markers are estimates' }
    $clock | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $logFolderPath 'ClockCalibration.json')
    $arguments = '--log-file "{0}"' -f (Join-Path $logFolderPath 'Godot.log')
    if ($Headless) { $arguments += ' --headless' }
    if ($Windowed) { $arguments += ' --windowed --resolution 1280x720' }
    if ($QuitAfter -gt 0) { $arguments += ' --quit-after ' + $QuitAfter }
    $arguments += ' -- --clock-qpc={0} --clock-frequency={1} --clock-unix={2}' -f $qpcAnchor, $frequency, $unixSeconds.ToString('F7', $culture)
    if ($Windowed) { $arguments += ' --application-windowed' }

    $applicationProcess = New-Object System.Diagnostics.Process
    $applicationProcess.StartInfo.FileName = Join-Path $buildPath 'Veehiicuul_Godot_GDScript.exe'
    $applicationProcess.StartInfo.Arguments = $arguments
    $applicationProcess.StartInfo.WorkingDirectory = $buildPath
    $applicationProcess.StartInfo.UseShellExecute = $false
    $applicationProcess.StartInfo.CreateNoWindow = $true
    $applicationProcess.StartInfo.WindowStyle = 'Hidden'
    $applicationProcess.StartInfo.RedirectStandardOutput = $true
    $applicationProcess.StartInfo.RedirectStandardError = $true
    if (-not $applicationProcess.Start()) { throw 'Could not launch the exported application.' }
    if (-not $applicationProcess.HasExited) { $applicationProcess.PriorityClass = [System.Diagnostics.ProcessPriorityClass]::High }
    $standardOutput = $applicationProcess.StandardOutput.ReadToEndAsync()
    $standardError = $applicationProcess.StandardError.ReadToEndAsync()
    $applicationProcess.WaitForExit()
    $resultCode = $applicationProcess.ExitCode
    Set-Content -LiteralPath (Join-Path $logFolderPath 'Console.log') -Value $standardOutput.Result
    Set-Content -LiteralPath (Join-Path $logFolderPath 'ConsoleError.log') -Value $standardError.Result
    if (($standardOutput.Result + $standardError.Result) -match '(?m)^(SCRIPT ERROR:|ERROR:)') { $resultCode = 1 }
    Write-Host "Application exited with code $resultCode."
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    $resultCode = 1
} finally {
    if ($null -ne $applicationProcess) { $applicationProcess.Dispose() }
    if ($null -ne $captureProcess) {
        Set-Content -LiteralPath (Join-Path $logFolderPath 'StopCapture') -Value 'Application session finished.'
        if (-not $captureProcess.HasExited -and -not $captureProcess.WaitForExit(25000)) {
            Write-Host 'PresentMon capture shutdown timed out. See CaptureLauncher.log.' -ForegroundColor Red
            $resultCode = 1
        }
        if (-not (Test-Path -LiteralPath (Join-Path $logFolderPath 'CaptureComplete') -PathType Leaf)) { $resultCode = 1 }
        $captureProcess.Dispose()
    }
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
exit $resultCode
