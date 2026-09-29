# Measures which deliberately injected defects in the collision manager and in the
# vehicle footprint the engine-side checks detect.
#
# The collision sources, this harness, and its engine project are copied into an
# isolated folder under the repository's ignored Build directory. Each mutant changes
# the copied sources only. The copy runs in the Godot editor binary without a window.
param(
    [string] $OutputDirectory = '',
    [string[]] $Only = @()
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# powershell.exe -File passes a comma-separated list as one string.
$Only = @($Only | ForEach-Object { $_ -split ',' } | Where-Object { $_ -ne '' })

$projectDirectory = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repositoryDirectory = [System.IO.Path]::GetFullPath((Join-Path $projectDirectory '..\..'))
$godot = Join-Path $env:UserProfile 'Program\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
if (-not (Test-Path -LiteralPath $godot -PathType Leaf)) { throw "Godot 4.7.2 .NET was not found at $godot" }
if ($OutputDirectory -eq '') {
    $OutputDirectory = Join-Path $repositoryDirectory ('Build\CollisionFromScratchNativeMutation\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
}
$workDirectory = Join-Path $OutputDirectory 'Veehiicuul'
$pristineDirectory = Join-Path $OutputDirectory 'Pristine'
$sourceRelative = 'Source\GameDataAndLogic'
$harnessRelative = 'Verification\CollisionDetectionFromScratch'

$manager = 'CollisionManager.cs'
$footprint = 'CollisionDetection\VehicleCollisionFootprint.cs'

$mutants = @(
    @{ Name = 'Unmodified'; Area = 'None'; Description = 'Control run without any change.'; Edits = @() },
    @{ Name = 'PositionZNotNegated'; Area = 'Manager pose'; Description = 'Collision Y takes engine Z without negation.'; Edits = @(
        @{ File = $manager; Find = 'RectanglePose pose = new(position.X, -position.Z, -rotationRadians);'; Replace = 'RectanglePose pose = new(position.X, position.Z, -rotationRadians);' }) },
    @{ Name = 'YawNotNegated'; Area = 'Manager pose'; Description = 'Clockwise yaw takes the engine yaw without negation.'; Edits = @(
        @{ File = $manager; Find = 'RectanglePose pose = new(position.X, -position.Z, -rotationRadians);'; Replace = 'RectanglePose pose = new(position.X, -position.Z, rotationRadians);' }) },
    @{ Name = 'RearShortenedInsteadOfFront'; Area = 'Manager bounds'; Description = 'The shortening is applied to the rear limit.'; Edits = @(
        @{ File = $manager; Find = 'new(raw.MinX, raw.MinY + ShortenColliderFront, raw.MaxX, raw.MaxY - ShortenColliderRear);'; Replace = 'new(raw.MinX, raw.MinY + ShortenColliderRear, raw.MaxX, raw.MaxY - ShortenColliderFront);' }) },
    @{ Name = 'FrontNotShortened'; Area = 'Manager bounds'; Description = 'The front limit is not shortened.'; Edits = @(
        @{ File = $manager; Find = 'private const float ShortenColliderFront = 0.165f;'; Replace = 'private const float ShortenColliderFront = 0.0f;' }) },
    @{ Name = 'AlwaysFirstVehicleBounds'; Area = 'Manager bounds'; Description = 'Every vehicle is tested with the first vehicle''s bounds.'; Edits = @(
        @{ File = $manager; Find = 'this._detector.IsColliding(this._vehicleBounds[carIndex], pose);'; Replace = 'this._detector.IsColliding(this._vehicleBounds[0], pose);' }) },
    @{ Name = 'CacheIgnoresYaw'; Area = 'Manager cache'; Description = 'The previous answer is reused when only the yaw changed.'; Edits = @(
        @{ File = $manager; Find = "            && -position.Z == this._previousPose.PositionY`n            && -rotationRadians == this._previousPose.RotationRadians) {"; Replace = "            && -position.Z == this._previousPose.PositionY) {" }) },
    @{ Name = 'CacheIgnoresPositionZ'; Area = 'Manager cache'; Description = 'The previous answer is reused when only engine Z changed.'; Edits = @(
        @{ File = $manager; Find = "            && position.X == this._previousPose.PositionX`n            && -position.Z == this._previousPose.PositionY`n"; Replace = "            && position.X == this._previousPose.PositionX`n" }) },
    @{ Name = 'CacheIgnoresVehicle'; Area = 'Manager cache'; Description = 'The previous answer is reused after another vehicle was selected.'; Edits = @(
        @{ File = $manager; Find = 'if (carIndex == this._previousCarIndex'; Replace = 'if (this._previousCarIndex >= 0' }) },
    @{ Name = 'ChildTransformsIgnored'; Area = 'Footprint'; Description = 'Meshes are treated as if they sat at the vehicle origin.'; Edits = @(
        @{ File = $footprint; Find = 'Transform3D childToVehicle = child is Node3D child3D ? nodeToVehicle * child3D.Transform : nodeToVehicle;'; Replace = 'Transform3D childToVehicle = nodeToVehicle;' }) },
    @{ Name = 'TransformsComposedInReverse'; Area = 'Footprint'; Description = 'Parent and child transforms are multiplied in the wrong order.'; Edits = @(
        @{ File = $footprint; Find = 'child is Node3D child3D ? nodeToVehicle * child3D.Transform : nodeToVehicle;'; Replace = 'child is Node3D child3D ? child3D.Transform * nodeToVehicle : nodeToVehicle;' }) },
    @{ Name = 'HiddenMeshesSkipped'; Area = 'Footprint'; Description = 'Meshes that are not visible are left out.'; Edits = @(
        @{ File = $footprint; Find = 'if (node is MeshInstance3D meshInstance && meshInstance.Mesh is not null) {'; Replace = 'if (node is MeshInstance3D meshInstance && meshInstance.Mesh is not null && meshInstance.Visible) {' }) },
    @{ Name = 'CustomBoundsIgnored'; Area = 'Footprint'; Description = 'Custom bounds never replace the mesh bounds.'; Edits = @(
        @{ File = $footprint; Find = 'Aabb bounds = customBounds == default ? meshInstance.GetAabb() : customBounds;'; Replace = 'Aabb bounds = customBounds == default ? meshInstance.GetAabb() : meshInstance.GetAabb();' }) },
    @{ Name = 'LengthScaledByWidthScale'; Area = 'Scaled bounds'; Description = 'The Z scale is read from the X axis.'; Edits = @(
        @{ File = $footprint; Find = 'float zScale = basis.Z.Length();'; Replace = 'float zScale = basis.X.Length();' }) },
    @{ Name = 'CollisionYNotNegated'; Area = 'Scaled bounds'; Description = 'Collision Y takes vehicle Z without negation.'; Edits = @(
        @{ File = $footprint; Find = "        float y0 = -this.MaxZ * zScale;`n        float y1 = -this.MinZ * zScale;"; Replace = "        float y0 = this.MinZ * zScale;`n        float y1 = this.MaxZ * zScale;" }) },
    @{ Name = 'RootTransformIncluded'; Area = 'Footprint'; Description = 'The vehicle''s own transform enters its local footprint.'; Edits = @(
        @{ File = $footprint; Find = 'IncludeNode(vehicleRoot, Transform3D.Identity, ref foundBounds, ref minX, ref minZ, ref maxX, ref maxZ);'; Replace = 'IncludeNode(vehicleRoot, vehicleRoot.Transform, ref foundBounds, ref minX, ref minZ, ref maxX, ref maxZ);' }) },
    @{ Name = 'ZeroScaleAccepted'; Area = 'Scaled bounds'; Description = 'A zero planar scale is accepted.'; Edits = @(
        @{ File = $footprint; Find = "            || !(xScale > 0f)`n            || !(zScale > 0f)) {"; Replace = "            || float.IsNaN(xScale)) {" }) }
)

function Copy-Inputs {
    param([string] $Destination)
    New-Item -ItemType Directory -Path (Join-Path $Destination "$sourceRelative\CollisionDetection") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $Destination $harnessRelative) -Force | Out-Null
    Copy-Item -Path (Join-Path $projectDirectory "$sourceRelative\CollisionDetection\*.cs") -Destination (Join-Path $Destination "$sourceRelative\CollisionDetection")
    Copy-Item -LiteralPath (Join-Path $projectDirectory "$sourceRelative\CollisionManager.cs") -Destination (Join-Path $Destination $sourceRelative)
    Copy-Item -LiteralPath (Join-Path $projectDirectory "$harnessRelative\Source") -Destination (Join-Path $Destination $harnessRelative) -Recurse
    $native = Join-Path $Destination "$harnessRelative\NativeProject"
    New-Item -ItemType Directory -Path $native -Force | Out-Null
    foreach ($name in @('project.godot', 'Main.tscn', 'CollisionDetectionNative.csproj', 'CollisionDetectionNative.slnx')) {
        Copy-Item -LiteralPath (Join-Path $projectDirectory "$harnessRelative\NativeProject\$name") -Destination $native
    }
    Copy-Item -LiteralPath (Join-Path $projectDirectory "$harnessRelative\NativeProject\Source") -Destination $native -Recurse
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
Copy-Inputs -Destination $pristineDirectory
Copy-Inputs -Destination $workDirectory
$nativeDirectory = Join-Path $workDirectory "$harnessRelative\NativeProject"
$nativeProject = Join-Path $nativeDirectory 'CollisionDetectionNative.csproj'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$results = @()

# The isolated copy needs its own resource cache before the first run.
& $godot --headless --editor --path $nativeDirectory --import --log-file (Join-Path $OutputDirectory 'Import.log') | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Import of the isolated copy failed: $LASTEXITCODE" }

foreach ($mutant in $mutants) {
    if ($Only.Count -gt 0 -and $Only -notcontains $mutant.Name) { continue }
    Copy-Item -Path (Join-Path $pristineDirectory "$sourceRelative\CollisionDetection\*.cs") -Destination (Join-Path $workDirectory "$sourceRelative\CollisionDetection") -Force
    Copy-Item -LiteralPath (Join-Path $pristineDirectory "$sourceRelative\CollisionManager.cs") -Destination (Join-Path $workDirectory $sourceRelative) -Force
    foreach ($edit in $mutant.Edits) {
        $path = Join-Path $workDirectory (Join-Path $sourceRelative $edit.File)
        $source = [System.IO.File]::ReadAllText($path) -replace "`r`n", "`n"
        $find = $edit.Find -replace "`r`n", "`n"
        $replace = $edit.Replace -replace "`r`n", "`n"
        $first = $source.IndexOf($find, [System.StringComparison]::Ordinal)
        if ($first -lt 0) { throw "Mutant $($mutant.Name): text not found in $($edit.File)." }
        if ($source.IndexOf($find, $first + 1, [System.StringComparison]::Ordinal) -ge 0) {
            throw "Mutant $($mutant.Name): text occurs more than once in $($edit.File)."
        }
        $source = $source.Substring(0, $first) + $replace + $source.Substring($first + $find.Length)
        [System.IO.File]::WriteAllText($path, $source, $utf8)
    }
    $mutantDirectory = Join-Path $OutputDirectory $mutant.Name
    New-Item -ItemType Directory -Path $mutantDirectory -Force | Out-Null
    # Analyzer findings caused by a mutation are not the subject here.
    $buildLog = & dotnet build $nativeProject --configuration Debug --nologo -p:NuGetAudit=false -p:RunAnalyzers=false -p:EnforceCodeStyleInBuild=false 2>&1
    $buildCode = $LASTEXITCODE
    [System.IO.File]::WriteAllText((Join-Path $mutantDirectory 'Build.log'), ($buildLog | Out-String), $utf8)
    $record = [ordered]@{
        Name = $mutant.Name; Area = $mutant.Area; Description = $mutant.Description
        Build = 'Pass'; Outcome = ''; FailedSuites = @(); Failures = 0
    }
    if ($buildCode -ne 0) {
        $record.Build = 'Fail'
    } else {
        $jsonPath = Join-Path $mutantDirectory 'NativeChecks.json'
        & $godot --headless --path $nativeDirectory --log-file (Join-Path $mutantDirectory 'NativeChecks.log') -- --mode=checks "--output=$jsonPath" | Out-Null
        $code = $LASTEXITCODE
        $record.Outcome = if ($code -eq 0) { 'Pass' } elseif ($code -eq 1) { 'Fail' } else { 'Error' }
        if (Test-Path -LiteralPath $jsonPath) {
            $checks = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
            $record.Failures = $checks.Failures
            $record.FailedSuites = @($checks.Suites | Where-Object { $_.Failures -gt 0 } | ForEach-Object { $_.Name })
        }
    }
    Write-Host "Mutant $($mutant.Name): build=$($record.Build) outcome=$($record.Outcome) failedSuites=$($record.FailedSuites.Count)"
    $results += [pscustomobject] $record
    [System.IO.File]::WriteAllText((Join-Path $OutputDirectory 'NativeMutationResults.json'), ($results | ConvertTo-Json -Depth 4), $utf8)
}
Write-Host "Mutation results: $(Join-Path $OutputDirectory 'NativeMutationResults.json')"
