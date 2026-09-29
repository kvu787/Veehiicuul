using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>Counters gathered while comparing the detector with the references.</summary>
internal sealed class ComparisonTally {
    public long Queries;
    public long Contacts;
    public long OriginLookups;
    public long CenterLookups;
    public long FullScans;
    public long IndexedWithoutGrid;
    public long Candidates;
    public long EmptyLookups;
    public int LargestCandidateList;
    public long IntersectingEdges;
    public long ToleranceClear;
    public long ToleranceColliding;
    public long ToleranceUndecided;

    public void Add(ComparisonTally other) {
        this.Queries += other.Queries;
        this.Contacts += other.Contacts;
        this.OriginLookups += other.OriginLookups;
        this.CenterLookups += other.CenterLookups;
        this.FullScans += other.FullScans;
        this.IndexedWithoutGrid += other.IndexedWithoutGrid;
        this.Candidates += other.Candidates;
        this.EmptyLookups += other.EmptyLookups;
        this.LargestCandidateList = Math.Max(this.LargestCandidateList, other.LargestCandidateList);
        this.IntersectingEdges += other.IntersectingEdges;
        this.ToleranceClear += other.ToleranceClear;
        this.ToleranceColliding += other.ToleranceColliding;
        this.ToleranceUndecided += other.ToleranceUndecided;
    }

    public string Summary() {
        long lookups = this.OriginLookups + this.CenterLookups;
        double meanCandidates = lookups == 0 ? 0.0 : (double)this.Candidates / lookups;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"queries={this.Queries}; contacts={this.Contacts}; originLookups={this.OriginLookups}; "
            + $"centerLookups={this.CenterLookups}; fullScans={this.FullScans}; "
            + $"fallbackIndex={this.IndexedWithoutGrid}; emptyLookups={this.EmptyLookups}; "
            + $"meanCandidates={meanCandidates:F2}; largestCandidateList={this.LargestCandidateList}; "
            + $"intersectingEdges={this.IntersectingEdges}; toleranceClear={this.ToleranceClear}; "
            + $"toleranceColliding={this.ToleranceColliding}; toleranceUndecided={this.ToleranceUndecided}");
    }
}

/// <summary>
/// The central comparison. For one pose it requires that
/// 1. the indexed query, the detector's linear scan, and the exact parametric
///    oracle on the detector's rounded corners all agree;
/// 2. every edge that truly intersects is among the candidates the detector's
///    own cell lookup returns, which a matching yes/no answer cannot show;
/// 3. the answer matches a distance-based classification that is built from
///    the pose convention alone, whenever that classification is decisive.
/// </summary>
internal static class Comparison {
    private sealed class Worker {
        public readonly float[] Corners = new float[8];
        public readonly List<int> Hits = [];
        public readonly ComparisonTally Tally = new();
    }

    public static ComparisonTally Compare(
        SuiteResult result,
        ValidationContext context,
        TrackCase track,
        TrackCollisionDetector detector,
        ExpandedGridView? grid,
        RectangleLocalBounds footprint,
        RectanglePose[] poses,
        string label,
        bool? expected = null) {
        ComparisonTally total = new();
        Lock merge = new();
        OracleTrack oracle = track.Track.Oracle;
        double tolerance = ToleranceFor(track, footprint);
        Parallel.For(
            0,
            poses.Length,
            new ParallelOptions { MaxDegreeOfParallelism = context.Threads },
            () => new Worker(),
            (index, _, worker) => {
                CompareOne(result, track, detector, grid, oracle, footprint, poses[index], label, expected,
                    tolerance, (index & 3) == 0, worker);
                return worker;
            },
            worker => {
                lock (merge) {
                    total.Add(worker.Tally);
                }
            });
        result.AddCases(poses.Length);
        return total;
    }

    /// <summary>
    /// Distances below this are left to the exact comparison. It covers the binary32
    /// rounding of corners at the track's coordinate magnitude with a wide margin.
    /// </summary>
    public static double ToleranceFor(TrackCase track, RectangleLocalBounds footprint) {
        OracleTrack oracle = track.Track.Oracle;
        double magnitude = Math.Max(
            Math.Max(Math.Abs((double)oracle.BoundsMinX), Math.Abs((double)oracle.BoundsMaxX)),
            Math.Max(Math.Abs((double)oracle.BoundsMinY), Math.Abs((double)oracle.BoundsMaxY)));
        magnitude += Math.Abs((double)footprint.MinX) + Math.Abs((double)footprint.MaxX)
            + Math.Abs((double)footprint.MinY) + Math.Abs((double)footprint.MaxY);
        return Math.Max(1e-5 * track.FootprintScale, 64.0 * ExactNumber.UnitInLastPlace((float)magnitude));
    }

    private static void CompareOne(
        SuiteResult result,
        TrackCase track,
        TrackCollisionDetector detector,
        ExpandedGridView? grid,
        OracleTrack oracle,
        RectangleLocalBounds footprint,
        RectanglePose pose,
        string label,
        bool? expected,
        double tolerance,
        bool classify,
        Worker worker) {
        ComparisonTally tally = worker.Tally;
        ++tally.Queries;
        bool indexed, linear;
        try {
            indexed = detector.IsColliding(footprint, pose);
            linear = detector.IsCollidingLinear(footprint, pose);
            DetectorInternals.Transform(footprint, pose, worker.Corners);
        } catch (Exception exception) {
            result.Fail($"{track.Name} {label}: unexpected {exception.GetType().Name}; "
                + $"{Exact.Text(footprint)} {Exact.Text(pose)}.");
            return;
        }

        worker.Hits.Clear();
        bool exact = oracle.Intersects(worker.Corners, worker.Hits, useBoundsFilter: true);
        tally.IntersectingEdges += worker.Hits.Count;
        if (exact) {
            ++tally.Contacts;
        }

        if (indexed != exact) {
            result.Fail($"{track.Name} {label}: indexed={indexed}, exact={exact}; "
                + $"{Exact.Text(footprint)} {Exact.Text(pose)}.");
        }

        if (linear != exact) {
            result.Fail($"{track.Name} {label}: linear={linear}, exact={exact}; "
                + $"{Exact.Text(footprint)} {Exact.Text(pose)}.");
        }

        if (expected is bool known && exact != known) {
            result.Fail($"{track.Name} {label}: expected {known} by construction, exact={exact}; "
                + $"{Exact.Text(footprint)} {Exact.Text(pose)}.");
        }

        if (grid is null) {
            ++tally.IndexedWithoutGrid;
        } else {
            CheckCandidates(result, track, grid, footprint, pose, label, worker);
        }

        if (classify) {
            ToleranceVerdict verdict = ToleranceOracle.Classify(
                oracle, footprint.MinX, footprint.MinY, footprint.MaxX, footprint.MaxY,
                pose.PositionX, pose.PositionY, pose.RotationRadians, tolerance);
            switch (verdict) {
            case ToleranceVerdict.Clear:
                ++tally.ToleranceClear;
                if (indexed) {
                    result.Fail($"{track.Name} {label}: reports contact although every barrier is more than "
                        + $"{tolerance:R} away; {Exact.Text(footprint)} {Exact.Text(pose)}.");
                }

                break;
            case ToleranceVerdict.Colliding:
                ++tally.ToleranceColliding;
                if (!indexed) {
                    result.Fail($"{track.Name} {label}: reports clear although a barrier crosses the perimeter "
                        + $"by more than {tolerance:R}; {Exact.Text(footprint)} {Exact.Text(pose)}.");
                }

                break;
            default:
                ++tally.ToleranceUndecided;
                break;
            }
        }
    }

    private static void CheckCandidates(
        SuiteResult result,
        TrackCase track,
        ExpandedGridView grid,
        RectangleLocalBounds footprint,
        RectanglePose pose,
        string label,
        Worker worker) {
        ComparisonTally tally = worker.Tally;
        (int Offset, int Count) range;
        if (grid.Supports(footprint, pose)) {
            ++tally.OriginLookups;
            range = grid.GetRange(pose.PositionX, pose.PositionY);
        } else if (grid.SupportsCentered(footprint, pose)) {
            ++tally.CenterLookups;
            float[] corners = worker.Corners;
            range = grid.GetRange(
                ((double)corners[0] + corners[4]) * 0.5,
                ((double)corners[1] + corners[5]) * 0.5);
        } else {
            ++tally.FullScans;
            return;
        }

        tally.Candidates += range.Count;
        tally.LargestCandidateList = Math.Max(tally.LargestCandidateList, range.Count);
        if (range.Count == 0) {
            ++tally.EmptyLookups;
        }

        foreach (int edge in worker.Hits) {
            bool listed = false;
            for (int index = range.Offset; index < range.Offset + range.Count; ++index) {
                if (grid.EdgeIds[index] == edge) {
                    listed = true;
                    break;
                }
            }

            if (!listed) {
                result.Fail($"{track.Name} {label}: intersecting edge {edge} is not a candidate; "
                    + $"{Exact.Text(footprint)} {Exact.Text(pose)}.");
            }
        }
    }
}

internal static class DifferentialChecks {
    public static void Run(ValidationContext context, IReadOnlyList<TrackCase> tracks) {
        foreach (TrackCase track in tracks) {
            context.Run($"Differential: {track.Name} ({track.IndexKind})", result => RunTrack(result, context, track));
        }
    }

    private static void RunTrack(SuiteResult result, ValidationContext context, TrackCase track) {
        (string Name, RectangleLocalBounds Bounds)[] footprints = [
            ("reference", track.Scale(Footprints.Reference)),
            ("small", track.Scale(Footprints.Small)),
            ("narrow", track.Scale(Footprints.Narrow)),
            ("shifted origin", track.Scale(Footprints.ShiftedOrigin)),
            ("oversized", track.Scale(Footprints.Oversized)),
            ("long", track.Scale(Footprints.Long)),
            ("broad", track.Scale(Footprints.Broad)),
            ("long with shifted origin", track.Scale(Footprints.LongShifted)),
        ];
        OracleTrack oracle = track.Track.Oracle;
        double scale = track.FootprintScale;
        // String hash codes differ between processes; this sum does not.
        int seed = 31000;
        foreach (char character in track.Name) {
            seed = seed * 31 + character;
        }

        seed = 31000 + Math.Abs(seed % 100000);
        // Tracks with very many edges cost more per exact comparison.
        double weight = oracle.EdgeCount > 20000 ? 0.15 : oracle.EdgeCount > 4000 ? 0.5 : 1.0;
        ComparisonTally total = new();
        float[] scratch = new float[8];
        foreach ((string name, RectangleLocalBounds footprint) in footprints) {
            total.Add(Comparison.Compare(
                result, context, track, track.Detector, track.Grid, footprint,
                PoseSampler.Uniform(oracle, context.Scaled((int)(24000 * weight)), seed++, 12.0 * scale),
                name + " uniform"));
            total.Add(Comparison.Compare(
                result, context, track, track.Detector, track.Grid, footprint,
                PoseSampler.NearBarrier(
                    oracle, footprint, context.Scaled((int)(24000 * weight)), seed++, 1e-7 * scale, 1.5 * scale),
                name + " near barrier"));

            RectanglePose[] landed = ExactVertexContacts(
                oracle, footprint, context.Scaled((int)(3000 * weight)), seed++, scratch, out int attempts);
            result.Fact(
                $"{name}: exact vertex contacts",
                $"{landed.Length} constructed from {attempts} attempts");
            total.Add(Comparison.Compare(
                result, context, track, track.Detector, track.Grid, footprint, landed,
                name + " corner on vertex", expected: true));

            if (track.Track.HasCenterline) {
                foreach (double lateral in new[] { 0.0, 0.55, -0.55 }) {
                    total.Add(Comparison.Compare(
                        result, context, track, track.Detector, track.Grid, footprint,
                        track.Track.LapPoses(context.Scaled((int)(4000 * weight)), 0.37 * scale, lateral),
                        string.Create(CultureInfo.InvariantCulture, $"{name} lap at lateral {lateral:F2}")));
                }
            }
        }

        result.Fact("Totals", total.Summary());
    }

    /// <summary>Poses whose rounded corner coincides with a barrier vertex, which is a contact.</summary>
    internal static RectanglePose[] ExactVertexContacts(
        OracleTrack oracle, RectangleLocalBounds footprint, int count, int seed, float[] scratch, out int attempts) {
        Random random = new(seed);
        List<RectanglePose> poses = new(count);
        attempts = 0;
        while (poses.Count < count && attempts < count * 4) {
            ++attempts;
            int edge = random.Next(oracle.EdgeCount);
            int corner = random.Next(4);
            float yaw = (float)((random.NextDouble() * 2.0 - 1.0) * Math.PI);
            if (PoseSampler.TryLandCornerExactly(
                    footprint, corner, oracle.Ax[edge], oracle.Ay[edge], yaw, 3, scratch, out RectanglePose pose)) {
                poses.Add(pose);
            }
        }

        return [.. poses];
    }
}
