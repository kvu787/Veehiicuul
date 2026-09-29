# Measures which deliberately injected defects the existing collision verification detects.
#
# Production sources and the existing harness are copied into an isolated folder under the
# repository's ignored Build directory. Each mutant changes the copied detector only. The
# repository's own sources, harness, and build output are never modified.
param(
    [string] $OutputDirectory = '',
    [string[]] $Only = @(),
    [switch] $SkipExtendedSuite,
    # Path, relative to the application folder, of a collider file that takes the place
    # of the Ribeye collider data in the isolated copy. The suites then judge that track.
    [string] $SubstituteRibeyeData = ''
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# powershell.exe -File passes a comma-separated list as one string.
$Only = @($Only | ForEach-Object { $_ -split ',' } | Where-Object { $_ -ne '' })

$projectDirectory =[System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$repositoryDirectory = [System.IO.Path]::GetFullPath((Join-Path $projectDirectory '..\..'))
if ($OutputDirectory -eq '') {
    $OutputDirectory = Join-Path $repositoryDirectory ('Build\CollisionMutationAnalysis\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
}
$workDirectory = Join-Path $OutputDirectory 'Veehiicuul'
$pristineDirectory = Join-Path $OutputDirectory 'Pristine'
$detectorRelative = 'Source\GameDataAndLogic\CollisionDetection'
$harnessRelative = 'Verification\CollisionDetection'

$detector = 'TrackCollisionDetector.cs'
$grid = 'TrackCollisionDetector.ExpandedGrid.cs'

# Each edit must match the pristine source exactly once, so a stale mutant fails loudly.
$mutants = @(
    @{ Name = 'Unmodified'; Path = 'None'; Description = 'Control run without any change.'; Edits = @() },
    @{ Name = 'RadiusIgnoresShorterExtent'; Path = 'Expanded grid'; Description = 'Expansion radius uses the larger half extent instead of the corner distance.'; Edits = @(
        @{ File = $grid; Find = 'double radius = Math.Sqrt((double)maximumLocalX * maximumLocalX + (double)maximumLocalY * maximumLocalY);'; Replace = 'double radius = Math.Max(maximumLocalX, maximumLocalY);' }) },
    @{ Name = 'RadiusShortByTenth'; Path = 'Expanded grid'; Description = 'Expansion radius is 0.1 too small.'; Edits = @(
        @{ File = $grid; Find = 'radius = Math.BitIncrement(radius + roundingMargin);'; Replace = 'radius = radius - 0.1;' }) },
    @{ Name = 'RadiusShortByHundredth'; Path = 'Expanded grid'; Description = 'Expansion radius is 0.01 too small.'; Edits = @(
        @{ File = $grid; Find = 'radius = Math.BitIncrement(radius + roundingMargin);'; Replace = 'radius = radius - 0.01;' }) },
    @{ Name = 'RadiusShortByThousandth'; Path = 'Expanded grid'; Description = 'Expansion radius is 0.001 too small.'; Edits = @(
        @{ File = $grid; Find = 'radius = Math.BitIncrement(radius + roundingMargin);'; Replace = 'radius = radius - 0.001;' }) },
    @{ Name = 'RadiusWithoutRoundingMargin'; Path = 'Expanded grid'; Description = 'Expansion radius omits the binary32 rounding margin.'; Edits = @(
        @{ File = $grid; Find = 'radius = Math.BitIncrement(radius + roundingMargin);'; Replace = 'radius = radius + 0.0 * roundingMargin;' }) },
    @{ Name = 'MaximumColumnOmitsRadius'; Path = 'Expanded grid'; Description = 'Edge coverage omits the radius on the maximum X side.'; Edits = @(
        @{ File = $grid; Find = 'int maxX = Map(Math.BitIncrement((double)bounds.MaxX + radius), originX, inverseCellSize, columnCount);'; Replace = 'int maxX = Map(Math.BitIncrement((double)bounds.MaxX), originX, inverseCellSize, columnCount);' }) },
    @{ Name = 'LastCandidateSkipped'; Path = 'Expanded grid'; Description = 'The origin-cell query skips the final candidate edge of each cell.'; Edits = @(
        @{ File = $detector; Find = "            RectangleQuad expandedRectangle = RectangleTransformer.Transform(localBounds, pose);`n            int end = range.Offset + range.Count;"; Replace = "            RectangleQuad expandedRectangle = RectangleTransformer.Transform(localBounds, pose);`n            int end = range.Offset + range.Count - 1;" }) },
    @{ Name = 'SupportsIgnoresBoundsLimit'; Path = 'Expanded grid'; Description = 'Origin-cell lookup accepts footprints larger than the indexed footprint.'; Edits = @(
        @{ File = $grid; Find = "                && bounds.MinX >= -this._maximumLocalX && bounds.MaxX <= this._maximumLocalX`n                && bounds.MinY >= -this._maximumLocalY && bounds.MaxY <= this._maximumLocalY`n"; Replace = "                && this._maximumLocalX > 0f && this._maximumLocalY > 0f`n" }) },
    @{ Name = 'CenteredLookupUsesOrigin'; Path = 'Expanded grid'; Description = 'Shifted-origin lookup uses the pose position instead of the rectangle center.'; Edits = @(
        @{ File = $detector; Find = "                CellRange range = this._expandedGrid.GetRange(`n                    ((double)rectangle.P0.X + rectangle.P2.X) * 0.5,`n                    ((double)rectangle.P0.Y + rectangle.P2.Y) * 0.5);"; Replace = "                CellRange range = this._expandedGrid.GetRange(`n                    pose.PositionX,`n                    pose.PositionY);" }) },
    @{ Name = 'ClosingEdgeTargetsSecondVertex'; Path = 'Edge construction'; Description = 'Each closing edge ends at the second vertex instead of the first.'; Edits = @(
        @{ File = $detector; Find = 'vertexIndex + 1 == vertices.Count ? 0 : vertexIndex + 1];'; Replace = 'vertexIndex + 1 == vertices.Count ? 1 : vertexIndex + 1];' }) },
    @{ Name = 'RotationDirectionReversed'; Path = 'Rectangle transform'; Description = 'The rectangle rotates counterclockwise for positive yaw.'; Edits = @(
        @{ File = $detector; Find = 'double worldXDouble = (double)pose.PositionX + xCosine + ySine;'; Replace = 'double worldXDouble = (double)pose.PositionX + xCosine - ySine;' },
        @{ File = $detector; Find = 'double worldYDouble = (double)pose.PositionY - xSine + yCosine;'; Replace = 'double worldYDouble = (double)pose.PositionY + xSine + yCosine;' }) },
    @{ Name = 'FourthSideNotTested'; Path = 'Narrow phase'; Description = 'The rectangle side from the fourth corner to the first is never tested.'; Edits = @(
        @{ File = $detector; Find = "                || RobustPredicates.SegmentsIntersect(a, b, this.P2, this.P3)`n                || RobustPredicates.SegmentsIntersect(a, b, this.P3, this.P0);"; Replace = "                || RobustPredicates.SegmentsIntersect(a, b, this.P2, this.P3);" }) },
    @{ Name = 'EndpointContactExcluded'; Path = 'Narrow phase'; Description = 'A collinear point at the minimum X end of a segment is not on it.'; Edits = @(
        @{ File = $detector; Find = 'return point.X >= Math.Min(a.X, b.X)'; Replace = 'return point.X > Math.Min(a.X, b.X)' }) },
    @{ Name = 'CrossingUsesEitherStraddle'; Path = 'Narrow phase'; Description = 'A crossing is reported when either segment straddles the other line.'; Edits = @(
        @{ File = $detector; Find = 'return abc != abd && cda != cdb;'; Replace = 'return abc != abd || cda != cdb;' }) },
    @{ Name = 'TouchingBoundsDoNotOverlap'; Path = 'Narrow phase'; Description = 'Bounding boxes that only touch are treated as separate.'; Edits = @(
        @{ File = $detector; Find = "            return this.MinX <= other.MaxX`n                && this.MaxX >= other.MinX`n                && this.MinY <= other.MaxY`n                && this.MaxY >= other.MinY;"; Replace = "            return this.MinX < other.MaxX`n                && this.MaxX > other.MinX`n                && this.MinY < other.MaxY`n                && this.MaxY > other.MinY;" }) },
    @{ Name = 'ErrorBoundFarTooSmall'; Path = 'Orientation filter'; Description = 'The floating-point filter trusts determinants it cannot certify.'; Edits = @(
        @{ File = $detector; Find = 'private const double CcwErrorBoundA = 3.3306690738754716e-16;'; Replace = 'private const double CcwErrorBoundA = 1e-30;' }) },
    @{ Name = 'ExactFallbackReturnsZero'; Path = 'Orientation filter'; Description = 'Uncertain orientations are reported as collinear without exact arithmetic.'; Edits = @(
        @{ File = $detector; Find = "            if (-determinant >= errorBound) {`n                return -1;`n            }`n`n            return ExactOrientationSign(a, b, c);"; Replace = "            if (-determinant >= errorBound) {`n                return -1;`n            }`n`n            return 0;" }) },
    @{ Name = 'BoundedIntegerShiftTooWide'; Path = 'Exact arithmetic'; Description = 'The 128-bit path accepts exponent spans that overflow 64-bit operands.'; Edits = @(
        @{ File = $detector; Find = 'if (shift > 37) {'; Replace = 'if (shift > 60) {' }) },
    @{ Name = 'SubnormalCheckRemoved'; Path = 'Orientation filter'; Description = 'Subnormal inputs use the filtered path. Expected to be behavior preserving.'; Edits = @(
        @{ File = $detector; Find = "            if (IsSubnormal(a.X)`n                || IsSubnormal(a.Y)`n                || IsSubnormal(b.X)`n                || IsSubnormal(b.Y)`n                || IsSubnormal(c.X)`n                || IsSubnormal(c.Y)) {`n                return ExactOrientationSign(a, b, c);`n            }"; Replace = "            _ = IsSubnormal(a.X);" }) },
    @{ Name = 'CenterGridExpansionDropped'; Path = 'Center grid fallback'; Description = 'Fallback grid queries are not expanded by the largest edge half extent.'; Edits = @(
        @{ File = $detector; Find = "(double)queryBounds.MinX - this.MaximumHalfExtentX);"; Replace = "(double)queryBounds.MinX);" },
        @{ File = $detector; Find = "(double)queryBounds.MinY - this.MaximumHalfExtentY);"; Replace = "(double)queryBounds.MinY);" },
        @{ File = $detector; Find = "(double)queryBounds.MaxX + this.MaximumHalfExtentX);"; Replace = "(double)queryBounds.MaxX);" },
        @{ File = $detector; Find = "(double)queryBounds.MaxY + this.MaximumHalfExtentY);"; Replace = "(double)queryBounds.MaxY);" }) },
    @{ Name = 'TreeVisitsLeftChildOnly'; Path = 'Long-edge tree fallback'; Description = 'Tree traversal never descends into right children.'; Edits = @(
        @{ File = $detector; Find = "            return this.QueryNode(node.Left, rectangle)`n                || this.QueryNode(node.Right, rectangle);"; Replace = "            return this.QueryNode(node.Left, rectangle);" }) },
    @{ Name = 'TreeLeafSkipsLastEdge'; Path = 'Long-edge tree fallback'; Description = 'Each tree leaf skips its final edge.'; Edits = @(
        @{ File = $detector; Find = "                int end = node.Start + node.Count;"; Replace = "                int end = node.Start + node.Count - 1;" }) },
    @{ Name = 'SparseCellsSkipLastRow'; Path = 'Center grid fallback'; Description = 'Sparse fallback queries skip the final covered row.'; Edits = @(
        @{ File = $detector; Find = "                ?? throw new InvalidOperationException(`"A nonempty collision grid must have cell storage.`");`n            for (int row = minRow; row <= maxRow; ++row) {"; Replace = "                ?? throw new InvalidOperationException(`"A nonempty collision grid must have cell storage.`");`n            for (int row = minRow; row < maxRow; ++row) {" }) }
)

function Copy-Inputs {
    param([string] $Destination)
    New-Item -ItemType Directory -Path (Join-Path $Destination $detectorRelative) -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $Destination $harnessRelative) -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $Destination 'Tracks\TrackData') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $Destination 'Tracks\Ribeye') -Force | Out-Null
    Copy-Item -Path (Join-Path $projectDirectory "$detectorRelative\*.cs") -Destination (Join-Path $Destination $detectorRelative)
    foreach ($pattern in @('*.cs', '*.csproj')) {
        Copy-Item -Path (Join-Path $projectDirectory "$harnessRelative\$pattern") -Destination (Join-Path $Destination $harnessRelative)
    }
    Copy-Item -LiteralPath (Join-Path $projectDirectory 'Tracks\TrackData\Track001_ColliderData.json') -Destination (Join-Path $Destination 'Tracks\TrackData')
    $ribeyeSource = 'Tracks\Ribeye\Ribeye_ColliderData.json'
    if ($SubstituteRibeyeData -ne '') { $ribeyeSource = $SubstituteRibeyeData }
    Copy-Item -LiteralPath (Join-Path $projectDirectory $ribeyeSource) -Destination (Join-Path $Destination 'Tracks\Ribeye\Ribeye_ColliderData.json')
}

function Invoke-Stage {
    param([string] $Executable, [string[]] $Arguments, [string] $LogPath, [int] $TimeoutSeconds)
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo.FileName = $Executable
    $process.StartInfo.Arguments = ($Arguments | ForEach-Object { '"' + $_ + '"' }) -join ' '
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.CreateNoWindow = $true
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    try {
        [void] $process.Start()
        $standardOutput = $process.StandardOutput.ReadToEndAsync()
        $standardError = $process.StandardError.ReadToEndAsync()
        $finished = $process.WaitForExit($TimeoutSeconds * 1000)
        if (-not $finished) {
            $process.Kill()
            $process.WaitForExit()
        }
        $text = $standardOutput.Result + $standardError.Result
        [System.IO.File]::WriteAllText($LogPath, $text, [System.Text.UTF8Encoding]::new($false))
        $failure = ''
        foreach ($line in ($text -split "`r?`n")) {
            if ($line.StartsWith('FAIL:')) { $failure = $line; break }
        }
        if (-not $finished) { return @{ Outcome = 'Timeout'; Failure = 'Stage exceeded its time limit.' } }
        if ($process.ExitCode -eq 0) { return @{ Outcome = 'Pass'; Failure = '' } }
        return @{ Outcome = 'Fail'; Failure = $failure }
    } finally {
        $process.Dispose()
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
Copy-Inputs -Destination $pristineDirectory
Copy-Inputs -Destination $workDirectory
$harnessProject = Join-Path $workDirectory "$harnessRelative\CollisionDetectionVerification.csproj"
$executable = Join-Path $workDirectory "$harnessRelative\bin\Release\net10.0\CollisionDetectionVerification.exe"
$utf8 = [System.Text.UTF8Encoding]::new($false)
$results = @()

foreach ($mutant in $mutants) {
    if ($Only.Count -gt 0 -and $Only -notcontains $mutant.Name) { continue }
    Write-Host "Mutant $($mutant.Name)"
    Copy-Item -Path (Join-Path $pristineDirectory "$detectorRelative\*.cs") -Destination (Join-Path $workDirectory $detectorRelative) -Force
    foreach ($edit in $mutant.Edits) {
        $path = Join-Path $workDirectory (Join-Path $detectorRelative $edit.File)
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
    $buildLog = & dotnet build $harnessProject --configuration Release --nologo -p:NuGetAudit=false 2>&1
    $buildCode = $LASTEXITCODE
    [System.IO.File]::WriteAllText((Join-Path $mutantDirectory 'Build.log'), ($buildLog | Out-String), $utf8)
    $record = [ordered]@{
        Name = $mutant.Name; Path = $mutant.Path; Description = $mutant.Description
        Build = 'Pass'; OriginalSuite = ''; OriginalFailure = ''; RibeyeSuite = ''; RibeyeFailure = ''
        ExtendedSuite = ''; ExtendedFailure = ''
    }
    if ($buildCode -ne 0) {
        $record.Build = 'Fail'
    } else {
        $stage = Invoke-Stage -Executable $executable -Arguments @() -LogPath (Join-Path $mutantDirectory 'OriginalSuite.log') -TimeoutSeconds 300
        $record.OriginalSuite = $stage.Outcome; $record.OriginalFailure = $stage.Failure
        $stage = Invoke-Stage -Executable $executable -Arguments @('--performance', '--ribeye', ('--output=' + (Join-Path $mutantDirectory 'Ribeye.json'))) -LogPath (Join-Path $mutantDirectory 'RibeyeSuite.log') -TimeoutSeconds 600
        $record.RibeyeSuite = $stage.Outcome; $record.RibeyeFailure = $stage.Failure
        if (-not $SkipExtendedSuite) {
            $stage = Invoke-Stage -Executable $executable -Arguments @('--performance', ('--output=' + (Join-Path $mutantDirectory 'Extended.json'))) -LogPath (Join-Path $mutantDirectory 'ExtendedSuite.log') -TimeoutSeconds 900
            $record.ExtendedSuite = $stage.Outcome; $record.ExtendedFailure = $stage.Failure
        }
    }
    Write-Host "  build=$($record.Build) original=$($record.OriginalSuite) ribeye=$($record.RibeyeSuite) extended=$($record.ExtendedSuite)"
    $results += [pscustomobject] $record
    [System.IO.File]::WriteAllText((Join-Path $OutputDirectory 'MutationResults.json'), ($results | ConvertTo-Json -Depth 4), $utf8)
}
Write-Host "Mutation results: $(Join-Path $OutputDirectory 'MutationResults.json')"
