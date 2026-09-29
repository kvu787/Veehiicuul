using System;
using System.Collections.Generic;
using System.Globalization;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>
/// Contact semantics on a lattice, where the expected answer needs no
/// floating point at all. Barrier vertices, footprint limits, and positions
/// are multiples of 1/4; rotations have cosine and sine from the 3-4-5
/// triangle or from quarter turns. Every ideal corner is then a lattice
/// point, and expected results come from 64-bit integer arithmetic on
/// coordinates multiplied by twenty.
///
/// The lattice makes touching and collinear configurations common: corners on
/// vertices, corners on edge interiors, sides lying along edges, sides
/// containing an entire edge, and gaps of exactly one lattice step.
/// </summary>
internal static class ContactChecks {
    private const int Unit = 20;

    // Cosine and sine of the clockwise yaw, in fifths.
    private static readonly (int Cosine, int Sine)[] Rotations = [
        (5, 0), (0, 5), (-5, 0), (0, -5),
        (3, 4), (4, 3), (-3, 4), (-4, 3), (3, -4), (4, -3), (-3, -4), (-4, -3),
    ];

    // Barriers in quarter units. Directions include the axes, the diagonal, and 3-4-5 slopes.
    private static readonly (int X, int Y)[][] Barriers = [
        [(176, 176), (184, 176), (184, 196), (176, 196)],
        [(200, 200), (202, 200), (202, 202), (200, 202)],
        [(152, 208), (158, 200), (162, 203), (156, 211)],
        [(208, 160), (216, 168), (208, 176), (200, 168)],
        [(176, 152), (200, 120), (200, 152)],
        [(224, 224), (225, 224), (224, 225)],
        [(144, 144), (160, 144), (160, 145), (144, 145)],
    ];

    public static void Run(ValidationContext context) {
        context.Run("Contact: lattice poses against integer arithmetic", result => Lattice(result, context));
        context.Run("Contact: named configurations", Named);
    }

    private static void Lattice(SuiteResult result, ValidationContext context) {
        (float X, float Y)[][] loops = new (float, float)[Barriers.Length][];
        List<(long Ax, long Ay, long Bx, long By)> edges = [];
        for (int barrier = 0; barrier < Barriers.Length; ++barrier) {
            (int X, int Y)[] points = Barriers[barrier];
            loops[barrier] = new (float, float)[points.Length];
            for (int index = 0; index < points.Length; ++index) {
                loops[barrier][index] = (points[index].X * 0.25f, points[index].Y * 0.25f);
                (int nextX, int nextY) = points[(index + 1) % points.Length];
                edges.Add((points[index].X * 5L, points[index].Y * 5L, nextX * 5L, nextY * 5L));
            }
        }

        SyntheticTrack track = SyntheticTrack.FromOutlines("Lattice", loops);
        RectangleLocalBounds footprint = Footprints.Lattice;
        (string Name, TrackCollisionDetector Detector)[] detectors = [
            ("index of the same footprint", new TrackCollisionDetector(track.Collider, footprint)),
            ("index of a larger footprint", new TrackCollisionDetector(track.Collider, Footprints.Oversized)),
            ("index of a smaller footprint", new TrackCollisionDetector(track.Collider, Footprints.Small)),
            ("index with tiny cells", new TrackCollisionDetector(track.Collider, footprint, 0.001)),
        ];
        // Footprint limits in twentieths: 1.25 and 2.5.
        (long X, long Y)[] local = [(-25, -50), (25, -50), (25, 50), (-25, 50)];
        float[] corners = new float[8];
        long[] expectedCorners = new long[8];
        long contacts = 0, touching = 0, poses = 0, offLattice = 0;
        int stride = context.WorkScale >= 1.0 ? 1 : context.WorkScale >= 0.2 ? 2 : 4;
        foreach ((int cosine, int sine) in Rotations) {
            float yaw = (float)Math.Atan2(sine, cosine);
            for (int quarterY = 128; quarterY <= 240; quarterY += stride) {
                for (int quarterX = 128; quarterX <= 240; quarterX += stride) {
                    ++poses;
                    RectanglePose pose = new(quarterX * 0.25f, quarterY * 0.25f, yaw);
                    // Clockwise rotation: (x, y) maps to (x cos + y sin, y cos - x sin).
                    for (int corner = 0; corner < 4; ++corner) {
                        (long x, long y) = local[corner];
                        expectedCorners[2 * corner] = quarterX * 5L + (x * cosine + y * sine) / 5;
                        expectedCorners[2 * corner + 1] = quarterY * 5L + (y * cosine - x * sine) / 5;
                    }

                    DetectorInternals.Transform(footprint, pose, corners);
                    bool onLattice = true;
                    for (int index = 0; index < 8; ++index) {
                        if (corners[index] * (float)Unit != expectedCorners[index]) {
                            onLattice = false;
                        }
                    }

                    result.Check(
                        onLattice,
                        $"Rounded corners leave the lattice; rotation ({cosine}/5, {sine}/5) {Exact.Text(pose)}.");
                    if (!onLattice) {
                        ++offLattice;
                        continue;
                    }

                    bool expected = false, proper = false;
                    foreach ((long ax, long ay, long bx, long by) in edges) {
                        for (int side = 0; side < 4; ++side) {
                            int next = (side + 1) & 3;
                            IntegerGeometry.Classify(
                                ax, ay, bx, by,
                                expectedCorners[2 * side], expectedCorners[2 * side + 1],
                                expectedCorners[2 * next], expectedCorners[2 * next + 1],
                                out bool intersects, out bool crossesProperly);
                            expected |= intersects;
                            proper |= crossesProperly;
                        }
                    }

                    if (expected) {
                        ++contacts;
                        if (!proper) {
                            ++touching;
                        }
                    }

                    bool exact = track.Oracle.Intersects(corners, null, useBoundsFilter: false);
                    result.Check(
                        exact == expected,
                        $"Exact oracle={exact}, integers={expected}; rotation ({cosine}/5, {sine}/5) {Exact.Text(pose)}.");
                    foreach ((string name, TrackCollisionDetector detector) in detectors) {
                        bool indexed = detector.IsColliding(footprint, pose);
                        bool linear = detector.IsCollidingLinear(footprint, pose);
                        result.Check(
                            indexed == expected,
                            $"{name}: indexed={indexed}, integers={expected}; rotation ({cosine}/5, {sine}/5) "
                            + $"{Exact.Text(pose)}.");
                        result.Check(
                            linear == expected,
                            $"{name}: linear={linear}, integers={expected}; rotation ({cosine}/5, {sine}/5) "
                            + $"{Exact.Text(pose)}.");
                    }
                }
            }
        }

        result.Fact("Poses", poses);
        result.Fact("Contacts", contacts);
        result.Fact("Contacts that only touch, without any proper crossing", touching);
        result.Fact("Poses whose rounded corners left the lattice", offLattice);
    }

    /// <summary>Configurations whose answer follows from their description.</summary>
    private static void Named(SuiteResult result) {
        // One barrier edge from (10, 4) to (10, 6) belongs to a thin sliver to its right.
        SyntheticTrack sliver = SyntheticTrack.FromOutlines(
            "Sliver", [(10f, 4f), (10.5f, 5f), (10f, 6f)]);
        // A long wall along X = 10 from Y = -20 to Y = 20, closed far to the right.
        SyntheticTrack wall = SyntheticTrack.FromOutlines(
            "Wall", [(10f, -20f), (40f, 0f), (10f, 20f)]);
        RectangleLocalBounds box = new(-2f, -3f, 2f, 3f);
        (string Name, SyntheticTrack Track, RectanglePose Pose, bool Expected)[] cases = [
            ("side contains a whole barrier edge", sliver, new RectanglePose(8f, 5f, 0f), true),
            ("side contains a whole barrier edge, from the inside", sliver, new RectanglePose(12f, 5f, 0f), true),
            ("side lies inside a longer barrier edge", wall, new RectanglePose(8f, 0f, 0f), true),
            // Two spacings of the corner coordinate, so that no rounding tie decides the case.
            ("side two steps short of the barrier edge", wall,
                new RectanglePose(8f - MathF.ScaleB(1f, -19), 0f, 0f), false),
            ("side two steps past the barrier edge", wall,
                new RectanglePose(8f + MathF.ScaleB(1f, -19), 0f, 0f), true),
            ("side overlaps the end of a barrier edge", wall, new RectanglePose(8f, 19f, 0f), true),
            ("corner meets the end of a barrier edge", wall, new RectanglePose(8f, 23f, 0f), true),
            ("corner one step beyond the end of a barrier edge", wall,
                new RectanglePose(8f, MathF.BitIncrement(23f), 0f), false),
            ("side collinear with a barrier edge, beyond its end", wall, new RectanglePose(8f, 30f, 0f), false),
            ("sliver entirely inside the rectangle", sliver, new RectanglePose(10.25f, 5f, 0f), false),
            ("rectangle entirely inside the wall triangle", wall, new RectanglePose(20f, 0f, 0.4f), false),
            ("corner enters the wall", wall, new RectanglePose(7f, 0f, MathF.PI / 4f), true),
        ];
        float[] corners = new float[8];
        foreach ((string name, SyntheticTrack track, RectanglePose pose, bool expected) in cases) {
            foreach (RectangleLocalBounds index in new[] { box, Footprints.Small, Footprints.Oversized }) {
                TrackCollisionDetector detector = new(track.Collider, index);
                DetectorInternals.Transform(box, pose, corners);
                result.Check(
                    track.Oracle.Intersects(corners, null, useBoundsFilter: false) == expected,
                    $"{name}: the exact oracle disagrees with the description.");
                result.Check(detector.IsColliding(box, pose) == expected, $"{name}: indexed query.");
                result.Check(detector.IsCollidingLinear(box, pose) == expected, $"{name}: linear query.");
            }
        }
    }
}

/// <summary>Closed-segment intersection on 64-bit integers.</summary>
internal static class IntegerGeometry {
    public static void Classify(
        long ax, long ay, long bx, long by, long cx, long cy, long dx, long dy,
        out bool intersects, out bool crossesProperly) {
        int first = Math.Sign((bx - ax) * (cy - ay) - (by - ay) * (cx - ax));
        int second = Math.Sign((bx - ax) * (dy - ay) - (by - ay) * (dx - ax));
        int third = Math.Sign((dx - cx) * (ay - cy) - (dy - cy) * (ax - cx));
        int fourth = Math.Sign((dx - cx) * (by - cy) - (dy - cy) * (bx - cx));
        crossesProperly = first * second < 0 && third * fourth < 0;
        intersects = crossesProperly
            || (first == 0 && Within(ax, ay, bx, by, cx, cy))
            || (second == 0 && Within(ax, ay, bx, by, dx, dy))
            || (third == 0 && Within(cx, cy, dx, dy, ax, ay))
            || (fourth == 0 && Within(cx, cy, dx, dy, bx, by));
    }

    private static bool Within(long ax, long ay, long bx, long by, long px, long py) {
        return px >= Math.Min(ax, bx) && px <= Math.Max(ax, bx)
            && py >= Math.Min(ay, by) && py <= Math.Max(ay, by);
    }
}
