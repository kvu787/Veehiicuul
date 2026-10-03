param(
    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$transcriptStarted = $false
try {
    $logFolderPath = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
    New-Item -ItemType Directory -Path $logFolderPath -ErrorAction Stop | Out-Null
    Start-Transcript -LiteralPath (Join-Path $logFolderPath 'Launcher.log') | Out-Null
    $transcriptStarted = $true
    Write-Host "Session logs: $logFolderPath"

    $presetName = if ($Configuration -ieq 'Debug') { 'RunDebug' } else { 'RunRelease' }
    $presets = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'CMakePresets.json') -Raw | ConvertFrom-Json
    $configurePreset = $presets.configurePresets | Where-Object { $_.name -ceq $presetName }
    $buildDirectory = $configurePreset.binaryDir.Replace('${sourceDir}', $PSScriptRoot)
    $applicationPath = Join-Path $buildDirectory 'Veehiicuul.exe'
    if (-not (Test-Path -LiteralPath $applicationPath -PathType Leaf)) {
        throw "No $Configuration build was found at '$applicationPath'. Run Build.cmd -Configuration $Configuration first."
    }

    $previousLogDirectory = $env:VEEHIICUUL_LOG_DIRECTORY
    try {
        $env:VEEHIICUUL_LOG_DIRECTORY = $logFolderPath
        $applicationProcess = Start-Process -FilePath $applicationPath -WorkingDirectory $PSScriptRoot -Wait -PassThru
    }
    finally {
        $env:VEEHIICUUL_LOG_DIRECTORY = $previousLogDirectory
    }
    if ($applicationProcess.ExitCode -ne 0) {
        throw "Application exited with code $($applicationProcess.ExitCode)."
    }
}
catch {
    Write-Host
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host 'Launch failed. Review the messages above.' -ForegroundColor Red
    exit 1
}
finally {
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
