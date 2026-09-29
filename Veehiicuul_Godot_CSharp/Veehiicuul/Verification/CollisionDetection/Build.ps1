Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$logDirectory = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
Start-Transcript -LiteralPath (Join-Path $logDirectory 'Build.log') | Out-Null
try {
    & dotnet build (Join-Path $PSScriptRoot 'CollisionDetectionVerification.csproj') --configuration Release --nologo -warnaserror
    if ($LASTEXITCODE -ne 0) { throw "Build exited with code $LASTEXITCODE." }
} finally {
    Stop-Transcript | Out-Null
}
