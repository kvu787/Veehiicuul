param(
    [switch] $VerificationOnly,
    [switch] $DisableTieredCompilation,
    [ValidateRange(1, 10)] [int] $Repetitions = 3
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$executable = Join-Path $PSScriptRoot 'bin\Release\net10.0\CollisionDetectionVerification.exe'
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw 'Run Build.cmd first.' }
$logDirectory = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$previousTiered = [Environment]::GetEnvironmentVariable('DOTNET_TieredCompilation', 'Process')
Start-Transcript -LiteralPath (Join-Path $logDirectory 'Run.log') | Out-Null
try {
    if ($DisableTieredCompilation) { $env:DOTNET_TieredCompilation = '0' }
    & $executable
    if ($LASTEXITCODE -ne 0) { throw "Verification exited with code $LASTEXITCODE." }
    if (-not $VerificationOnly) {
        for ($repetition = 1; $repetition -le $Repetitions; ++$repetition) {
            & $executable --performance "--output=$logDirectory\Performance$repetition.json"
            if ($LASTEXITCODE -ne 0) { throw "Benchmark exited with code $LASTEXITCODE." }
        }
    }
    Write-Host "Results: $logDirectory"
} finally {
    [Environment]::SetEnvironmentVariable('DOTNET_TieredCompilation', $previousTiered, 'Process')
    Stop-Transcript | Out-Null
}
