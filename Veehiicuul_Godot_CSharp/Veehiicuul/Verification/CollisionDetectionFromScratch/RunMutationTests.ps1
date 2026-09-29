# Measures which deliberately injected detector defects this harness detects.
#
# The detector sources and this harness are copied into an isolated folder under the
# repository's ignored Build directory. Each mutant changes the copied detector only.
# Nothing in the repository's own source folders is modified.
param(
    [string] $OutputDirectory = '',
    [string[]] $Only = @(),
    [double] $Scale = 0.1
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# powershell.exe -File passes a comma-separated list as one string.
$Only = @($Only | ForEach-Object { $_ -split ',' } | Where-Object { $_ -ne '' })

$projectDirectory = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repositoryDirectory = [System.IO.Path]::GetFullPath((Join-Path $projectDirectory '..\..'))
if ($OutputDirectory -eq '') {
    $OutputDirectory = Join-Path $repositoryDirectory ('Build\CollisionFromScratchMutation\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
}
$workDirectory = Join-Path $OutputDirectory 'Veehiicuul'
$pristineDirectory = Join-Path $OutputDirectory 'Pristine'
$detectorRelative = 'Source\GameDataAndLogic\CollisionDetection'
$harnessRelative = 'Verification\CollisionDetectionFromScratch'

$detector = 'TrackCollisionDetector.cs'
$grid = 'TrackCollisionDetector.ExpandedGrid.cs'

# Each edit must match the pristine source exactly once, so a stale mutant fails loudly.
$mutants = @(
    @{ Name = 'Unmodified'; Area = 'None'; Description = 'Control run without any change.'; Edits = @() },

    @{ Name = 'ReachUsesLargerExtentOnly'; Area = 'Expanded grid'; Description = 'Reach is the larger half extent instead of the corner distance.'; Edits = @(
        @{ File = $grid; Find = 'double radius = Math.Sqrt((double)maximumLocalX * maximumLocalX + (double)maximumLocalY * maximumLocalY);'; Replace = 'double radius = Math.Max(maximumLocalX, maximumLocalY);' }) },
    @{ Name = 'ReachShortByOneTenth'; Area = 'Expanded grid'; Description = 'Reach is 0.1 too small.'; Edits = @(
        @{ File = $grid; Find = 'radius = Math.BitIncrement(radius + roundingMargin);'; Replace = 'radius = radius - 0.1;' }) },
    @{ Name = 'ReachShortByOneHundredth'; Area = 'Expanded grid'; Description = 'Reach is 0.01 too small.'; Edits = @(
        @{ File = $grid; Find = 'radius = Math.BitIncrement(radius + roundingMargin);'; Replace = 'radius = radius - 0.01;' }) },
    @{ Name = 'ReachShortByOneThousandth'; Area = 'Expanded grid'; Description = 'Reach is 0.001 too small.'; Edits = @(
        @{ File = $grid; Find = 'radius = Math.BitIncrement(radius + roundingMargin);'; Replace = 'radius = radius - 0.001;' }) },
    @{ Name = 'ReachShortByOneMillionth'; Area = 'Expanded grid'; Description = 'Reach is 0.000001 too small.'; Edits = @(
        @{ File = $grid; Find = 'radius = Math.BitIncrement(radius + roundingMargin);'; Replace = 'radius = radius - 0.000001;' }) },
    @{ Name = 'ReachWithoutRoundingMargin'; Area = 'Expanded grid'; Description = 'Reach omits the margin for binary32 rounding of corners.'; Edits = @(
        @{ File = $grid; Find = 'radius = Math.BitIncrement(radius + roundingMargin);'; Replace = 'radius = radius + 0.0 * roundingMargin;' }) },
    @{ Name = 'UpperColumnOmitsReach'; Area = 'Expanded grid'; Description = 'Edge coverage omits the reach on the upper X side.'; Edits = @(
        @{ File = $grid; Find = 'int maxX = Map(Math.BitIncrement((double)bounds.MaxX + radius), originX, inverseCellSize, columnCount);'; Replace = 'int maxX = Map(Math.BitIncrement((double)bounds.MaxX), originX, inverseCellSize, columnCount);' }) },
    @{ Name = 'LowerRowOmitsReach'; Area = 'Expanded grid'; Description = 'Edge coverage omits the reach on the lower Y side.'; Edits = @(
        @{ File = $grid; Find = 'int minY = Map(Math.BitDecrement((double)bounds.MinY - radius), originY, inverseCellSize, rowCount);'; Replace = 'int minY = Map(Math.BitDecrement((double)bounds.MinY), originY, inverseCellSize, rowCount);' }) },
    @{ Name = 'LastCandidateSkipped'; Area = 'Expanded grid'; Description = 'The origin-cell query skips the final candidate of each cell.'; Edits = @(
        @{ File = $detector; Find = "            RectangleQuad expandedRectangle = RectangleTransformer.Transform(localBounds, pose);`n            int end = range.Offset + range.Count;"; Replace = "            RectangleQuad expandedRectangle = RectangleTransformer.Transform(localBounds, pose);`n            int end = range.Offset + range.Count - 1;" }) },
    @{ Name = 'CellLookupRoundsToNearest'; Area = 'Expanded grid'; Description = 'The cell lookup rounds the column to the nearest cell.'; Edits = @(
        @{ File = $grid; Find = 'return this._cells[(int)row * this.ColumnCount + (int)column];'; Replace = 'return this._cells[(int)row * this.ColumnCount + Math.Min(this.ColumnCount - 1, (int)(column + 0.5))];' }) },
    @{ Name = 'OriginLookupIgnoresFootprintLimit'; Area = 'Expanded grid'; Description = 'Origin-cell lookup accepts footprints larger than the indexed one.'; Edits = @(
        @{ File = $grid; Find = "                && bounds.MinX >= -this._maximumLocalX && bounds.MaxX <= this._maximumLocalX`n                && bounds.MinY >= -this._maximumLocalY && bounds.MaxY <= this._maximumLocalY`n"; Replace = "                && this._maximumLocalX > 0f && this._maximumLocalY > 0f`n" }) },
    @{ Name = 'CenterLookupIgnoresHeightLimit'; Area = 'Expanded grid'; Description = 'Center-cell lookup accepts footprints taller than the indexed one.'; Edits = @(
        @{ File = $grid; Find = "                && (double)bounds.MaxY - bounds.MinY <= 2.0 * this._maximumLocalY`n"; Replace = "                && this._maximumLocalY > 0f`n" }) },
    @{ Name = 'CenterLookupUsesOrigin'; Area = 'Expanded grid'; Description = 'Shifted-origin lookup uses the pose position instead of the rectangle center.'; Edits = @(
        @{ File = $detector; Find = "                CellRange range = this._expandedGrid.GetRange(`n                    ((double)rectangle.P0.X + rectangle.P2.X) * 0.5,`n                    ((double)rectangle.P0.Y + rectangle.P2.Y) * 0.5);"; Replace = "                CellRange range = this._expandedGrid.GetRange(`n                    pose.PositionX,`n                    pose.PositionY);" }) },
    @{ Name = 'CenterLookupUsesAdjacentCorners'; Area = 'Expanded grid'; Description = 'Shifted-origin lookup averages two adjacent corners instead of opposite ones.'; Edits = @(
        @{ File = $detector; Find = "                    ((double)rectangle.P0.X + rectangle.P2.X) * 0.5,`n                    ((double)rectangle.P0.Y + rectangle.P2.Y) * 0.5);"; Replace = "                    ((double)rectangle.P0.X + rectangle.P1.X) * 0.5,`n                    ((double)rectangle.P0.Y + rectangle.P1.Y) * 0.5);" }) },
    @{ Name = 'OverallBoundsIgnoreUpperY'; Area = 'Expanded grid'; Description = 'The overall bounds never grow in the upper Y direction.'; Edits = @(
        @{ File = $detector; Find = '            maxY = Math.Max(maxY, bounds.MaxY);'; Replace = '            maxY = Math.Max(maxY, first.MaxY);' }) },

    @{ Name = 'ClosingEdgeTargetsSecondVertex'; Area = 'Edge construction'; Description = 'Each closing edge ends at the second vertex instead of the first.'; Edits = @(
        @{ File = $detector; Find = 'vertexIndex + 1 == vertices.Count ? 0 : vertexIndex + 1];'; Replace = 'vertexIndex + 1 == vertices.Count ? 1 : vertexIndex + 1];' }) },
    @{ Name = 'CoordinatesSwapped'; Area = 'Edge construction'; Description = 'Barrier X and Y are exchanged.'; Edits = @(
        @{ File = $detector; Find = 'PointF a = new(rawA.X, rawA.Y);'; Replace = 'PointF a = new(rawA.Y, rawA.X);' },
        @{ File = $detector; Find = 'PointF b = new(rawB.X, rawB.Y);'; Replace = 'PointF b = new(rawB.Y, rawB.X);' }) },

    @{ Name = 'RotationDirectionReversed'; Area = 'Rectangle transform'; Description = 'Positive yaw turns the rectangle counterclockwise.'; Edits = @(
        @{ File = $detector; Find = 'double worldXDouble = (double)pose.PositionX + xCosine + ySine;'; Replace = 'double worldXDouble = (double)pose.PositionX + xCosine - ySine;' },
        @{ File = $detector; Find = 'double worldYDouble = (double)pose.PositionY - xSine + yCosine;'; Replace = 'double worldYDouble = (double)pose.PositionY + xSine + yCosine;' }) },
    @{ Name = 'SineAndCosineExchanged'; Area = 'Rectangle transform'; Description = 'Sine and cosine are exchanged.'; Edits = @(
        @{ File = $detector; Find = '(double sine, double cosine) = Math.SinCos(pose.RotationRadians);'; Replace = '(double cosine, double sine) = Math.SinCos(pose.RotationRadians);' }) },
    @{ Name = 'TrigonometryInSinglePrecision'; Area = 'Rectangle transform'; Description = 'Sine and cosine are rounded to binary32 before use.'; Edits = @(
        @{ File = $detector; Find = '(double sine, double cosine) = Math.SinCos(pose.RotationRadians);'; Replace = '(double sine, double cosine) = MathF.SinCos(pose.RotationRadians);' }) },
    @{ Name = 'OppositeCornersExchanged'; Area = 'Rectangle transform'; Description = 'The third and fourth corners are exchanged, which crosses two sides.'; Edits = @(
        @{ File = $detector; Find = 'return new RectangleQuad(p0, p1, p2, p3);'; Replace = 'return new RectangleQuad(p0, p1, p3, p2);' }) },

    @{ Name = 'FourthSideNotTested'; Area = 'Narrow phase'; Description = 'The side from the fourth corner to the first is never tested.'; Edits = @(
        @{ File = $detector; Find = "                || RobustPredicates.SegmentsIntersect(a, b, this.P2, this.P3)`n                || RobustPredicates.SegmentsIntersect(a, b, this.P3, this.P0);"; Replace = "                || RobustPredicates.SegmentsIntersect(a, b, this.P2, this.P3);" }) },
    @{ Name = 'CollinearPointExcludedAtLowerX'; Area = 'Narrow phase'; Description = 'A collinear point at the lower X end of a segment is not on it.'; Edits = @(
        @{ File = $detector; Find = 'return point.X >= Math.Min(a.X, b.X)'; Replace = 'return point.X > Math.Min(a.X, b.X)' }) },
    @{ Name = 'CollinearOverlapIgnored'; Area = 'Narrow phase'; Description = 'Collinear points are never treated as lying on a segment.'; Edits = @(
        @{ File = $detector; Find = 'return point.X >= Math.Min(a.X, b.X)'; Replace = 'return point.X > Math.Max(a.X, b.X)' }) },
    @{ Name = 'CrossingUsesEitherStraddle'; Area = 'Narrow phase'; Description = 'A crossing is reported when either segment straddles the other line.'; Edits = @(
        @{ File = $detector; Find = 'return abc != abd && cda != cdb;'; Replace = 'return abc != abd || cda != cdb;' }) },
    @{ Name = 'TouchingIsNotCrossing'; Area = 'Narrow phase'; Description = 'The final test requires strictly opposite signs.'; Edits = @(
        @{ File = $detector; Find = 'return abc != abd && cda != cdb;'; Replace = 'return abc * abd < 0 && cda * cdb < 0;' }) },
    @{ Name = 'TouchingBoundsDoNotOverlap'; Area = 'Narrow phase'; Description = 'Bounding boxes that only touch are treated as separate.'; Edits = @(
        @{ File = $detector; Find = "            return this.MinX <= other.MaxX`n                && this.MaxX >= other.MinX`n                && this.MinY <= other.MaxY`n                && this.MaxY >= other.MinY;"; Replace = "            return this.MinX < other.MaxX`n                && this.MaxX > other.MinX`n                && this.MinY < other.MaxY`n                && this.MaxY > other.MinY;" }) },
    @{ Name = 'EarlyRejectionInverted'; Area = 'Narrow phase'; Description = 'Segments on the same side are no longer rejected; opposite sides are.'; Edits = @(
        @{ File = $detector; Find = 'if (abc != 0 && abc == abd) {'; Replace = 'if (abc != 0 && abc == -abd) {' }) },

    @{ Name = 'ErrorBoundFarTooSmall'; Area = 'Orientation arithmetic'; Description = 'The filter trusts determinants it cannot certify.'; Edits = @(
        @{ File = $detector; Find = 'private const double CcwErrorBoundA = 3.3306690738754716e-16;'; Replace = 'private const double CcwErrorBoundA = 1e-30;' }) },
    @{ Name = 'UncertainOrientationIsCollinear'; Area = 'Orientation arithmetic'; Description = 'Uncertain orientations are reported as collinear without exact arithmetic.'; Edits = @(
        @{ File = $detector; Find = "            if (-determinant >= errorBound) {`n                return -1;`n            }`n`n            return ExactOrientationSign(a, b, c);"; Replace = "            if (-determinant >= errorBound) {`n                return -1;`n            }`n`n            return 0;" }) },
    @{ Name = 'BoundedIntegerShiftTooWide'; Area = 'Orientation arithmetic'; Description = 'The 128-bit route accepts exponent spans that overflow 64-bit operands.'; Edits = @(
        @{ File = $detector; Find = 'if (shift > 37) {'; Replace = 'if (shift > 60) {' }) },
    @{ Name = 'ExactDeterminantSignReversed'; Area = 'Orientation arithmetic'; Description = 'The 128-bit route returns the opposite sign.'; Edits = @(
        @{ File = $detector; Find = 'return exact > 0 ? 1 : exact < 0 ? -1 : 0;'; Replace = 'return exact > 0 ? -1 : exact < 0 ? 1 : 0;' }) },
    @{ Name = 'LargeIntegerDeterminantWrong'; Area = 'Orientation arithmetic'; Description = 'The arbitrary-precision route subtracts the wrong product.'; Edits = @(
        @{ File = $detector; Find = "            BigInteger determinant = ((bxi - axi) * (cyi - ayi))`n                - ((byi - ayi) * (cxi - axi));"; Replace = "            BigInteger determinant = ((bxi - axi) * (cyi - ayi))`n                - ((byi - ayi) * (cxi - ayi));" }) },
    @{ Name = 'SubnormalCheckRemoved'; Area = 'Orientation arithmetic'; Description = 'Subnormal inputs use the filtered route. Expected to preserve behavior.'; Edits = @(
        @{ File = $detector; Find = "            if (IsSubnormal(a.X)`n                || IsSubnormal(a.Y)`n                || IsSubnormal(b.X)`n                || IsSubnormal(b.Y)`n                || IsSubnormal(c.X)`n                || IsSubnormal(c.Y)) {`n                return ExactOrientationSign(a, b, c);`n            }"; Replace = "            _ = IsSubnormal(a.X);" }) },

    @{ Name = 'CenterGridExpansionDropped'; Area = 'Fallback center grid'; Description = 'Fallback grid queries are not grown by the largest edge half extent.'; Edits = @(
        @{ File = $detector; Find = "(double)queryBounds.MinX - this.MaximumHalfExtentX);"; Replace = "(double)queryBounds.MinX);" },
        @{ File = $detector; Find = "(double)queryBounds.MinY - this.MaximumHalfExtentY);"; Replace = "(double)queryBounds.MinY);" },
        @{ File = $detector; Find = "(double)queryBounds.MaxX + this.MaximumHalfExtentX);"; Replace = "(double)queryBounds.MaxX);" },
        @{ File = $detector; Find = "(double)queryBounds.MaxY + this.MaximumHalfExtentY);"; Replace = "(double)queryBounds.MaxY);" }) },
    @{ Name = 'SparseCellsSkipLastRow'; Area = 'Fallback center grid'; Description = 'Sparse fallback queries skip the final covered row.'; Edits = @(
        @{ File = $detector; Find = "                ?? throw new InvalidOperationException(`"A nonempty collision grid must have cell storage.`");`n            for (int row = minRow; row <= maxRow; ++row) {"; Replace = "                ?? throw new InvalidOperationException(`"A nonempty collision grid must have cell storage.`");`n            for (int row = minRow; row < maxRow; ++row) {" }) },
    @{ Name = 'DenseCellsSkipLastColumn'; Area = 'Fallback center grid'; Description = 'Dense fallback queries skip the final covered column.'; Edits = @(
        @{ File = $detector; Find = "                int rowOffset = row * this._grid.ColumnCount;`n                for (int column = minColumn; column <= maxColumn; ++column) {"; Replace = "                int rowOffset = row * this._grid.ColumnCount;`n                for (int column = minColumn; column < maxColumn; ++column) {" }) },
    @{ Name = 'SparseCellKeysCollide'; Area = 'Fallback center grid'; Description = 'Sparse cell keys keep only sixteen bits of the row.'; Edits = @(
        @{ File = $detector; Find = 'return ((long)row << 32) | (uint)column;'; Replace = 'return ((long)(row & 0xffff) << 32) | (uint)(column & 0xfff);' }) },
    @{ Name = 'BroadQuerySkipsLongEdges'; Area = 'Fallback center grid'; Description = 'A grid hit is required before long edges are consulted.'; Edits = @(
        @{ File = $detector; Find = "                    maxRow)) {`n                return true;`n            }`n        }`n`n        return this._outliers.Intersects(rectangle);"; Replace = "                    maxRow)) {`n                return true;`n            }`n`n            return false;`n        }`n`n        return this._outliers.Intersects(rectangle);" }) },

    @{ Name = 'TreeVisitsLeftChildOnly'; Area = 'Fallback long-edge tree'; Description = 'Tree traversal never descends into right children.'; Edits = @(
        @{ File = $detector; Find = "            return this.QueryNode(node.Left, rectangle)`n                || this.QueryNode(node.Right, rectangle);"; Replace = "            return this.QueryNode(node.Left, rectangle);" }) },
    @{ Name = 'TreeLeafSkipsLastEdge'; Area = 'Fallback long-edge tree'; Description = 'Each tree leaf skips its final edge.'; Edits = @(
        @{ File = $detector; Find = "                int end = node.Start + node.Count;"; Replace = "                int end = node.Start + node.Count - 1;" }) },
    @{ Name = 'TreeBoundsOmitLastEdge'; Area = 'Fallback long-edge tree'; Description = 'Node bounds omit the final edge of their range.'; Edits = @(
        @{ File = $detector; Find = "            for (int i = start + 1; i < end; ++i) {`n                bounds = AabbF.Combine(bounds, this._edges[this._edgeOrder[i]].Bounds);"; Replace = "            for (int i = start + 1; i < end - 1; ++i) {`n                bounds = AabbF.Combine(bounds, this._edges[this._edgeOrder[i]].Bounds);" }) },
    @{ Name = 'ShortListSkipsFirstEdge'; Area = 'Fallback long-edge tree'; Description = 'The short long-edge list skips its first edge.'; Edits = @(
        @{ File = $detector; Find = "                for (int i = 0; i < this._edgeOrder.Length; ++i) {"; Replace = "                for (int i = 1; i < this._edgeOrder.Length; ++i) {" }) },

    @{ Name = 'FullScanSkipsLastEdge'; Area = 'Full scan'; Description = 'The full scan, which also serves as the detector''s own reference, skips the last edge.'; Edits = @(
        @{ File = $detector; Find = "        for (int edgeIndex = 0; edgeIndex < this._edges.Length; ++edgeIndex) {"; Replace = "        for (int edgeIndex = 0; edgeIndex < this._edges.Length - 1; ++edgeIndex) {" }) },
    @{ Name = 'OverflowingCornersAccepted'; Area = 'Input handling'; Description = 'Corner coordinates that overflow binary32 are accepted.'; Edits = @(
        @{ File = $detector; Find = "            if (!CollisionMath.IsFinite(worldX) || !CollisionMath.IsFinite(worldY)) {"; Replace = "            if (float.IsNaN(worldX) || float.IsNaN(worldY)) {" }) },
    @{ Name = 'ZeroLengthEdgeAccepted'; Area = 'Input handling'; Description = 'Zero-length barrier edges are accepted.'; Edits = @(
        @{ File = $detector; Find = "                if (a.X == b.X && a.Y == b.Y) {"; Replace = "                if (float.IsNaN(a.X) && a.Y == b.Y) {" }) },
    @{ Name = 'TwoVertexOutlineAccepted'; Area = 'Input handling'; Description = 'Outlines with two vertices are accepted.'; Edits = @(
        @{ File = $detector; Find = "            if (outline.Vertices.Count < 3) {"; Replace = "            if (outline.Vertices.Count < 2) {" }) }
)

function Copy-Inputs {
    param([string] $Destination)
    New-Item -ItemType Directory -Path (Join-Path $Destination $detectorRelative) -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $Destination $harnessRelative) -Force | Out-Null
    Copy-Item -Path (Join-Path $projectDirectory "$detectorRelative\*.cs") -Destination (Join-Path $Destination $detectorRelative)
    Copy-Item -LiteralPath (Join-Path $projectDirectory "$harnessRelative\CollisionDetectionFromScratch.csproj") -Destination (Join-Path $Destination $harnessRelative)
    Copy-Item -LiteralPath (Join-Path $projectDirectory "$harnessRelative\Source") -Destination (Join-Path $Destination $harnessRelative) -Recurse
}

function Invoke-Validation {
    param([string] $Executable, [string] $LogPath, [string] $JsonPath)
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo.FileName = $Executable
    $process.StartInfo.Arguments = 'validate --scale={0} "--output={1}"' -f $Scale.ToString([System.Globalization.CultureInfo]::InvariantCulture), $JsonPath
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.CreateNoWindow = $true
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    try {
        [void] $process.Start()
        $standardOutput = $process.StandardOutput.ReadToEndAsync()
        $standardError = $process.StandardError.ReadToEndAsync()
        $finished = $process.WaitForExit(1800 * 1000)
        if (-not $finished) {
            $process.Kill()
            $process.WaitForExit()
        }
        [System.IO.File]::WriteAllText($LogPath, $standardOutput.Result + $standardError.Result, [System.Text.UTF8Encoding]::new($false))
        if (-not $finished) { return 'Timeout' }
        if ($process.ExitCode -eq 0) { return 'Pass' }
        if ($process.ExitCode -eq 1) { return 'Fail' }
        return 'Error'
    } finally {
        $process.Dispose()
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
Copy-Inputs -Destination $pristineDirectory
Copy-Inputs -Destination $workDirectory
$harnessProject = Join-Path $workDirectory "$harnessRelative\CollisionDetectionFromScratch.csproj"
$executable = Join-Path $workDirectory "$harnessRelative\bin\Release\net10.0\CollisionDetectionFromScratch.exe"
$utf8 = [System.Text.UTF8Encoding]::new($false)
$results = @()

foreach ($mutant in $mutants) {
    if ($Only.Count -gt 0 -and $Only -notcontains $mutant.Name) { continue }
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
    # Analyzer findings caused by a mutation are not the subject here.
    $buildLog = & dotnet build $harnessProject --configuration Release --nologo -p:NuGetAudit=false -p:RunAnalyzers=false 2>&1
    $buildCode = $LASTEXITCODE
    [System.IO.File]::WriteAllText((Join-Path $mutantDirectory 'Build.log'), ($buildLog | Out-String), $utf8)
    $record = [ordered]@{
        Name = $mutant.Name; Area = $mutant.Area; Description = $mutant.Description
        Build = 'Pass'; Outcome = ''; FailedSuites = @(); Failures = 0
    }
    if ($buildCode -ne 0) {
        $record.Build = 'Fail'
    } else {
        $jsonPath = Join-Path $mutantDirectory 'Validation.json'
        $record.Outcome = Invoke-Validation -Executable $executable -LogPath (Join-Path $mutantDirectory 'Validation.log') -JsonPath $jsonPath
        if (Test-Path -LiteralPath $jsonPath) {
            $validation = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
            $record.Failures = $validation.Failures
            $record.FailedSuites = @($validation.Suites | Where-Object { $_.Failures -gt 0 } | ForEach-Object { $_.Name })
        }
    }
    Write-Host "Mutant $($mutant.Name): build=$($record.Build) outcome=$($record.Outcome) failedSuites=$($record.FailedSuites.Count)"
    $results += [pscustomobject] $record
    [System.IO.File]::WriteAllText((Join-Path $OutputDirectory 'MutationResults.json'), ($results | ConvertTo-Json -Depth 4), $utf8)
}
Write-Host "Mutation results: $(Join-Path $OutputDirectory 'MutationResults.json')"
