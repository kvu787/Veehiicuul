param(
    [switch] $VerifyStartupFailure
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

$transcriptStarted = $false
$previousLogDirectory = $env:VEEHIICUUL_LOG_DIRECTORY
$resultCode = 0
try {
    $logFolderPath = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
    New-Item -ItemType Directory -Path $logFolderPath -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $PSScriptRoot 'MyLogOutput\.gdignore') -Value ''
    Start-Transcript -LiteralPath (Join-Path $logFolderPath 'Launcher.log') | Out-Null
    $transcriptStarted = $true
    $env:VEEHIICUUL_LOG_DIRECTORY = $logFolderPath

    Write-Host "Session logs: $logFolderPath"
    foreach ($relativePath in @('Build\Veehiicuul_Godot_CSharp.exe', 'Build\Veehiicuul_Godot_CSharp.pck', 'Build\data_Veehiicuul_Godot_CSharp_windows_x86_64\Veehiicuul_Godot_CSharp.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $relativePath) -PathType Leaf)) {
            throw "Application build is missing or incomplete ($relativePath). Run Build.cmd first."
        }
    }
    $executable = Join-Path $PSScriptRoot 'Build\Veehiicuul_Godot_CSharp.exe'
    $arguments = @('--log-file', ('"' + (Join-Path $logFolderPath 'Godot.log') + '"'))
    if ($VerifyStartupFailure) { $arguments = @('--headless') + $arguments }
    # Own the process handle from creation. Windows PowerShell's Start-Process can
    # lose ExitCode for an application that terminates this quickly.
    $applicationProcess = New-Object System.Diagnostics.Process
    $applicationProcess.StartInfo.FileName = $executable
    $applicationProcess.StartInfo.Arguments = $arguments -join ' '
    $applicationProcess.StartInfo.WorkingDirectory = Split-Path $executable
    $applicationProcess.StartInfo.UseShellExecute = $false
    $applicationProcess.StartInfo.CreateNoWindow = $true
    $applicationProcess.StartInfo.WindowStyle = 'Hidden'
    $applicationProcess.StartInfo.RedirectStandardOutput = $true
    $applicationProcess.StartInfo.RedirectStandardError = $true
    if (-not $applicationProcess.Start()) { throw 'Could not launch the exported application.' }
    $standardOutput = $applicationProcess.StandardOutput.ReadToEndAsync()
    $standardError = $applicationProcess.StandardError.ReadToEndAsync()
    if ($VerifyStartupFailure) {
        if (-not $applicationProcess.WaitForExit(20000)) {
            $applicationProcess.Kill()
            throw 'Startup failure verification timed out: the app did not close.'
        }
    } else {
        $applicationProcess.WaitForExit()
    }
    $resultCode = $applicationProcess.ExitCode
    Set-Content -LiteralPath (Join-Path $logFolderPath 'Console.log') -Value $standardOutput.Result
    Set-Content -LiteralPath (Join-Path $logFolderPath 'ConsoleError.log') -Value $standardError.Result
    $applicationProcess.Dispose()
    if ($VerifyStartupFailure) {
        if ($resultCode -ne 1) { throw "Expected application failure exit code 1, got $resultCode." }
        $exceptionLog = Get-Content -LiteralPath (Join-Path $logFolderPath 'Godot.log') -Raw
        if ($exceptionLog -notmatch 'Intentional startup stop:' -or $exceptionLog -notmatch 'Main\._Ready') {
            throw 'Godot.log does not identify the intentional Main._Ready exception.'
        }
        if (Select-String -LiteralPath (Join-Path $logFolderPath 'Godot.log') -Pattern 'Game initialization completed|A frame ran before' -Quiet) {
            throw 'Game initialization or processing occurred after the startup exception.'
        }
        Write-Host 'PASS: the exported app stopped in Main._Ready and quit through Godot (exit code 1).'
        $resultCode = 0
    } else {
        Write-Host "Application exited with code $resultCode. The intentional startup exception is recorded in Godot.log."
    }
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    $resultCode = 1
} finally {
    $env:VEEHIICUUL_LOG_DIRECTORY = $previousLogDirectory
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
exit $resultCode
