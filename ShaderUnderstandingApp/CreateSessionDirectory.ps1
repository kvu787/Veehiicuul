Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$logParent = Join-Path $PSScriptRoot 'LogOutput'
New-Item -ItemType Directory -Path $logParent -Force | Out-Null
while ($true) {
    $directory = Join-Path $logParent (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss')
    try {
        # Creating without -Force reserves the timestamp atomically, including
        # when a build and several launchers start in the same second.
        New-Item -ItemType Directory -Path $directory | Out-Null
        return $directory
    } catch {
        if (-not (Test-Path -LiteralPath $directory -PathType Container)) { throw }
        Start-Sleep -Milliseconds 40
    }
}
