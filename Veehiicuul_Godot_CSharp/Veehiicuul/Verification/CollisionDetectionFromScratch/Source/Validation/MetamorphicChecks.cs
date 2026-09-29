using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>
/// Changes that must not change any answer: a different cell size, a different
/// index footprint, and a different order of the same barrier edges. Each
/// variant selects different index structures and code paths for the same
/// geometry, and every answer is compared with the exact oracle.
/// </summary>
internal static class MetamorphicChecks {
    private static readonly string[] TrackNames = [
        TrackCatalog.Circuit, TrackCatalog.CircuitCompact, TrackCatalog.Scattered,
        TrackCatalog.CircuitWide, TrackCatalog.RectangularSegmented, TrackCatalog.Rectangular,
    ];

    public static void Run(ValidationContext context, IReadOnlyList<TrackCase> tracks) {
        foreach (TrackCase track in tracks) {
            if (Array.IndexOf(TrackNames, track.Name) < 0) {
                continue;
            }

            context.Run($"Metamorphic: {track.Name}", result => RunTrack(result, context, track));
        }
    }

    private static void RunTrack(SuiteResult result, ValidationContext context, TrackCase track) {
        RectangleLocalBounds footprint = Footprints.Reference;
        OracleTrack oracle = track.Track.Oracle;
        List<RectanglePose> poseList = [];
        poseList.AddRange(PoseSampler.Uniform(oracle, context.Scaled(12_000), 8401, 10.0));
        poseList.AddRange(PoseSampler.NearBarrier(oracle, footprint, context.Scaled(12_000), 8402, 1e-7, 1.5));
        RectanglePose[] poses = [.. poseList];
        bool[] expected = new bool[poses.Length];
        Parallel.For(
            0, poses.Length, new ParallelOptions { MaxDegreeOfParallelism = context.Threads },
            () => new float[8],
            (index, _, corners) => {
                DetectorInternals.Transform(footprint, poses[index], corners);
                expected[index] = oracle.Intersects(corners);
                return corners;
            },
            _ => { });

        SortedDictionary<string, int> kinds = new(StringComparer.Ordinal);
        void Variant(string name, ColliderJson collider, RectangleLocalBounds index, double cellSizeScale) {
            TrackCollisionDetector detector;
            try {
                detector = new TrackCollisionDetector(collider, index, cellSizeScale);
            } catch (Exception exception) {
                result.Check(false, $"{track.Name}, {name}: construction failed with {exception.GetType().Name}.");
                return;
            }

            string kind = detector.UsesExpandedGrid
                ? "expanded grid"
                : (detector.OrdinaryEdgeCount == 0 ? "no grid" : detector.UsesDenseGrid ? "dense center grid" : "sparse center grid")
                    + (detector.OutlierEdgeCount == 0 ? string.Empty : detector.OutlierBvhNodeCount > 0 ? " + tree" : " + list");
            lock (kinds) {
                kinds[kind] = kinds.TryGetValue(kind, out int seen) ? seen + 1 : 1;
            }

            Parallel.For(
                0, poses.Length, new ParallelOptions { MaxDegreeOfParallelism = context.Threads },
                index => {
                    bool actual = detector.IsColliding(footprint, poses[index]);
                    if (actual != expected[index]) {
                        result.Fail(
                            $"{track.Name}, {name} ({kind}): indexed={actual}, exact={expected[index]}; "
                            + $"{Exact.Text(poses[index])}.");
                    }
                });
            result.AddCases(poses.Length);
        }

        ColliderJson original = track.Track.Collider;
        foreach (double cellSizeScale in new[] { 0.01, 0.125, 0.25, 0.5, 1.0, 2.0, 4.0, 16.0, 64.0, 1024.0 }) {
            Variant(
                string.Create(CultureInfo.InvariantCulture, $"cell size scale {cellSizeScale}"),
                original, footprint, cellSizeScale);
        }

        (string Name, RectangleLocalBounds Bounds)[] indexes = [
            ("index footprint small", Footprints.Small),
            ("index footprint narrow", Footprints.Narrow),
            ("index footprint oversized", Footprints.Oversized),
            ("index footprint with shifted origin", Footprints.ShiftedOrigin),
            ("index footprint one thousandth", new RectangleLocalBounds(-0.0015f, -0.003f, 0.0015f, 0.003f)),
            ("index footprint one thousand", new RectangleLocalBounds(-1500f, -3000f, 1500f, 3000f)),
        ];
        foreach ((string name, RectangleLocalBounds index) in indexes) {
            Variant(name, original, index, 0.5);
            Variant(name + ", cell size scale 4", original, index, 4.0);
        }

        Variant("outlines in reverse order", Reorder(original, reverseOutlines: true, false, 0, false), footprint, 0.5);
        Variant("vertices in reverse order", Reorder(original, false, reverseVertices: true, 0, false), footprint, 0.5);
        Variant("first vertex moved by one", Reorder(original, false, false, 1, false), footprint, 0.5);
        Variant("first vertex moved by seven", Reorder(original, false, false, 7, false), footprint, 0.5);
        Variant("every outline twice", Reorder(original, false, false, 0, duplicate: true), footprint, 0.5);
        Variant(
            "reversed, moved, and doubled, on the fallback index",
            Reorder(original, true, true, 3, true), new RectangleLocalBounds(-0.0015f, -0.003f, 0.0015f, 0.003f), 0.5);

        result.Fact("Index kinds exercised", string.Join("; ", DescribeKinds(kinds)));
    }

    private static IEnumerable<string> DescribeKinds(SortedDictionary<string, int> kinds) {
        foreach (KeyValuePair<string, int> pair in kinds) {
            yield return $"{pair.Key} x{pair.Value}";
        }
    }

    private static ColliderJson Reorder(
        ColliderJson source, bool reverseOutlines, bool reverseVertices, int rotation, bool duplicate) {
        ColliderJson result = new();
        foreach (Outline outline in source.Outlines) {
            List<CoordinateXY> vertices = new(outline.Vertices.Count);
            int count = outline.Vertices.Count;
            for (int index = 0; index < count; ++index) {
                vertices.Add(outline.Vertices[(index + rotation) % count]);
            }

            if (reverseVertices) {
                vertices.Reverse();
            }

            result.Outlines.Add(new Outline { Vertices = vertices });
            if (duplicate) {
                result.Outlines.Add(new Outline { Vertices = [.. vertices] });
            }
        }

        if (reverseOutlines) {
            result.Outlines.Reverse();
        }

        return result;
    }
}
