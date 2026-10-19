param(
    [ValidateSet('Open', 'Test', 'Prepare', 'Verify', 'Preview', 'Benchmark')]
    [string]$Mode = 'Open',
    [string]$BlenderPath = "$env:USERPROFILE\Program\blender-4.5.14-windows-x64\blender.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$logRoot = Join-Path $PSScriptRoot 'LogOutput'
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
while ($true) {
    $logDirectory = Join-Path $logRoot (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss')
    try {
        # Do not mix two sessions that start in the same second. Directory
        # creation is exclusive, including when two wrappers run concurrently.
        New-Item -ItemType Directory -Path $logDirectory -ErrorAction Stop | Out-Null
        break
    }
    catch {
        if (-not (Test-Path -LiteralPath $logDirectory -PathType Container)) {
            throw
        }
        Start-Sleep -Milliseconds 100
    }
}
$logPath = Join-Path $logDirectory 'Blender.log'
try {
    if (-not (Test-Path -LiteralPath $BlenderPath -PathType Leaf)) {
        throw "Blender was not found at '$BlenderPath'. Pass -BlenderPath with the path to Blender 4.5.14."
    }
    $examplePath = Join-Path $PSScriptRoot 'MirrorWorkflowGeometryNodes.blend'
    $arguments = switch ($Mode) {
        'Open' { @('--disable-autoexec', $examplePath) }
        'Test' { @('--background', '--factory-startup', '--disable-autoexec', '--python-exit-code', '1', '--python', (Join-Path $PSScriptRoot 'TestGeometryNodes.py')) }
        'Prepare' { @('--background', '--factory-startup', '--disable-autoexec', '--python-exit-code', '1', '--python', (Join-Path $PSScriptRoot 'PrepareExample.py')) }
        'Verify' { @('--background', '--factory-startup', '--disable-autoexec', $examplePath, '--python-exit-code', '1', '--python', (Join-Path $PSScriptRoot 'VerifySavedExample.py')) }
        'Preview' { @('--background', '--factory-startup', '--disable-autoexec', $examplePath, '--python-exit-code', '1', '--python', (Join-Path $PSScriptRoot 'RenderPreview.py')) }
        'Benchmark' { @('--background', '--factory-startup', '--disable-autoexec', '--python-exit-code', '1', '--python', (Join-Path $PSScriptRoot 'BenchmarkGeometryNodes.py')) }
    }
    if ($Mode -in @('Open', 'Verify', 'Preview') -and -not (Test-Path -LiteralPath $examplePath -PathType Leaf)) {
        throw 'The example is missing. Run Run.cmd -Mode Prepare to create it.'
    }
    "Mode: $Mode`nBlender: $BlenderPath`nStarted: $([DateTime]::UtcNow.ToString('o'))" | Set-Content -LiteralPath $logPath
    & $BlenderPath @arguments *>&1 | Tee-Object -FilePath $logPath -Append
    $blenderExitCode = $LASTEXITCODE
    "Finished: $([DateTime]::UtcNow.ToString('o'))`nExit code: $blenderExitCode" | Add-Content -LiteralPath $logPath
    if ($blenderExitCode -ne 0) {
        throw "Blender exited with code $blenderExitCode. See '$logPath'."
    }
}
catch {
    $_ | Out-String | Add-Content -LiteralPath $logPath
    Write-Error $_
    exit 1
}
