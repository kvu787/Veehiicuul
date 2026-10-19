param(
    [ValidateSet('Open', 'Test', 'Prepare', 'Verify', 'Preview')]
    [string]$Mode = 'Open',
    [string]$BlenderPath = "$env:USERPROFILE\Program\blender-4.5.14-windows-x64\blender.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$logDirectory = Join-Path $PSScriptRoot ('LogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
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
        'Verify' { @('--background', '--disable-autoexec', $examplePath, '--python-exit-code', '1', '--python', (Join-Path $PSScriptRoot 'VerifySavedExample.py')) }
        'Preview' { @('--background', '--disable-autoexec', $examplePath, '--python-exit-code', '1', '--python', (Join-Path $PSScriptRoot 'RenderPreview.py')) }
    }
    if ($Mode -in @('Open', 'Verify', 'Preview') -and -not (Test-Path -LiteralPath $examplePath -PathType Leaf)) {
        throw 'The example is missing. Run Run.cmd -Mode Prepare to create it.'
    }
    "Mode: $Mode`nBlender: $BlenderPath`nStarted: $([DateTime]::UtcNow.ToString('o'))" | Set-Content -LiteralPath $logPath
    & $BlenderPath @arguments *>&1 | Tee-Object -FilePath $logPath -Append
    if ($LASTEXITCODE -ne 0) {
        throw "Blender exited with code $LASTEXITCODE. See '$logPath'."
    }
}
catch {
    $_ | Out-String | Add-Content -LiteralPath $logPath
    Write-Error $_
    exit 1
}
