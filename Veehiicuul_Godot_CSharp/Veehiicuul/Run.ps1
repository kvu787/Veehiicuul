Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$transcriptStarted = $false
$applicationProcess = $null
$resultCode = 0
try {
    $logFolderPath = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
    New-Item -ItemType Directory -Path $logFolderPath -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $PSScriptRoot 'MyLogOutput\.gdignore') -Value ''
    Start-Transcript -LiteralPath (Join-Path $logFolderPath 'Launcher.log') | Out-Null
    $transcriptStarted = $true

    Write-Host "Session logs: $logFolderPath"
    foreach ($relativePath in @('Build\Veehiicuul_Godot_CSharp.exe', 'Build\Veehiicuul_Godot_CSharp.pck', 'Build\data_Veehiicuul_Godot_CSharp_windows_x86_64\Veehiicuul_Godot_CSharp.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $relativePath) -PathType Leaf)) {
            throw "Application build is missing or incomplete ($relativePath). Run Build.cmd first."
        }
    }
    $executable = Join-Path $PSScriptRoot 'Build\Veehiicuul_Godot_CSharp.exe'
    # Own the process handle from creation. Windows PowerShell's Start-Process can
    # lose ExitCode for an application that terminates this quickly.
    $applicationProcess = New-Object System.Diagnostics.Process
    $applicationProcess.StartInfo.FileName = $executable
    $applicationProcess.StartInfo.Arguments = '--log-file "{0}"' -f (Join-Path $logFolderPath 'Godot.log')
    $applicationProcess.StartInfo.WorkingDirectory = Split-Path -Parent $executable
    $applicationProcess.StartInfo.UseShellExecute = $false
    $applicationProcess.StartInfo.CreateNoWindow = $true
    $applicationProcess.StartInfo.WindowStyle = 'Hidden'
    $applicationProcess.StartInfo.RedirectStandardOutput = $true
    $applicationProcess.StartInfo.RedirectStandardError = $true
    if (-not $applicationProcess.Start()) { throw 'Could not launch the exported application.' }
    $applicationProcess.PriorityClass = [System.Diagnostics.ProcessPriorityClass]::High
    $standardOutput = $applicationProcess.StandardOutput.ReadToEndAsync()
    $standardError = $applicationProcess.StandardError.ReadToEndAsync()
    $applicationProcess.WaitForExit()
    $resultCode = $applicationProcess.ExitCode
    Set-Content -LiteralPath (Join-Path $logFolderPath 'Console.log') -Value $standardOutput.Result
    Set-Content -LiteralPath (Join-Path $logFolderPath 'ConsoleError.log') -Value $standardError.Result
    Write-Host "Application exited with code $resultCode."
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    $resultCode = 1
} finally {
    if ($null -ne $applicationProcess) { $applicationProcess.Dispose() }
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
exit $resultCode
