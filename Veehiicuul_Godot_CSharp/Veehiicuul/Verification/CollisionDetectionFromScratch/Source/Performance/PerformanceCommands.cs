using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>Single-call timings taken one call at a time, with the caches disturbed in between.</summary>
internal sealed class SingleCallMeasurement {
    public required string Name { get; init; }
    public required int EvictedBytes { get; init; }
    public required int Samples { get; init; }
    public required double MeanNanoseconds { get; init; }
    public required double TimerMeanNanoseconds { get; init; }
    public required double MedianNanoseconds { get; init; }
    public required double Percentile90Nanoseconds { get; init; }
    public required double Percentile99Nanoseconds { get; init; }
    public required double MaximumNanoseconds { get; init; }
    public required long Contacts { get; init; }

    /// <summary>Mean with the mean cost of reading the timer removed.</summary>
    public double NetMeanNanoseconds => this.MeanNanoseconds - this.TimerMeanNanoseconds;
}

internal sealed class ConstructionMeasurement {
    public required string Name { get; init; }
    public required string IndexKind { get; init; }
    public required int Edges { get; init; }
    public required long GridCells { get; init; }
    public required int References { get; init; }
    public required double FirstMilliseconds { get; init; }
    public required double[] Milliseconds { get; init; }
    public required long AllocatedBytes { get; init; }
    public required long RetainedBytes { get; init; }
    public double MedianMilliseconds => Statistics.Percentile(this.Milliseconds, 0.5);
}

internal static class PerformanceCommands {
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static int Measure(Options options) {
        string suite = options.Text("suite", "queries");
        string affinity = options.Text("affinity", "performance");
        string priority = options.Text("priority", "high");
        string topology = ProcessorTopology.Apply(affinity, priority);
        Console.WriteLine("Processor: " + topology);
        Console.WriteLine(
            $"Runtime settings: TieredCompilation={Setting("DOTNET_TieredCompilation")}; "
            + $"TieredPGO={Setting("DOTNET_TieredPGO")}; ReadyToRun={Setting("DOTNET_ReadyToRun")}; "
            + $"server collection={GCSettings.IsServerGC}; latency mode={GCSettings.LatencyMode}");

        List<Measurement> measurements = [];
        List<SingleCallMeasurement> singleCalls = [];
        List<ConstructionMeasurement> constructions = [];
        void Add(Measurement measurement) {
            measurements.Add(measurement);
            Console.WriteLine(measurement.Summary());
        }

        switch (suite) {
        case "queries":
            Queries(Add, options.List("tracks"));
            break;
        case "paths":
            Paths(Add);
            break;
        case "extent":
            Extent(Add);
            break;
        case "spacing":
            Spacing(Add);
            break;
        case "cellsize":
            CellSize(Add, options.List("tracks"));
            break;
        case "breakdown":
            Breakdown(Add);
            break;
        case "indexfootprint":
            IndexFootprint(Add);
            break;
        case "construction":
            Construction(constructions);
            break;
        case "single":
            SingleCalls(singleCalls);
            break;
        default:
            throw new ArgumentException("Unknown suite: " + suite);
        }

        string output = options.Text("output", string.Empty);
        if (output.Length > 0) {
            File.WriteAllText(output, JsonSerializer.Serialize(new {
                Suite = suite,
                Framework = RuntimeInformation.FrameworkDescription,
                OperatingSystem = RuntimeInformation.OSDescription,
                Processor = topology,
                TieredCompilation = Setting("DOTNET_TieredCompilation"),
                TieredPGO = Setting("DOTNET_TieredPGO"),
                StopwatchFrequency = Stopwatch.Frequency,
                Bench.SampleCount,
                Measurements = measurements,
                SingleCalls = singleCalls,
                Constructions = constructions,
                Bench.Sink,
            }, JsonOptions));
        }

        return 0;
    }

    private static string Setting(string name) {
        return Environment.GetEnvironmentVariable(name) ?? "default";
    }

    private static void Fact(Measurement measurement, TrackCase track) {
        TrackCollisionDetector detector = track.Detector;
        measurement.Facts["Track"] = track.Name;
        measurement.Facts["IndexKind"] = track.IndexKind;
        measurement.Facts["Edges"] = detector.EdgeCount.ToString(CultureInfo.InvariantCulture);
        measurement.Facts["GridCells"] = detector.GridCellCount.ToString(CultureInfo.InvariantCulture);
        measurement.Facts["References"] = detector.StoredGridEdgeReferenceCount.ToString(CultureInfo.InvariantCulture);
        measurement.Facts["LongEdges"] = detector.OutlierEdgeCount.ToString(CultureInfo.InvariantCulture);
        measurement.Facts["CellSize"] = detector.CellSize.ToString("R", CultureInfo.InvariantCulture);
    }

    private static Measurement Timed<T>(string name, TrackCase track, T workload) where T : struct, IWorkload {
        Measurement measurement = Bench.Run(name, workload);
        Fact(measurement, track);
        return measurement;
    }

    private static void Queries(Action<Measurement> add, HashSet<string> only) {
        string[] names = [
            TrackCatalog.Circuit, TrackCatalog.CircuitFine, TrackCatalog.CircuitCompact, TrackCatalog.Scattered,
            TrackCatalog.RectangularSegmented, TrackCatalog.Rectangular, TrackCatalog.CircuitFar,
            TrackCatalog.CircuitWide, TrackCatalog.CircuitVast, TrackCatalog.ScatteredRemote, TrackCatalog.Crowded,
        ];
        foreach (string name in names) {
            if (only.Count > 0 && !only.Contains(name)) {
                continue;
            }

            TrackCase track = TrackCatalog.Build(name);
            RectangleLocalBounds footprint = track.IndexFootprint;
            TrackCollisionDetector detector = track.Detector;
            Console.WriteLine($"Track {name}: {track.IndexKind}; edges={detector.EdgeCount}; "
                + $"cells={detector.GridCellCount}; references={detector.StoredGridEdgeReferenceCount}");
            if (track.Track.HasCenterline) {
                RectanglePose[] lap = Workloads.Lap(track, 0.0, 0.25);
                add(Timed($"{name}/LapCenter", track, new IndexedQueries(detector, footprint, lap)));
                add(Timed($"{name}/LapCenter/Linear", track, new LinearQueries(detector, footprint, lap)));
                RectanglePose[] beside = Workloads.LapBesideBarrier(track, footprint, 0.1, 0.25);
                if (beside.Length >= 256) {
                    add(Timed($"{name}/LapBesideBarrier", track, new IndexedQueries(detector, footprint, beside)));
                }
            }

            RectanglePose[] mixed = Workloads.Uniform(track, 8601);
            add(Timed($"{name}/UniformMixed", track, new IndexedQueries(detector, footprint, mixed)));
            add(Timed($"{name}/UniformMixed/Linear", track, new LinearQueries(detector, footprint, mixed)));
            add(Timed($"{name}/UniformClear", track, new IndexedQueries(
                detector, footprint, Workloads.UniformClear(track, footprint, 8602))));
            add(Timed($"{name}/NearMiss", track, new IndexedQueries(
                detector, footprint, Workloads.NearMisses(track, footprint, 8603))));
            add(Timed($"{name}/Contact", track, new IndexedQueries(
                detector, footprint, Workloads.Contacts(track, footprint, 8604))));
            add(Timed($"{name}/Outside", track, new IndexedQueries(detector, footprint, Workloads.Outside(track))));
            if (track.Grid is not null) {
                RectanglePose[] empty = Workloads.EmptyCells(track, 8605);
                if (empty.Length >= 256) {
                    add(Timed($"{name}/EmptyCell", track, new IndexedQueries(detector, footprint, empty)));
                }

                RectanglePose[] busiest = Workloads.BusiestClearCell(track, footprint, 8606, out int candidates);
                Measurement measurement = Timed(
                    $"{name}/BusiestClearCell", track, new IndexedQueries(detector, footprint, busiest));
                measurement.Facts["Candidates"] = candidates.ToString(CultureInfo.InvariantCulture);
                add(measurement);
            }
        }
    }

    /// <summary>The three expanded-grid routes and the loop overhead, on one track and one pose set.</summary>
    private static void Paths(Action<Measurement> add) {
        TrackCase track = TrackCatalog.Build(TrackCatalog.Circuit);
        RectanglePose[] mixed = Workloads.Uniform(track, 8601);
        add(Timed("Paths/LoopOnly", track, new EmptyLoop(mixed)));
        (string Name, RectangleLocalBounds Bounds)[] footprints = [
            ("OriginLookup/Reference", Footprints.Reference),
            ("OriginLookup/Small", Footprints.Small),
            ("CenterLookup/ShiftedOrigin", Footprints.ShiftedOrigin),
            ("FullScan/Oversized", Footprints.Oversized),
        ];
        foreach ((string name, RectangleLocalBounds footprint) in footprints) {
            add(Timed($"Paths/{name}", track, new IndexedQueries(track.Detector, footprint, mixed)));
            add(Timed($"Paths/{name}/Linear", track, new LinearQueries(track.Detector, footprint, mixed)));
        }
    }

    /// <summary>
    /// One circuit indexed for the vehicle that queries it and for larger vehicles. The
    /// collision manager indexes a track for the union of its vehicles, so a small vehicle
    /// queries an index whose reach is longer than it needs.
    /// </summary>
    private static void IndexFootprint(Action<Measurement> add) {
        TrackCase own = TrackCatalog.Build(TrackCatalog.Circuit);
        RectangleLocalBounds vehicle = own.IndexFootprint;
        (string Name, RectanglePose[] Poses)[] workloads = [
            ("LapCenter", Workloads.Lap(own, 0.0, 0.25)),
            ("NearMiss", Workloads.NearMisses(own, vehicle, 8603)),
            ("Contact", Workloads.Contacts(own, vehicle, 8604)),
        ];
        (string Name, RectangleLocalBounds Bounds)[] indexes = [
            ("Own", vehicle),
            // The union of the four vehicles of the engine-side fixture.
            ("OneAndAHalf", new RectangleLocalBounds(-2.25f, -4.335f, 2.25f, 4.5f)),
            ("Twice", new RectangleLocalBounds(-3f, -5.835f, 3f, 6f)),
        ];
        foreach ((string indexName, RectangleLocalBounds bounds) in indexes) {
            TrackCase track = new(own.Track, bounds, 1.0, "index footprint");
            foreach ((string workload, RectanglePose[] poses) in workloads) {
                Measurement measurement = Timed(
                    $"IndexFootprint/{indexName}/{workload}", track,
                    new IndexedQueries(track.Detector, vehicle, poses));
                measurement.Facts["IndexFootprint"] = string.Create(
                    CultureInfo.InvariantCulture,
                    $"({bounds.MinX:R}, {bounds.MinY:R}, {bounds.MaxX:R}, {bounds.MaxY:R})");
                if (track.Grid is not null) {
                    long candidates = 0;
                    foreach (RectanglePose pose in poses) {
                        candidates += track.Grid.GetRange(pose.PositionX, pose.PositionY).Count;
                    }

                    measurement.Facts["MeanCandidates"] = ((double)candidates / poses.Length).ToString(
                        "F2", CultureInfo.InvariantCulture);
                }

                add(measurement);
            }
        }
    }

    /// <summary>The same circuit shape at growing size, across the expanded grid's cell limit.</summary>
    private static void Extent(Action<Measurement> add) {
        foreach (double radius in new[] { 40.0, 80.0, 110.0, 140.0, 155.0, 160.0, 165.0, 170.0, 180.0, 200.0, 300.0, 600.0, 1200.0 }) {
            string name = string.Create(CultureInfo.InvariantCulture, $"Extent/Radius{radius:F0}");
            TrackCase track = new(
                SyntheticTrack.Circuit(name, 20260929, radius, 9.0, 2.0, 2.0),
                Footprints.Reference, 1.0, "extent sweep");
            RectangleLocalBounds footprint = track.IndexFootprint;
            add(Timed($"{name}/LapCenter", track, new IndexedQueries(
                track.Detector, footprint, Workloads.Lap(track, 0.0, 0.25))));
            add(Timed($"{name}/UniformClear", track, new IndexedQueries(
                track.Detector, footprint, Workloads.UniformClear(track, footprint, 8602))));
            add(Timed($"{name}/NearMiss", track, new IndexedQueries(
                track.Detector, footprint, Workloads.NearMisses(track, footprint, 8603))));
            add(Timed($"{name}/Contact", track, new IndexedQueries(
                track.Detector, footprint, Workloads.Contacts(track, footprint, 8604))));
        }
    }

    /// <summary>Edge length at two fixed sizes: inside and beyond the expanded grid's cell limit.</summary>
    private static void Spacing(Action<Measurement> add) {
        foreach (double radius in new[] { 110.0, 200.0 }) {
            foreach (double spacing in new[] { 0.25, 0.5, 1.0, 1.4, 1.6, 2.0, 3.0, 4.0, 8.0 }) {
                string name = string.Create(
                    CultureInfo.InvariantCulture, $"Spacing/Radius{radius:F0}/Edge{spacing:F2}");
                TrackCase track = new(
                    SyntheticTrack.Circuit(name, 20260929, radius, 9.0, spacing, spacing),
                    Footprints.Reference, 1.0, "edge length sweep");
                RectangleLocalBounds footprint = track.IndexFootprint;
                add(Timed($"{name}/LapCenter", track, new IndexedQueries(
                    track.Detector, footprint, Workloads.Lap(track, 0.0, 0.25))));
                add(Timed($"{name}/NearMiss", track, new IndexedQueries(
                    track.Detector, footprint, Workloads.NearMisses(track, footprint, 8603))));
                add(Timed($"{name}/Contact", track, new IndexedQueries(
                    track.Detector, footprint, Workloads.Contacts(track, footprint, 8604))));
            }
        }
    }

    /// <summary>Cell size as a multiple of the shorter footprint side, for both index families.</summary>
    private static void CellSize(Action<Measurement> add, HashSet<string> only) {
        foreach (string trackName in new[] {
            TrackCatalog.Circuit, TrackCatalog.CircuitFine, TrackCatalog.CircuitWide, TrackCatalog.CircuitVast,
            TrackCatalog.ScatteredRemote, TrackCatalog.Crowded,
        }) {
            if (only.Count > 0 && !only.Contains(trackName)) {
                continue;
            }

            TrackCase reference = TrackCatalog.Build(trackName);
            RectangleLocalBounds footprint = reference.IndexFootprint;
            // Obstacle fields have no lap. Their clear poses are spread over the whole track.
            (string Name, RectanglePose[] Poses) clear = reference.Track.HasCenterline
                ? ("LapCenter", Workloads.Lap(reference, 0.0, 0.25))
                : ("UniformClear", Workloads.UniformClear(reference, footprint, 8602));
            RectanglePose[] nearMiss = Workloads.NearMisses(reference, footprint, 8603);
            RectanglePose[] contact = Workloads.Contacts(reference, footprint, 8604);
            foreach (double scale in new[] { 0.125, 0.25, 0.5, 1.0, 2.0, 4.0, 8.0, 16.0 }) {
                TrackCollisionDetector detector = new(reference.Track.Collider, footprint, scale);
                string name = string.Create(CultureInfo.InvariantCulture, $"CellSize/{trackName}/Scale{scale:F3}");
                foreach ((string workload, RectanglePose[] poses) in new[] {
                    clear, ("NearMiss", nearMiss), ("Contact", contact),
                }) {
                    Measurement measurement = Bench.Run($"{name}/{workload}", new IndexedQueries(detector, footprint, poses));
                    measurement.Facts["Track"] = trackName;
                    measurement.Facts["CellSizeScale"] = scale.ToString("R", CultureInfo.InvariantCulture);
                    measurement.Facts["CellSize"] = detector.CellSize.ToString("R", CultureInfo.InvariantCulture);
                    measurement.Facts["ExpandedGrid"] = detector.UsesExpandedGrid.ToString();
                    measurement.Facts["DenseGrid"] = detector.UsesDenseGrid.ToString();
                    measurement.Facts["GridCells"] = detector.GridCellCount.ToString(CultureInfo.InvariantCulture);
                    measurement.Facts["References"] = detector.StoredGridEdgeReferenceCount.ToString(CultureInfo.InvariantCulture);
                    measurement.Facts["LongEdges"] = detector.OutlierEdgeCount.ToString(CultureInfo.InvariantCulture);
                    add(measurement);
                }
            }
        }
    }

    /// <summary>
    /// Fixtures that isolate one cost each: candidates rejected by their bounding box,
    /// candidates that need orientation tests, and the three orientation routes.
    /// </summary>
    private static void Breakdown(Action<Measurement> add) {
        RectangleLocalBounds index = Footprints.Reference;
        RectangleLocalBounds small = Footprints.Small;
        RectanglePose[] Repeated(RectanglePose pose) {
            RectanglePose[] poses = new RectanglePose[64];
            Array.Fill(poses, pose);
            return poses;
        }

        foreach (int count in new[] { 1, 2, 4, 8, 16, 32, 64 }) {
            // Tiny triangles 2.4 units from the pose: inside the reach of the indexed
            // footprint, outside the bounding box of the small footprint at any yaw.
            List<(float X, float Y)[]> far = [];
            for (int triangle = 0; triangle < count; ++triangle) {
                double angle = Math.Tau * triangle / count;
                float x = (float)(2.4 * Math.Cos(angle)), y = (float)(2.4 * Math.Sin(angle));
                far.Add([(x, y), (x + 0.01f, y), (x, y + 0.01f)]);
            }

            TrackCase farTrack = new(
                SyntheticTrack.FromOutlines($"Far{count}", [.. far]), index, 1.0, "bounding-box rejections");
            Measurement rejected = Timed(
                $"Breakdown/BoxRejected/{count * 3}Edges", farTrack,
                new IndexedQueries(farTrack.Detector, small, Repeated(new RectanglePose(0f, 0f, 0.6f))));
            rejected.Facts["Candidates"] = farTrack.Grid!.GetRange(0.0, 0.0).Count.ToString(CultureInfo.InvariantCulture);
            add(rejected);

            // Tiny triangles inside the rectangle: every side's box overlaps, no side intersects.
            List<(float X, float Y)[]> inside = [];
            for (int triangle = 0; triangle < count; ++triangle) {
                double angle = Math.Tau * triangle / count;
                float x = (float)(0.3 * Math.Cos(angle)), y = (float)(0.3 * Math.Sin(angle));
                inside.Add([(x, y), (x + 0.01f, y), (x, y + 0.01f)]);
            }

            TrackCase insideTrack = new(
                SyntheticTrack.FromOutlines($"Inside{count}", [.. inside]), index, 1.0, "orientation tests");
            Measurement tested = Timed(
                $"Breakdown/OrientationTested/{count * 3}Edges", insideTrack,
                new IndexedQueries(insideTrack.Detector, index, Repeated(new RectanglePose(0f, 0f, MathF.PI / 4f))));
            tested.Facts["Candidates"] = insideTrack.Grid!.GetRange(0.0, 0.0).Count.ToString(CultureInfo.InvariantCulture);
            add(tested);
        }

        // Orientation routes, through the compiled accessor. The accessor's own cost is
        // measured by the first case, where the filter decides immediately.
        Random random = new(8610);
        float[] certain = new float[6 * 1024], collinear = new float[6 * 1024];
        float[] wide128 = new float[6 * 1024], wideLarge = new float[6 * 1024], subnormal = new float[6 * 1024];
        for (int index6 = 0; index6 < 1024; ++index6) {
            int start = index6 * 6;
            for (int offset = 0; offset < 6; ++offset) {
                certain[start + offset] = (float)((random.NextDouble() * 2.0 - 1.0) * 300.0);
                subnormal[start + offset] = random.Next(-4000, 4001) * float.Epsilon;
            }

            float x = random.Next(-400, 401) * 0.25f, y = random.Next(-400, 401) * 0.25f;
            float dx = random.Next(1, 41) * 0.25f, dy = random.Next(1, 41) * 0.25f;
            collinear[start] = x;
            collinear[start + 1] = y;
            collinear[start + 2] = x + 3f * dx;
            collinear[start + 3] = y + 3f * dy;
            collinear[start + 4] = x + dx;
            collinear[start + 5] = y + dy;
            Fill(wide128, start, random, 30);
            Fill(wideLarge, start, random, 60);
        }

        static void Fill(float[] values, int start, Random random, int span) {
            int u = random.Next(1 << 22, 1 << 24) | 1, v = random.Next(1 << 22, 1 << 24) | 1;
            values[start] = MathF.ScaleB(u, -40);
            values[start + 1] = MathF.BitIncrement(MathF.ScaleB(v, -40));
            values[start + 2] = MathF.ScaleB(u, -40 + span);
            values[start + 3] = MathF.ScaleB(v, -40 + span);
            values[start + 4] = MathF.ScaleB(u, -41 + span);
            values[start + 5] = MathF.ScaleB(v, -41 + span);
        }

        add(Bench.Run("Breakdown/Orientation/FilterDecides", new OrientationCalls(certain, DetectorInternals.OrientationSign)));
        add(Bench.Run("Breakdown/Orientation/ExactlyCollinear", new OrientationCalls(collinear, DetectorInternals.OrientationSign)));
        add(Bench.Run("Breakdown/Orientation/Span30Through128Bit", new OrientationCalls(wide128, DetectorInternals.OrientationSign)));
        add(Bench.Run("Breakdown/Orientation/Span60ThroughLargeIntegers", new OrientationCalls(wideLarge, DetectorInternals.OrientationSign)));
        add(Bench.Run("Breakdown/Orientation/SubnormalInputs", new OrientationCalls(subnormal, DetectorInternals.OrientationSign)));
        add(Bench.Run("Breakdown/ExactRoute/OrdinaryInputs", new OrientationCalls(certain, DetectorInternals.ExactOrientationSign)));
    }

    private static void Construction(List<ConstructionMeasurement> results) {
        foreach (string name in TrackCatalog.Names) {
            TrackCase track = TrackCatalog.Build(name);
            ColliderJson collider = track.Track.Collider;
            RectangleLocalBounds footprint = track.IndexFootprint;

            // Repeat until consecutive constructions agree, then sample.
            double first = double.NaN;
            Queue<double> recent = new();
            long warmupStart = Stopwatch.GetTimestamp();
            while (Stopwatch.GetElapsedTime(warmupStart).TotalMilliseconds < 8000.0) {
                long start = Stopwatch.GetTimestamp();
                TrackCollisionDetector detector = new(collider, footprint);
                double milliseconds = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                GC.KeepAlive(detector);
                if (double.IsNaN(first)) {
                    first = milliseconds;
                }

                recent.Enqueue(milliseconds);
                if (recent.Count > 12) {
                    recent.Dequeue();
                }

                if (recent.Count == 12 && Stopwatch.GetElapsedTime(warmupStart).TotalMilliseconds > 500.0) {
                    double low = double.PositiveInfinity, high = 0.0;
                    foreach (double value in recent) {
                        low = Math.Min(low, value);
                        high = Math.Max(high, value);
                    }

                    if (high <= low * 1.25) {
                        break;
                    }
                }
            }

            double[] samples = new double[Bench.SampleCount];
            long allocated = 0;
            for (int sample = 0; sample < samples.Length; ++sample) {
                long before = GC.GetAllocatedBytesForCurrentThread();
                long start = Stopwatch.GetTimestamp();
                TrackCollisionDetector detector = new(collider, footprint);
                samples[sample] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                GC.KeepAlive(detector);
            }

            // Retained size: hold several detectors and compare collected heap sizes.
            const int held = 8;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long baseline = GC.GetTotalMemory(forceFullCollection: true);
            TrackCollisionDetector[] detectors = new TrackCollisionDetector[held];
            for (int index = 0; index < held; ++index) {
                detectors[index] = new TrackCollisionDetector(collider, footprint);
            }

            long retained = (GC.GetTotalMemory(forceFullCollection: true) - baseline) / held;
            GC.KeepAlive(detectors);

            ConstructionMeasurement measurement = new() {
                Name = name,
                IndexKind = track.IndexKind,
                Edges = track.Detector.EdgeCount,
                GridCells = track.Detector.GridCellCount,
                References = track.Detector.StoredGridEdgeReferenceCount,
                FirstMilliseconds = first,
                Milliseconds = samples,
                AllocatedBytes = allocated,
                RetainedBytes = retained,
            };
            results.Add(measurement);
            Console.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"Construction {name}: {track.IndexKind}; edges={measurement.Edges}; first={first:F3} ms; "
                + $"median={measurement.MedianMilliseconds:F3} ms; allocated={allocated}; retained={retained}"));
        }
    }

    /// <summary>
    /// One call per sample, timed individually. Between samples a buffer larger than a
    /// cache level is rewritten, so the call finds the index and its own code displaced,
    /// as it does when a frame's other work runs between two collision queries.
    /// </summary>
    private static void SingleCalls(List<SingleCallMeasurement> results) {
        foreach (string trackName in new[] { TrackCatalog.Circuit, TrackCatalog.CircuitWide }) {
            TrackCase track = TrackCatalog.Build(trackName);
            RectangleLocalBounds footprint = track.IndexFootprint;
            (string Name, RectanglePose[] Poses)[] workloads = [
                ("LapCenter", Workloads.Lap(track, 0.0, 0.25)),
                ("NearMiss", Workloads.NearMisses(track, footprint, 8603)),
                ("Contact", Workloads.Contacts(track, footprint, 8604)),
            ];
            foreach ((string workloadName, RectanglePose[] poses) in workloads) {
                // Bring the query to its final code before timing single calls.
                _ = Bench.Run("warmup", new IndexedQueries(track.Detector, footprint, poses));
                foreach (int megabytes in new[] { 0, 1, 4, 16, 64 }) {
                    SingleCallMeasurement measurement = TimeSingleCalls(
                        $"Single/{trackName}/{workloadName}/Evict{megabytes}MB",
                        track.Detector, footprint, poses, megabytes * 1024 * 1024, megabytes >= 16 ? 800 : 4000);
                    results.Add(measurement);
                    Console.WriteLine(string.Create(
                        CultureInfo.InvariantCulture,
                        $"{measurement.Name}: mean={measurement.MeanNanoseconds:F1} ns; "
                        + $"timer={measurement.TimerMeanNanoseconds:F1} ns; net={measurement.NetMeanNanoseconds:F1} ns; "
                        + $"median={measurement.MedianNanoseconds:F0}; p90={measurement.Percentile90Nanoseconds:F0}; "
                        + $"p99={measurement.Percentile99Nanoseconds:F0}; max={measurement.MaximumNanoseconds:F0}"));
                }
            }
        }
    }

    private static SingleCallMeasurement TimeSingleCalls(
        string name, TrackCollisionDetector detector, RectangleLocalBounds footprint, RectanglePose[] poses,
        int evictedBytes, int samples) {
        byte[] buffer = new byte[Math.Max(evictedBytes, 1)];
        double[] call = new double[samples];
        double[] timer = new double[samples];
        double tick = 1e9 / Stopwatch.Frequency;
        long contacts = 0;
        for (int sample = -64; sample < samples; ++sample) {
            RectanglePose pose = poses[(sample + 64) * 7 % poses.Length];
            Evict(buffer, evictedBytes);
            long start = Stopwatch.GetTimestamp();
            bool colliding = detector.IsColliding(footprint, pose);
            long end = Stopwatch.GetTimestamp();
            Evict(buffer, evictedBytes);
            long emptyStart = Stopwatch.GetTimestamp();
            long emptyEnd = Stopwatch.GetTimestamp();
            if (sample >= 0) {
                call[sample] = (end - start) * tick;
                timer[sample] = (emptyEnd - emptyStart) * tick;
                if (colliding) {
                    ++contacts;
                }
            }
        }

        return new SingleCallMeasurement {
            Name = name,
            EvictedBytes = evictedBytes,
            Samples = samples,
            MeanNanoseconds = Statistics.Mean(call),
            TimerMeanNanoseconds = Statistics.Mean(timer),
            MedianNanoseconds = Statistics.Percentile(call, 0.5),
            Percentile90Nanoseconds = Statistics.Percentile(call, 0.9),
            Percentile99Nanoseconds = Statistics.Percentile(call, 0.99),
            MaximumNanoseconds = Statistics.Percentile(call, 1.0),
            Contacts = contacts,
        };
    }

    private static void Evict(byte[] buffer, int bytes) {
        for (int index = 0; index < bytes; index += 64) {
            ++buffer[index];
        }
    }

    /// <summary>
    /// First use in a new process: the first construction and the first queries include
    /// the compilation of the detector's code, as they do when the game loads a track.
    /// </summary>
    public static int ColdStart(Options options) {
        string trackName = options.Text("track", TrackCatalog.Circuit);
        string topology = ProcessorTopology.Apply(
            options.Text("affinity", "performance"), options.Text("priority", "high"));
        // Defining a track generates its outlines without running any detector code.
        TrackDefinition definition = TrackCatalog.Define(trackName);
        RectangleLocalBounds footprint = definition.IndexFootprint;
        RectanglePose[] poses = definition.Track.HasCenterline
            ? definition.Track.LapPoses(4096, 0.25 * definition.FootprintScale, 0.0)
            : PoseSampler.Uniform(definition.Track.Oracle, 4096, 8601, 0.0);
        double tick = 1e6 / Stopwatch.Frequency;

        long start = Stopwatch.GetTimestamp();
        TrackCollisionDetector detector = new(definition.Track.Collider, footprint);
        long built = Stopwatch.GetTimestamp();
        bool first = detector.IsColliding(footprint, poses[0]);
        long queried = Stopwatch.GetTimestamp();
        double firstConstruction = (built - start) * tick;
        double firstQuery = (queried - built) * tick;

        // Later single calls, one per simulated frame, each timed alone. The pause
        // between calls lasts one timer interrupt, about 16 ms.
        int[] checkpoints = [1, 2, 5, 10, 30, 60, 120, 300];
        double[] laterQueries = new double[checkpoints.Length];
        int contacts = first ? 1 : 0;
        int call = 0;
        for (int index = 0; index < checkpoints.Length; ++index) {
            while (call < checkpoints[index] - 1) {
                if (detector.IsColliding(footprint, poses[++call % poses.Length])) {
                    ++contacts;
                }

                // Time passes between calls, as between frames, so that the runtime's
                // background recompilation proceeds as it would in the application.
                System.Threading.Thread.Sleep(1);
            }

            long before = Stopwatch.GetTimestamp();
            bool colliding = detector.IsColliding(footprint, poses[++call % poses.Length]);
            laterQueries[index] = (Stopwatch.GetTimestamp() - before) * tick;
            if (colliding) {
                ++contacts;
            }
        }

        long secondStart = Stopwatch.GetTimestamp();
        TrackCollisionDetector second = new(definition.Track.Collider, footprint);
        double secondConstruction = (Stopwatch.GetTimestamp() - secondStart) * tick;
        GC.KeepAlive(second);

        object result = new {
            Track = trackName,
            IndexKind = TrackCase.Describe(detector),
            Processor = topology,
            TieredCompilation = Setting("DOTNET_TieredCompilation"),
            TieredPGO = Setting("DOTNET_TieredPGO"),
            FirstConstructionMicroseconds = firstConstruction,
            SecondConstructionMicroseconds = secondConstruction,
            FirstQueryMicroseconds = firstQuery,
            Calls = checkpoints,
            QueryMicroseconds = laterQueries,
            Contacts = contacts,
        };
        string json = JsonSerializer.Serialize(result);
        Console.WriteLine("COLDSTART " + json);
        string output = options.Text("output", string.Empty);
        if (output.Length > 0) {
            File.AppendAllText(output, json + "\n");
        }

        return 0;
    }
}
