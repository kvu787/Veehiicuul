Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$sourceDirectory = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repositoryDirectory = [System.IO.Path]::GetFullPath((Join-Path $sourceDirectory '..\..'))
$godotDirectory = Join-Path $env:UserProfile 'Program\Godot_v4.7.2-stable_mono_win64'
$godotPath = Join-Path $godotDirectory 'Godot_v4.7.2-stable_mono_win64_console.exe'
foreach ($required in @($godotPath, (Join-Path $godotDirectory '_sc_'), (Join-Path $godotDirectory 'editor_data\export_templates\4.7.2.stable.mono\windows_release_x86_64.exe'))) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required Godot installation file missing: $required" }
}
$timestamp = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$logDirectory = Join-Path $PSScriptRoot ('MyLogOutput\' + $timestamp)
$projectDirectory = Join-Path $repositoryDirectory ('Build\CollisionNativeAnalysis\' + $timestamp)
New-Item -ItemType Directory -Path $logDirectory, $projectDirectory -Force | Out-Null
Start-Transcript -LiteralPath (Join-Path $logDirectory 'NativeAnalysis.log') | Out-Null
try {
    # Copy only the inputs and imported resource cache. Never modify the open
    # editor's sources, Debug assembly, or resource cache.
    foreach ($directory in @('Source', 'Resources', 'Scenes', 'Tracks')) {
        Copy-Item -LiteralPath (Join-Path $sourceDirectory $directory) -Destination $projectDirectory -Recurse
    }
    foreach ($name in @('project.godot', 'Veehiicuul_Godot_CSharp.csproj')) {
        Copy-Item -LiteralPath (Join-Path $sourceDirectory $name) -Destination $projectDirectory
    }
    $cacheDirectory = Join-Path $projectDirectory '.godot'
    New-Item -ItemType Directory -Path $cacheDirectory -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $sourceDirectory '.godot\imported') -Destination $cacheDirectory -Recurse
    Copy-Item -LiteralPath (Join-Path $sourceDirectory '.godot\uid_cache.bin') -Destination $cacheDirectory
    $baselineDirectory = Join-Path $logDirectory 'Baseline'
    New-Item -ItemType Directory -Path $baselineDirectory -Force | Out-Null
    foreach ($name in @('TrackCollisionDetector', 'VehicleCollisionFootprint', 'CollisionManager')) {
        $relative = if ($name -eq 'CollisionManager') { 'Source/GameDataAndLogic/CollisionManager.cs' } else { "Source/GameDataAndLogic/CollisionDetection/$name.cs" }
        $historical = & git -C $sourceDirectory show "afcad73:Veehiicuul_Godot_CSharp/Veehiicuul/$relative"
        if ($LASTEXITCODE -ne 0) { throw 'Historical collision baseline is unavailable.' }
        $source = ($historical -join "`n")
        foreach ($type in @('TrackCollisionDetector', 'VehicleCollisionFootprint', 'CollisionManager', 'RectangleLocalBounds', 'RectanglePose', 'CollisionMath')) {
            $source = $source.Replace($type, ('Baseline' + $type))
        }
        [System.IO.File]::WriteAllText((Join-Path $baselineDirectory ($name + '.cs')), $source + "`n", [System.Text.UTF8Encoding]::new($false))
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'NativePerformanceAnalysis.cs.txt') -Destination (Join-Path $projectDirectory 'Source\Main_GodotAdapter.cs')
    & dotnet build (Join-Path $projectDirectory 'Veehiicuul_Godot_CSharp.csproj') --configuration Debug --nologo -p:Optimize=true -p:DebugType=None -p:NuGetAudit=false "-p:CollisionBaselineDirectory=$baselineDirectory"
    if ($LASTEXITCODE -ne 0) { throw "Native harness build failed: $LASTEXITCODE" }
    & $godotPath --headless --path $projectDirectory --log-file (Join-Path $logDirectory 'Godot.log')
    if ($LASTEXITCODE -ne 0) { throw "Native harness failed: $LASTEXITCODE" }
    if (-not (Select-String -LiteralPath (Join-Path $logDirectory 'Godot.log') -SimpleMatch 'PASS: native collision analysis completed.' -Quiet)) { throw 'Native completion marker missing.' }
    Write-Host "Native results: $logDirectory"
} finally {
    Stop-Transcript | Out-Null
}
