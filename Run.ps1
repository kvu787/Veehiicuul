param(
    [switch] $BuildOnly,
    [switch] $Test,
    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

& (Join-Path $PSScriptRoot 'Cpp\Run.ps1') @PSBoundParameters
exit $LASTEXITCODE
