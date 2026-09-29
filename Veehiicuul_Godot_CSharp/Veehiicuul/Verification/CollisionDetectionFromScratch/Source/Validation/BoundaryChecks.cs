using System;
using System.Collections.Generic;
using System.Globalization;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>
/// Adversarial poses for the expanded grid. A cell lists the edges within the
/// footprint's corner distance of the cell. The list is only just sufficient
/// when a corner points straight along a grid axis, reaches a barrier at its
/// full distance, and the pose sits on the first representable position of a
/// cell. Random poses almost never meet all three conditions; these do.
/// </summary>
internal static class BoundaryChecks {
    private const double Extent = 130.0;
    private const int RowCount = 27;
    private const double RowSpacing = 9.0;
    private const int BoundaryStride = 6;
    private const int PositionSteps = 6;

    public static void Run(ValidationContext context) {
        context.Run("Boundary: barriers placed at full reach from cell boundaries", result => {
            ComparisonTally total = new();
            long aligned = 0, poses = 0;
            int shifts = context.WorkScale >= 1.0 ? BoundaryStride : context.WorkScale >= 0.2 ? 2 : 1;
            for (int axis = 0; axis < 2; ++axis) {
                foreach (int sign in new[] { 1, -1 }) {
                    for (int shift = 0; shift < shifts; ++shift) {
                        total.Add(AlignedPins(result, context, axis, sign, shift, ref aligned, ref poses));
                    }
                }
            }

            result.Fact("Poses", poses);
            result.Fact("Poses whose corner landed exactly on the barrier's row", aligned);
            result.Fact("Totals", total.Summary());
        });
        context.Run("Boundary: every cell corner with corners at full reach", result => {
            foreach (string name in new[] { TrackCatalog.CircuitCompact, TrackCatalog.Scattered }) {
                TrackCase track = TrackCatalog.Build(name);
                result.Fact(name, CellCorners(result, context, track).Summary());
            }
        });
        context.Run("Boundary: corners at full reach land on barrier vertices", result => {
            foreach (string name in new[] { TrackCatalog.Circuit, TrackCatalog.CircuitFine, TrackCatalog.Scattered }) {
                TrackCase track = TrackCatalog.Build(name);
                result.Fact(name, ReachContacts(result, context, track).Summary());
            }
        });
    }

    /// <summary>
    /// Yaw that points a footprint corner along a direction given as a counterclockwise
    /// angle from +X. The corner's own direction is its angle in the local frame; a
    /// clockwise yaw subtracts from it.
    /// </summary>
    internal static float ReachYaw(RectangleLocalBounds footprint, int corner, double direction) {
        double localX = corner is 0 or 3 ? footprint.MinX : footprint.MaxX;
        double localY = corner is 0 or 1 ? footprint.MinY : footprint.MaxY;
        double yaw = Math.Atan2(localY, localX) - direction;
        return (float)Math.IEEERemainder(yaw, Math.Tau);
    }

    private static (float X, float Y) Point(int axis, float along, float across) {
        return axis == 0 ? (along, across) : (across, along);
    }

    private static ComparisonTally AlignedPins(
        SuiteResult result, ValidationContext context, int axis, int sign, int shift,
        ref long aligned, ref long poseCount) {
        RectangleLocalBounds footprint = Footprints.Reference;
        float extent = (float)Extent;
        (float X, float Y)[][] anchors = [
            [(-extent, -extent), (-extent + 0.5f, -extent), (-extent, -extent + 0.5f)],
            [(extent, extent), (extent - 0.5f, extent), (extent, extent - 0.5f)],
        ];
        TrackCollisionDetector anchorsOnly = new(
            SyntheticTrack.FromOutlines("Anchors", anchors).Collider, footprint);
        ExpandedGridView? anchorGrid = ExpandedGridView.TryCreate(anchorsOnly);
        if (anchorGrid is null) {
            result.Fail("The anchor fixture did not receive an expanded grid.");
            return new ComparisonTally();
        }

        double cell = 1.0 / anchorGrid.InverseCellSize;
        double origin = axis == 0 ? anchorGrid.OriginX : anchorGrid.OriginY;
        int boundaryCount = axis == 0 ? anchorGrid.ColumnCount : anchorGrid.RowCount;
        double reach = Math.Sqrt(
            (double)anchorGrid.MaximumLocalX * anchorGrid.MaximumLocalX
            + (double)anchorGrid.MaximumLocalY * anchorGrid.MaximumLocalY);

        List<(float X, float Y)[]> loops = [.. anchors];
        List<(double Boundary, float Tip, float Across)> pins = [];
        for (int boundary = shift; boundary < boundaryCount; boundary += BoundaryStride) {
            double coordinate = origin + boundary * cell;
            if (Math.Abs(coordinate) > Extent - 30.0) {
                continue;
            }

            for (int row = 0; row < RowCount; ++row) {
                float across = (float)((row - RowCount / 2) * RowSpacing + 0.5);
                float tip = ExactNumber.Step((float)(coordinate - sign * reach), row - RowCount / 2);
                // The barrier is a narrow triangle whose tip points at the vehicle.
                float root = tip - sign;
                loops.Add([
                    Point(axis, tip, across),
                    Point(axis, root, across + 0.25f),
                    Point(axis, root, across - 0.25f),
                ]);
                pins.Add((coordinate, tip, across));
            }
        }

        string name = string.Create(
            CultureInfo.InvariantCulture,
            $"Pins axis={(axis == 0 ? "X" : "Y")} side={(sign > 0 ? "upper" : "lower")} shift={shift}");
        TrackCase track = new(
            SyntheticTrack.FromOutlines(name, [.. loops]), footprint, 1.0, "barriers at full reach");
        if (track.Grid is null
            || (axis == 0 ? track.Grid.OriginX : track.Grid.OriginY) != origin
            || track.Grid.InverseCellSize != anchorGrid.InverseCellSize) {
            result.Fail(name + ": the grid moved when the barriers were added.");
            return new ComparisonTally();
        }

        // The corner must point from the vehicle toward the barrier.
        double direction = axis == 0 ? (sign > 0 ? Math.PI : 0.0) : (sign > 0 ? -Math.PI / 2.0 : Math.PI / 2.0);
        List<RectanglePose> poses = [];
        float[] corners = new float[8];
        foreach ((double boundary, float _, float across) in pins) {
            for (int corner = 0; corner < 4; ++corner) {
                float yaw = ReachYaw(footprint, corner, direction);
                for (int step = -PositionSteps; step <= PositionSteps; ++step) {
                    float along = ExactNumber.Step((float)boundary, step);
                    (float x, float y) = Point(axis, along, across);
                    RectanglePose pose = new(x, y, yaw);
                    poses.Add(pose);
                    DetectorInternals.Transform(footprint, pose, corners);
                    if (corners[2 * corner + (axis == 0 ? 1 : 0)] == across) {
                        ++aligned;
                    }
                }
            }
        }

        poseCount += poses.Count;
        return Comparison.Compare(
            result, context, track, track.Detector, track.Grid, footprint, [.. poses], "full reach");
    }

    private static ComparisonTally CellCorners(SuiteResult result, ValidationContext context, TrackCase track) {
        ExpandedGridView grid = track.Grid ?? throw new InvalidOperationException(track.Name + " has no expanded grid.");
        RectangleLocalBounds footprint = track.IndexFootprint;
        double cell = 1.0 / grid.InverseCellSize;
        Random random = new(8301);
        float[] yaws = new float[20];
        for (int corner = 0; corner < 4; ++corner) {
            for (int quarter = 0; quarter < 4; ++quarter) {
                yaws[corner * 4 + quarter] = ReachYaw(footprint, corner, quarter * Math.PI / 2.0);
            }
        }

        int stride = context.WorkScale >= 1.0 ? 1 : context.WorkScale >= 0.2 ? 2 : 5;
        List<RectanglePose> poses = [];
        for (int row = 0; row <= grid.RowCount; row += stride) {
            for (int column = 0; column <= grid.ColumnCount; column += stride) {
                float x = (float)(grid.OriginX + column * cell);
                float y = (float)(grid.OriginY + row * cell);
                for (int index = 16; index < yaws.Length; ++index) {
                    yaws[index] = (float)((random.NextDouble() * 2.0 - 1.0) * Math.PI);
                }

                for (int stepY = -1; stepY <= 1; ++stepY) {
                    for (int stepX = -1; stepX <= 1; ++stepX) {
                        foreach (float yaw in yaws) {
                            poses.Add(new RectanglePose(
                                ExactNumber.Step(x, stepX), ExactNumber.Step(y, stepY), yaw));
                        }
                    }
                }
            }
        }

        return Comparison.Compare(
            result, context, track, track.Detector, track.Grid, footprint, [.. poses], "cell corner");
    }

    private static ComparisonTally ReachContacts(SuiteResult result, ValidationContext context, TrackCase track) {
        RectangleLocalBounds footprint = track.IndexFootprint;
        OracleTrack oracle = track.Track.Oracle;
        Random random = new(8302);
        float[] scratch = new float[8];
        List<RectanglePose> poses = [];
        int count = context.Scaled(30_000);
        for (int iteration = 0; iteration < count; ++iteration) {
            int edge = random.Next(oracle.EdgeCount);
            int corner = random.Next(4);
            float yaw = ReachYaw(footprint, corner, random.Next(4) * Math.PI / 2.0);
            if (PoseSampler.TryLandCornerExactly(
                    footprint, corner, oracle.Ax[edge], oracle.Ay[edge], yaw, 3, scratch, out RectanglePose pose)) {
                poses.Add(pose);
            }
        }

        return Comparison.Compare(
            result, context, track, track.Detector, track.Grid, footprint, [.. poses], "corner at full reach on vertex",
            expected: true);
    }
}
