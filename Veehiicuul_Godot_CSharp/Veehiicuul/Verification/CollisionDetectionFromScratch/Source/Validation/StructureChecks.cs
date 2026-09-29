using System;
using System.Collections.Generic;
using System.Globalization;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>
/// Checks what the detector builds: its edge list, the reported index
/// properties, the completeness of every expanded-grid cell, and repeatability.
/// </summary>
internal static class StructureChecks {
    public static void Run(ValidationContext context, IReadOnlyList<TrackCase> tracks) {
        context.Run("Structure: edges match the outlines", result => {
            foreach (TrackCase track in tracks) {
                CheckEdges(result, track);
            }
        });
        context.Run("Structure: index properties are consistent", result => {
            foreach (TrackCase track in tracks) {
                CheckProperties(result, track);
            }
        });
        context.Run("Structure: every expanded-grid cell lists every reachable edge", result => {
            foreach (TrackCase track in tracks) {
                CheckCellCoverage(result, track);
            }
        });
        context.Run("Structure: construction is repeatable", result => {
            foreach (TrackCase track in tracks) {
                CheckRepeatable(result, track);
            }
        });
    }

    private static void CheckEdges(SuiteResult result, TrackCase track) {
        OracleTrack oracle = track.Track.Oracle;
        float[,] edges = DetectorInternals.ReadEdges(track.Detector);
        result.Check(edges.GetLength(0) == oracle.EdgeCount, $"{track.Name}: edge count differs.");
        result.Check(track.Detector.EdgeCount == oracle.EdgeCount, $"{track.Name}: EdgeCount property differs.");
        result.Check(
            track.Detector.OutlineCount == track.Track.Collider.Outlines.Count,
            $"{track.Name}: OutlineCount property differs.");
        int count = Math.Min(edges.GetLength(0), oracle.EdgeCount);
        for (int edge = 0; edge < count; ++edge) {
            bool same = edges[edge, 0] == oracle.Ax[edge] && edges[edge, 1] == oracle.Ay[edge]
                && edges[edge, 2] == oracle.Bx[edge] && edges[edge, 3] == oracle.By[edge];
            result.Check(same, $"{track.Name}: edge {edge} differs from the outline.");
        }
    }

    private static void CheckProperties(SuiteResult result, TrackCase track) {
        TrackCollisionDetector detector = track.Detector;
        string name = track.Name;
        result.Check(
            detector.OrdinaryEdgeCount + detector.OutlierEdgeCount == detector.EdgeCount,
            $"{name}: ordinary plus long edges do not sum to all edges.");
        result.Check(detector.CellSize > 0.0, $"{name}: cell size is not positive.");
        double width = (double)track.IndexFootprint.MaxX - track.IndexFootprint.MinX;
        double height = (double)track.IndexFootprint.MaxY - track.IndexFootprint.MinY;
        result.Check(
            detector.CellSize == Math.Min(width, height) * 0.5,
            $"{name}: cell size is not half of the shorter footprint side.");
        result.Check(
            detector.OccupiedCellCount <= detector.GridCellCount,
            $"{name}: more occupied cells than cells.");
        result.Check(
            detector.OversizedEdgeCount == detector.OutlierEdgeCount,
            $"{name}: the two long-edge counts differ.");
        if (detector.UsesExpandedGrid) {
            result.Check(detector.GridCellCount <= 65536, $"{name}: expanded grid exceeds its cell limit.");
            result.Check(
                detector.StoredGridEdgeReferenceCount <= 1048576,
                $"{name}: expanded grid exceeds its reference limit.");
            result.Check(detector.OutlierEdgeCount == 0, $"{name}: expanded grid reports long edges.");
        } else {
            CheckCenterGrid(result, track);
        }

        result.Fact(
            name,
            string.Create(
                CultureInfo.InvariantCulture,
                $"{track.IndexKind}; edges={detector.EdgeCount}; cell={detector.CellSize:R}; "
                + $"grid={detector.GridColumnCount}x{detector.GridRowCount}; occupied={detector.OccupiedCellCount}; "
                + $"references={detector.StoredGridEdgeReferenceCount}; longEdges={detector.OutlierEdgeCount}; "
                + $"treeNodes={detector.OutlierBvhNodeCount}; {track.Purpose}"));
    }

    /// <summary>
    /// The fallback grid files each edge that fits in a cell under the cell that holds
    /// the center of its bounding box. Recounting those cells from the outlines shows
    /// whether distinct cells were merged or edges were misfiled as long.
    /// </summary>
    private static void CheckCenterGrid(SuiteResult result, TrackCase track) {
        TrackCollisionDetector detector = track.Detector;
        OracleTrack oracle = track.Track.Oracle;
        double cell = detector.CellSize;
        List<(double X, double Y)> centers = [];
        double originX = double.PositiveInfinity, originY = double.PositiveInfinity;
        int longEdges = 0;
        for (int edge = 0; edge < oracle.EdgeCount; ++edge) {
            double width = (double)oracle.MaxX[edge] - oracle.MinX[edge];
            double height = (double)oracle.MaxY[edge] - oracle.MinY[edge];
            if (width > cell || height > cell) {
                ++longEdges;
                continue;
            }

            double x = ((double)oracle.MinX[edge] + oracle.MaxX[edge]) * 0.5;
            double y = ((double)oracle.MinY[edge] + oracle.MaxY[edge]) * 0.5;
            centers.Add((x, y));
            originX = Math.Min(originX, x);
            originY = Math.Min(originY, y);
        }

        if (detector.OutlierBvhNodeCount > 0) {
            int allocated = DetectorInternals.ReadAllocatedTreeNodes(detector);
            result.Check(
                allocated >= detector.OutlierBvhNodeCount,
                $"{track.Name}: more tree nodes used than allocated.");
            result.Fact(
                track.Name + " long-edge tree",
                $"nodes used={detector.OutlierBvhNodeCount}; nodes allocated={allocated}");
        }

        if (detector.OrdinaryEdgeCount == 0) {
            // A coordinate span too large for integer cells moves every edge to the long-edge index.
            result.Check(
                detector.OutlierEdgeCount == oracle.EdgeCount,
                $"{track.Name}: without a grid, every edge must be in the long-edge index.");
            return;
        }

        result.Check(
            detector.OutlierEdgeCount == longEdges,
            $"{track.Name}: {detector.OutlierEdgeCount} long edges reported, {longEdges} counted.");
        HashSet<(long Column, long Row)> cells = [];
        foreach ((double x, double y) in centers) {
            cells.Add(((long)Math.Floor((x - originX) / cell), (long)Math.Floor((y - originY) / cell)));
        }

        result.Check(
            detector.OccupiedCellCount == cells.Count,
            $"{track.Name}: {detector.OccupiedCellCount} occupied cells reported, {cells.Count} counted.");
        result.Check(
            detector.StoredGridEdgeReferenceCount == centers.Count,
            $"{track.Name}: {detector.StoredGridEdgeReferenceCount} grid references reported, "
            + $"{centers.Count} counted.");
    }

    /// <summary>
    /// Recomputes, for every cell, which edges lie within the footprint's corner
    /// distance of the cell rectangle, using distances between boxes in binary64.
    /// Every such edge must be listed. The count of listed edges beyond that set
    /// measures how much extra work the cell's list causes.
    /// </summary>
    private static void CheckCellCoverage(SuiteResult result, TrackCase track) {
        ExpandedGridView? grid = track.Grid;
        if (grid is null) {
            return;
        }

        OracleTrack oracle = track.Track.Oracle;
        double cellSize = 1.0 / grid.InverseCellSize;
        double reach = Math.Sqrt(
            (double)grid.MaximumLocalX * grid.MaximumLocalX + (double)grid.MaximumLocalY * grid.MaximumLocalY);
        // A pose on a cell's upper boundary belongs to the next cell, and binary64 rounding
        // differs between this recomputation and the detector. Exact boundaries are the
        // subject of the adversarial suites, so require only what is clearly within reach.
        reach -= 1e-9 * Math.Max(1.0, reach);
        long listed = 0, required = 0, missing = 0;
        int largestList = 0;
        bool[] present = new bool[oracle.EdgeCount];
        for (int row = 0; row < grid.RowCount; ++row) {
            double cellMinY = grid.OriginY + row * cellSize;
            double cellMaxY = cellMinY + cellSize;
            for (int column = 0; column < grid.ColumnCount; ++column) {
                double cellMinX = grid.OriginX + column * cellSize;
                double cellMaxX = cellMinX + cellSize;
                int cell = row * grid.ColumnCount + column;
                int offset = grid.CellOffsets[cell], count = grid.CellCounts[cell];
                largestList = Math.Max(largestList, count);
                listed += count;
                for (int index = offset; index < offset + count; ++index) {
                    present[grid.EdgeIds[index]] = true;
                }

                for (int edge = 0; edge < oracle.EdgeCount; ++edge) {
                    // Axis-aligned box distance: the detector expands edge boxes, not edges.
                    double gapX = Math.Max(0.0, Math.Max(oracle.MinX[edge] - cellMaxX, cellMinX - oracle.MaxX[edge]));
                    double gapY = Math.Max(0.0, Math.Max(oracle.MinY[edge] - cellMaxY, cellMinY - oracle.MaxY[edge]));
                    if (gapX < reach && gapY < reach) {
                        ++required;
                        if (!present[edge]) {
                            ++missing;
                            result.Fail($"{track.Name}: cell ({column}, {row}) omits edge {edge}.");
                        }
                    }
                }

                for (int index = offset; index < offset + count; ++index) {
                    present[grid.EdgeIds[index]] = false;
                }
            }
        }

        result.AddCases((long)grid.RowCount * grid.ColumnCount);
        result.Fact(
            track.Name,
            string.Create(
                CultureInfo.InvariantCulture,
                $"cells={grid.RowCount * grid.ColumnCount}; listed={listed}; withinReach={required}; "
                + $"missing={missing}; largestList={largestList}"));
    }

    private static void CheckRepeatable(SuiteResult result, TrackCase track) {
        TrackCollisionDetector second = new(track.Track.Collider, track.IndexFootprint);
        TrackCollisionDetector first = track.Detector;
        bool same = first.EdgeCount == second.EdgeCount
            && first.GridColumnCount == second.GridColumnCount
            && first.GridRowCount == second.GridRowCount
            && first.OccupiedCellCount == second.OccupiedCellCount
            && first.StoredGridEdgeReferenceCount == second.StoredGridEdgeReferenceCount
            && first.OutlierEdgeCount == second.OutlierEdgeCount
            && first.OutlierBvhNodeCount == second.OutlierBvhNodeCount
            && first.UsesExpandedGrid == second.UsesExpandedGrid
            && first.UsesDenseGrid == second.UsesDenseGrid;
        result.Check(same, $"{track.Name}: a second construction reports a different index.");

        RectangleLocalBounds footprint = track.Scale(Footprints.Reference);
        RectanglePose[] poses = PoseSampler.NearBarrier(
            track.Track.Oracle, footprint, 2000, 9100, 1e-6 * track.FootprintScale, 2.0 * track.FootprintScale);
        bool[] forward = new bool[poses.Length];
        for (int index = 0; index < poses.Length; ++index) {
            forward[index] = first.IsColliding(footprint, poses[index]);
        }

        // Reverse order on the first detector exposes hidden query state; the second
        // detector exposes construction differences.
        for (int index = poses.Length - 1; index >= 0; --index) {
            result.Check(
                first.IsColliding(footprint, poses[index]) == forward[index],
                $"{track.Name}: result depends on query order; {Exact.Text(poses[index])}.");
            result.Check(
                second.IsColliding(footprint, poses[index]) == forward[index],
                $"{track.Name}: second detector disagrees; {Exact.Text(poses[index])}.");
        }
    }
}
