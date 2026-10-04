[CmdletBinding()]
param([switch] $Render)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Verification {
    param([string] $Name, [string[]] $Arguments)
    $process = New-Object System.Diagnostics.Process
    try {
        $process.StartInfo.FileName = $godot
        $process.StartInfo.Arguments = ($Arguments | ForEach-Object { '"' + $_ + '"' }) -join ' '
        $process.StartInfo.WorkingDirectory = $projectPath
        $process.StartInfo.UseShellExecute = $false
        $process.StartInfo.CreateNoWindow = $true
        $process.StartInfo.WindowStyle = 'Hidden'
        $process.StartInfo.RedirectStandardOutput = $true
        $process.StartInfo.RedirectStandardError = $true
        if (-not $process.Start()) { throw 'Could not start verification.' }
        $output = $process.StandardOutput.ReadToEndAsync()
        $errors = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(60000)) {
            $process.Kill()
            $process.WaitForExit()
            throw "Verification timed out: $Name"
        }
        $combined = $output.Result + "`n" + $errors.Result
        Set-Content -LiteralPath (Join-Path $logFolderPath ($Name + 'Console.log')) -Value $combined
        Write-Host $combined
        if ($process.ExitCode -ne 0 -or $combined -match '(?m)^(SCRIPT ERROR:|ERROR:|CHECK FAILED:)') { throw "Verification failed: $Name" }
    } finally {
        $process.Dispose()
    }
}

$resultCode = 0
$transcriptStarted = $false
try {
    $godot = Join-Path $env:UserProfile 'Program\Godot_v4.7.2-stable_win64.exe\Godot_v4.7.2-stable_win64_console.exe'
    $projectPath = Join-Path $PSScriptRoot 'Veehiicuul'
    $logFolderPath = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
    New-Item -ItemType Directory -Path $logFolderPath -Force | Out-Null
    Start-Transcript -LiteralPath (Join-Path $logFolderPath 'Verification.log') | Out-Null
    $transcriptStarted = $true
    Write-Host "Verification logs: $logFolderPath"
    foreach ($name in @('BehaviorChecks', 'ApplicationChecks')) {
        Invoke-Verification -Name $name -Arguments @('--headless', '--path', $projectPath, '--script', "res://Verification/$name.gd", '--log-file', (Join-Path $logFolderPath ($name + '.log')))
    }
    if ($Render) {
        Invoke-Verification -Name 'VisualChecks' -Arguments @('--path', $projectPath, '--windowed', '--resolution', '1280x720', '--script', 'res://Verification/VisualChecks.gd', '--log-file', (Join-Path $logFolderPath 'VisualChecks.log'), '--', '--application-windowed', ('--capture-directory=' + $logFolderPath))
        foreach ($name in @('FixedCamera.png', 'FollowCamera.png')) {
            if (-not (Test-Path -LiteralPath (Join-Path $logFolderPath $name) -PathType Leaf)) { throw "Missing visual capture: $name" }
        }
    }
    Write-Host 'All verification checks passed.'
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    $resultCode = 1
} finally {
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
exit $resultCode
