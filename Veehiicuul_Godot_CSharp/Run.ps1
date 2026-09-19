param(
    [switch] $BuildOnly,
    [switch] $VerifyStartupFailure
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

function Invoke-Checked {
    param([string] $Program, [string[]] $Arguments)
    & $Program @Arguments
    if ($LASTEXITCODE -ne 0) { throw "'$Program' exited with code $LASTEXITCODE." }
}

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

    $godotDirectory = Join-Path $env:UserProfile 'Program\Godot_v4.7.2-stable_mono_win64'
    $godot = Join-Path $godotDirectory 'Godot_v4.7.2-stable_mono_win64_console.exe'
    if (-not (Test-Path -LiteralPath $godot)) { throw "Godot 4.7.2 .NET was not found at $godot" }
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install the .NET 10 SDK.' }
    $template = Join-Path $godotDirectory 'editor_data\export_templates\4.7.2.stable.mono\windows_release_x86_64.exe'
    if (-not (Test-Path -LiteralPath $template)) { throw "Matching .NET export templates are missing: $template" }
    New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'Build') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $PSScriptRoot 'Build\.gdignore') -Value ''
    Write-Host "Session logs: $logFolderPath"
    Invoke-Checked 'dotnet' @('build', 'Veehiicuul_Godot_CSharp.slnx', '--configuration', 'Debug', '--nologo', '-warnaserror')
    Invoke-Checked $godot @('--headless', '--editor', '--path', $PSScriptRoot, '--import', '--log-file', (Join-Path $logFolderPath 'Import.log'))
    if (Select-String -LiteralPath (Join-Path $logFolderPath 'Import.log') -Pattern '^ERROR:' -Quiet) {
        throw 'Godot reported an import error. See Import.log.'
    }
    Invoke-Checked 'dotnet' @('build', 'Veehiicuul_Godot_CSharp.slnx', '--configuration', 'ExportRelease', '--nologo', '-warnaserror')
    Invoke-Checked $godot @('--headless', '--path', $PSScriptRoot, '--export-release', 'Windows Desktop', '--log-file', (Join-Path $logFolderPath 'Export.log'))
    if (Select-String -LiteralPath (Join-Path $logFolderPath 'Export.log') -Pattern '^ERROR:' -Quiet) {
        throw 'Godot reported an export error. See Export.log.'
    }
    if (-not $BuildOnly) {
        $executable = Join-Path $PSScriptRoot 'Build\Veehiicuul_Godot_CSharp.exe'
        $arguments = @('--log-file', ('"' + (Join-Path $logFolderPath 'Godot.log') + '"'))
        if ($VerifyStartupFailure) { $arguments = @('--headless') + $arguments }
        # Own the process handle from creation. Windows PowerShell's Start-Process can
        # lose ExitCode for an application that terminates this quickly.
        $applicationProcess = New-Object System.Diagnostics.Process
        $applicationProcess.StartInfo.FileName = $executable
        $applicationProcess.StartInfo.Arguments = $arguments -join ' '
        $applicationProcess.StartInfo.WorkingDirectory = $PSScriptRoot
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
            if ($resultCode -ne -1) { throw "Expected immediate process termination exit code -1, got $resultCode." }
            $fatalLog = Get-Content -LiteralPath (Join-Path $logFolderPath 'Fatal.log') -Raw
            if ($fatalLog -notmatch 'Intentional startup stop:' -or $fatalLog -notmatch 'Main\._Ready') {
                throw 'Fatal.log does not identify the intentional Main._Ready exception.'
            }
            if (Select-String -LiteralPath (Join-Path $logFolderPath 'Godot.log') -Pattern 'Game initialization completed|A frame ran before' -Quiet) {
                throw 'Game initialization or processing occurred after the startup exception.'
            }
            Write-Host 'PASS: the exported app stopped in Main._Ready and terminated immediately (exit code -1).'
            $resultCode = 0
        } else {
            Write-Host "Application exited with code $resultCode. The intentional startup exception is recorded in Fatal.log."
        }
    }
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    $resultCode = 1
} finally {
    $env:VEEHIICUUL_LOG_DIRECTORY = $previousLogDirectory
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
exit $resultCode
