Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Checked {
    param([string] $Program, [string[]] $Arguments)
    & $Program @Arguments
    if ($LASTEXITCODE -ne 0) { throw "'$Program' exited with code $LASTEXITCODE." }
}

$transcriptStarted = $false
try {
    $logDirectory = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
    Start-Transcript -LiteralPath (Join-Path $logDirectory 'Build.log') | Out-Null
    $transcriptStarted = $true
    $applicationDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\Veehiicuul'))
    $engineDirectory = Join-Path $PSScriptRoot 'MyBuildOutput\EngineProject'
    New-Item -ItemType Directory -Path $engineDirectory -Force | Out-Null
    # The isolated snapshot runs the unmodified application classes and real imported assets.
    # Verification code is never compiled into the production application.
    foreach ($name in @('Source', 'Tracks', 'Resources', 'Veehiicuul_Godot_CSharp.csproj', 'Veehiicuul_Godot_CSharp.slnx', 'project.godot', 'export_presets.cfg', 'Build.ps1')) {
        Copy-Item -LiteralPath (Join-Path $applicationDirectory $name) -Destination $engineDirectory -Recurse -Force
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Engine\EngineReview.cs') -Destination (Join-Path $engineDirectory 'EngineReview.cs') -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Engine\Review.tscn') -Destination (Join-Path $engineDirectory 'Main.tscn') -Force
    $configurationPath = Join-Path $engineDirectory 'project.godot'
    $configuration = [IO.File]::ReadAllText($configurationPath).Replace('window/size/mode=4', 'window/size/mode=0')
    [IO.File]::WriteAllText($configurationPath, $configuration, [Text.UTF8Encoding]::new($false))
    Invoke-Checked 'powershell.exe' @('-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $engineDirectory 'Build.ps1'))
    Invoke-Checked 'dotnet' @('build', (Join-Path $PSScriptRoot 'CollisionReview.csproj'), '--configuration', 'Release', '--nologo', '-warnaserror')
}
catch {
    Write-Host $_.Exception.ToString() -ForegroundColor Red
    exit 1
}
finally {
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
