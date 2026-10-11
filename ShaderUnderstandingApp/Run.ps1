[CmdletBinding()]
param(
    [switch]$VerifyInterface,
    [switch]$WaitForKey,
    [string]$BuildDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

try {
    $buildRoot = Join-Path $PSScriptRoot 'BuildOutput'
    if (-not $BuildDirectory) {
        $currentBuildPath = Join-Path $buildRoot 'CurrentBuild.txt'
        if (-not (Test-Path -LiteralPath $currentBuildPath -PathType Leaf)) {
            throw 'No built application was found. Double-click Build.cmd first.'
        }
        # Readers may keep the old pointer open during an atomic publication.
        # Either pointer names a complete, immutable build directory.
        $stream = [System.IO.File]::Open($currentBuildPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, ([System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete))
        $reader = [System.IO.StreamReader]::new($stream)
        try { $relativeBuild = $reader.ReadToEnd().Trim() } finally { $reader.Dispose() }
        if (-not $relativeBuild) { throw 'The current build reference is empty. Double-click Build.cmd first.' }
        $BuildDirectory = Join-Path $buildRoot $relativeBuild
    }
    $BuildDirectory = [System.IO.Path]::GetFullPath($BuildDirectory)
    $releasesRoot = [System.IO.Path]::GetFullPath((Join-Path $buildRoot 'Releases'))
    if (-not $BuildDirectory.StartsWith($releasesRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'The selected build must be inside BuildOutput/Releases.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $BuildDirectory 'ShaderUnderstandingApp.exe') -PathType Leaf)) {
        throw 'No built application was found. Double-click Build.cmd first.'
    }
    $logDirectory = & (Join-Path $PSScriptRoot 'CreateSessionDirectory.ps1')
    $applicationDirectory = Join-Path $logDirectory 'Application'
    New-Item -ItemType Directory -Path $applicationDirectory | Out-Null
    # Only this session uses these binaries. Sessions launched here never load
    # assemblies from the build or publication directories.
    Get-ChildItem -LiteralPath $BuildDirectory -Force | Copy-Item -Destination $applicationDirectory -Recurse
    $applicationPath = Join-Path $applicationDirectory 'ShaderUnderstandingApp.exe'
    $arguments = @('--log-directory', ('"{0}"' -f $logDirectory))
    if ($VerifyInterface) { $arguments += '--verify-interface' }
    @("Started: $(Get-Date -Format o)", "Build: $BuildDirectory", "Application: $applicationPath", "Offscreen verification: $VerifyInterface") |
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
