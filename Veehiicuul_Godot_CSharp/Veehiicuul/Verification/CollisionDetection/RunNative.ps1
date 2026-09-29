Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectDirectory = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$adapterPath = Join-Path $projectDirectory 'Source\Main_GodotAdapter.cs'
$projectPath = Join-Path $projectDirectory 'Veehiicuul_Godot_CSharp.csproj'
$godotDirectory = Join-Path $env:UserProfile 'Program\Godot_v4.7.2-stable_mono_win64'
$godotPath = Join-Path $godotDirectory 'Godot_v4.7.2-stable_mono_win64_console.exe'
foreach ($required in @($godotPath, (Join-Path $godotDirectory '_sc_'), (Join-Path $godotDirectory 'editor_data\export_templates\4.7.2.stable.mono\windows_release_x86_64.exe'))) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required Godot installation file missing: $required" }
}
if (Get-Process -Name 'Godot*' -ErrorAction SilentlyContinue) { throw 'Close Godot before temporarily building the native analysis adapter.' }
$logDirectory = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$original = [System.IO.File]::ReadAllBytes($adapterPath)
[System.IO.File]::WriteAllBytes((Join-Path $logDirectory 'OriginalAdapter.cs.txt'), $original)
Start-Transcript -LiteralPath (Join-Path $logDirectory 'NativeAnalysis.log') | Out-Null
try {
    [System.IO.File]::WriteAllBytes($adapterPath, [System.IO.File]::ReadAllBytes((Join-Path $PSScriptRoot 'NativePerformanceAnalysis.cs.txt')))
    & dotnet build $projectPath --configuration Debug --nologo --no-restore -p:Optimize=true -p:DebugType=None
    if ($LASTEXITCODE -ne 0) { throw "Native harness build failed: $LASTEXITCODE" }
    & $godotPath --headless --path $projectDirectory --log-file (Join-Path $logDirectory 'Godot.log')
    if ($LASTEXITCODE -ne 0) { throw "Native harness failed: $LASTEXITCODE" }
    if (-not (Select-String -LiteralPath (Join-Path $logDirectory 'Godot.log') -SimpleMatch 'PASS: native collision analysis completed.' -Quiet)) { throw 'Native completion marker missing.' }
} finally {
    [System.IO.File]::WriteAllBytes($adapterPath, $original)
    & dotnet build $projectPath --configuration Debug --nologo --no-restore
    $restoreExitCode = $LASTEXITCODE
    Stop-Transcript | Out-Null
    if ($restoreExitCode -ne 0) { throw "Source was restored, but normal Debug rebuild failed: $restoreExitCode" }
}
