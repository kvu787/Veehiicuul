[CmdletBinding()]
param(
    [switch]$VerifyInterface,
    [switch]$WaitForKey
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

try {
    $applicationPath = Join-Path $PSScriptRoot 'BuildOutput/Release/ShaderUnderstandingApp.exe'
    if (-not (Test-Path -LiteralPath $applicationPath -PathType Leaf)) {
        throw 'No built application was found. Double-click Build.cmd first.'
    }
    $logParent = Join-Path $PSScriptRoot 'LogOutput'
    $logDirectory = Join-Path $logParent (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss')
    while (Test-Path -LiteralPath $logDirectory) {
        Start-Sleep -Milliseconds 40
        $logDirectory = Join-Path $logParent (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss')
    }
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
    $arguments = @('--log-directory', ('"{0}"' -f $logDirectory))
    if ($VerifyInterface) { $arguments += '--verify-interface' }
    @("Started: $(Get-Date -Format o)", "Application: $applicationPath", "Offscreen verification: $VerifyInterface") |
        Set-Content -LiteralPath (Join-Path $logDirectory 'Launcher.log') -Encoding utf8
    $launchOptions = @{
        FilePath = $applicationPath
        WorkingDirectory = $PSScriptRoot
        ArgumentList = $arguments
        PassThru = $true
        RedirectStandardOutput = Join-Path $logDirectory 'StandardOutput.log'
        RedirectStandardError = Join-Path $logDirectory 'StandardError.log'
    }
    if ($VerifyInterface) {
        # Verification renders WPF into PNGs without showing any application
        # window. The helper itself must also remain hidden.
        $launchOptions.WindowStyle = 'Hidden'
        $process = Start-Process @launchOptions
        if (-not $process.WaitForExit(60000)) {
            $process.Kill()
            throw "Interface verification exceeded 60 seconds. See $logDirectory."
        }
        if ($process.ExitCode -ne 0) { throw "Interface verification failed with exit code $($process.ExitCode). See $logDirectory." }
        Get-Content -LiteralPath (Join-Path $logDirectory 'Application.log')
    } else {
        # A normal user launch intentionally opens the requested interactive GUI.
        Start-Process @launchOptions | Out-Null
    }
    Write-Host "Session logs: $logDirectory"
    exit 0
} catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    if ($WaitForKey) { Read-Host 'Press Enter to close' | Out-Null }
    exit 1
}
