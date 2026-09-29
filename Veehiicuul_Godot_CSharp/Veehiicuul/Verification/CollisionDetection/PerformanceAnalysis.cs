using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Veehiicuul_Godot_CSharp;

namespace VeehiicuulCollisionVerification;

/// <summary>Reproducible kernel measurements, separate from Godot and rendering.</summary>
internal static class PerformanceAnalysis {
    private const int SampleCount = 15;
    private static readonly RectangleLocalBounds CarBounds = new(-1.5f, -2.992522f, 1.5f, 3f);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly List<object> Results = [];
    private static long Checksum;
    private static int OracleComparisons;

    internal static void Run(string[] arguments) {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Console.WriteLine($"Environment: {RuntimeInformation.FrameworkDescription}; {RuntimeInformation.OSDescription}; {RuntimeInformation.ProcessArchitecture}; logical processors={Environment.ProcessorCount}; stopwatch={Stopwatch.Frequency}; tiered={Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "default"}");
        foreach (string name in new[] { "Ribeye", "Track001" }) {
            ColliderJson data = JsonSerializer.Deserialize<ColliderJson>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, name + "_ColliderData.json")))!;
            AnalyzeTrack(name, data);
        }
        AnalyzeSynthetic();
        ValidateIndependentOracle();
        ValidateInvalidInputs();
        Console.WriteLine($"PASS: independent exact oracle comparisons={OracleComparisons}; checksum={Checksum}.");
        string output = arguments.FirstOrDefault(value => value.StartsWith("--output=", StringComparison.Ordinal))?[9..]
            ?? Path.Combine(AppContext.BaseDirectory, "PerformanceResults.json");
        File.WriteAllText(output, JsonSerializer.Serialize(new {
            Framework = RuntimeInformation.FrameworkDescription,
            OperatingSystem = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            LogicalProcessors = Environment.ProcessorCount,
            TieredCompilation = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "default",
            SampleCount, OracleComparisons, Checksum, Results
        }, JsonOptions));
    }

    private static void AnalyzeTrack(string name, ColliderJson data) {
        TrackCollisionDetector detector = new(data, CarBounds);
        DescribeIndex(name, detector);
        MeasureConstruction(name, data, CarBounds);
        CoordinateXY[] points = data.Outlines.SelectMany(outline => outline.Vertices).ToArray();
        float minX = points.Min(point => point.X), maxX = points.Max(point => point.X);
        float minY = points.Min(point => point.Y), maxY = points.Max(point => point.Y);
        Random random = new(20260929);
        Query[] mixed = Enumerable.Range(0, 4096).Select(_ => new Query(CarBounds, new RectanglePose(
            Next(random, minX, maxX), Next(random, minY, maxY), Next(random, -MathF.PI, MathF.PI)))).ToArray();
        List<Query> neighborhoods = [];
        foreach (Outline outline in data.Outlines) {
            for (int index = 0; index < outline.Vertices.Count; ++index) {
                CoordinateXY a = outline.Vertices[index], b = outline.Vertices[(index + 1) % outline.Vertices.Count];
                float angle = MathF.Atan2(b.X - a.X, b.Y - a.Y);
                foreach (float offset in new[] { -3.01f, -1.49f, 0f, 1.49f, 3.01f }) {
                    neighborhoods.Add(new Query(CarBounds, new RectanglePose(
                        (a.X + b.X) * 0.5f + offset * MathF.Cos(angle),
                        (a.Y + b.Y) * 0.5f - offset * MathF.Sin(angle), angle)));
                }
            }
        }
        foreach (Query query in mixed.Concat(neighborhoods)) {
            Require(detector.IsColliding(query.Bounds, query.Pose) == detector.IsCollidingLinear(query.Bounds, query.Pose), name + " broad-phase mismatch");
        }
        foreach (Query query in mixed.Take(256).Concat(neighborhoods.Where((_, index) => index % 13 == 0))) {
            CompareOracle(data, detector, query);
        }
        Console.WriteLine($"Validation {name}: {mixed.Length + neighborhoods.Count} indexed/linear comparisons, every edge plus closing edges.");
        Benchmark(name + "/Mixed", detector, mixed);
        Benchmark(name + "/ClearInsideBounds", detector, mixed.Where(query => !detector.IsColliding(query.Bounds, query.Pose)).ToArray());
        Benchmark(name + "/Contact", detector, neighborhoods.Where(query => detector.IsColliding(query.Bounds, query.Pose)).ToArray());
        Benchmark(name + "/ExactVertexContact", detector, points.Select(point => new Query(new(0, 0, 3, 6), new(point.X, point.Y, 0))).ToArray());
        Benchmark(name + "/OutsideBounds", detector, [new(CarBounds, new(maxX + 100, maxY + 100, 0.7f))]);
        Benchmark(name + "/HugeContainingRectangle", detector, [new(new(minX - 100, minY - 100, maxX + 100, maxY + 100), new(0, 0, 0))]);
        foreach (float scale in new[] { 0.25f, 2f, 4f }) {
            RectangleLocalBounds scaled = new(CarBounds.MinX * scale, CarBounds.MinY * scale, CarBounds.MaxX * scale, CarBounds.MaxY * scale);
            TrackCollisionDetector alternative = new(data, scaled);
            DescribeIndex(name + $"/CellScale{scale}", alternative);
            Benchmark(name + $"/CellScale{scale}", alternative, mixed);
        }
        if (name == "Ribeye") {
            Benchmark(name + "/Spawn", detector, [new(CarBounds, new(117.841125f, 61.20298f, 1.793361f))]);
        }
    }

    private static void AnalyzeSynthetic() {
        RectangleLocalBounds small = new(-0.5f, -0.5f, 0.5f, 0.5f);
        foreach (int copies in new[] { 1, 16, 256 }) {
            ColliderJson original = JsonSerializer.Deserialize<ColliderJson>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ribeye_ColliderData.json")))!;
            ColliderJson tiled = new();
            for (int copy = 0; copy < copies; ++copy) {
                foreach (Outline outline in original.Outlines) {
                    tiled.Outlines.Add(new Outline { Vertices = outline.Vertices.Select(point => new CoordinateXY(point.X + (copy % 16) * 1000, point.Y + (copy / 16) * 1000)).ToList() });
                }
            }
            string name = "TiledRibeye/" + copies;
            TrackCollisionDetector detector = new(tiled, CarBounds);
            DescribeIndex(name, detector);
            MeasureConstruction(name, tiled, CarBounds);
            Query[] queries = Enumerable.Range(0, 512).Select(index => {
                int copy = index % copies;
                return new Query(CarBounds, new(117.841125f + (copy % 16) * 1000, 61.20298f + (copy / 16) * 1000, 1.793361f));
            }).ToArray();
            Benchmark(name + "/Clear", detector, queries, copies < 256);
        }
        ColliderJson sparse = new() { Outlines = [Square(-10000, 0, 0.25f), Square(0, 0, 0.25f), Square(10000, 0, 0.25f)] };
        TrackCollisionDetector sparseDetector = new(sparse, small);
        DescribeIndex("Sparse", sparseDetector);
        Benchmark("Sparse/Contact", sparseDetector, [new(new(-0.5f, -0.1f, 0.5f, 0.1f), new(0, 0, 0.1f))]);
        Benchmark("Sparse/EmptyCell", sparseDetector, [new(small, new(5000, 0, 0))]);
        foreach (int count in new[] { 1, 256, 4096 }) {
            ColliderJson longEdges = new() { Outlines = Enumerable.Range(0, count).Select(index => Square(index * 20, 0, 5)).ToList() };
            TrackCollisionDetector detector = new(longEdges, small);
            DescribeIndex("LongEdges/" + count, detector);
            MeasureConstruction("LongEdges/" + count, longEdges, small);
            Benchmark("LongEdges/" + count + "/Contact", detector, [new(small, new((count - 1) * 20 + 5, 0, 0.3f))]);
            Benchmark("LongEdges/" + count + "/Clear", detector, [new(small, new((count - 1) * 20, 0, 0.3f))]);
        }
        ColliderJson square = new() { Outlines = [Square(5, 5, 5)] };
        TrackCollisionDetector exact = new(square, small);
        Benchmark("Exact/Collinear", exact, [new(new(0, -1, 2, 1), new(10, 5, 0))]);
        Benchmark("Exact/Endpoint", exact, [new(new(0, 0, 2, 2), new(10, 10, 0))]);
        Benchmark("Exact/OneUlpGap", exact, [new(new(0, 0, 2, 2), new(float.BitIncrement(10), 10, 0))]);
        ColliderJson diagonal = new() { Outlines = [new() { Vertices = [new(10, 10), new(20, 20), new(20, 10)] }] };
        TrackCollisionDetector diagonalDetector = new(diagonal, small);
        Query diagonalQuery = new(new(0, 0, 2, 2), new(15, 15, 0));
        CompareOracle(diagonal, diagonalDetector, diagonalQuery);
        Benchmark("Exact/DiagonalContact", diagonalDetector, [diagonalQuery]);
        float tiny = float.Epsilon * 1024;
        ColliderJson subnormal = new() { Outlines = [Square(0, 0, tiny)] };
        TrackCollisionDetector subnormalDetector = new(subnormal, new(-tiny / 4, -tiny / 4, tiny / 4, tiny / 4));
        Query subnormalQuery = new(new(-tiny / 4, -tiny / 4, tiny / 4, tiny / 4), new(tiny, 0, 0));
        CompareOracle(subnormal, subnormalDetector, subnormalQuery);
        Benchmark("Exact/Subnormal", subnormalDetector, [subnormalQuery]);
        ColliderJson span = new() { Outlines = [
            new() { Vertices = [new(-1e10f, 0), new(-1e10f, 0.25f), new(-1e10f + 2048, 0.25f)] },
            new() { Vertices = [new(1e10f, 0), new(1e10f, 0.25f), new(1e10f + 2048, 0.25f)] }, Square(0, 0, 0.25f)] };
        TrackCollisionDetector fallback = new(span, small);
        Require(fallback.OrdinaryEdgeCount == 0 && fallback.OutlierEdgeCount == 10, "Integer-span fixture did not force grid fallback");
        DescribeIndex("GridIntegerFallback", fallback);
        Query fallbackQuery = new(new(-1, -0.1f, 1, 0.1f), new(0, 0, 0));
        CompareOracle(span, fallback, fallbackQuery);
        Benchmark("GridIntegerFallback/Contact", fallback, [fallbackQuery]);
    }

    private static void Benchmark(string name, TrackCollisionDetector detector, Query[] queries, bool compareLinear = true) {
        Require(queries.Length > 0, "Empty benchmark: " + name);
        long expected = 0;
        foreach (Query query in queries) {
            bool actual = detector.IsColliding(query.Bounds, query.Pose);
            Require(actual == detector.IsCollidingLinear(query.Bounds, query.Pose), "Benchmark mismatch: " + name);
            if (actual) { ++expected; }
        }
        // Warm both methods for at least 150 ms, including tiered-JIT promotion time.
        long warmStart = Stopwatch.GetTimestamp();
        do {
            Checksum += Batch(detector, queries, 1, false);
            if (compareLinear) { Checksum += Batch(detector, queries, 1, true); }
        } while (Stopwatch.GetElapsedTime(warmStart).TotalMilliseconds < 150);
        int indexedRepetitions = Calibrate(detector, queries, false);
        int linearRepetitions = compareLinear ? Calibrate(detector, queries, true) : 1;
        double[] indexed = new double[SampleCount], linear = new double[SampleCount];
        for (int sample = 0; sample < SampleCount; ++sample) {
            // Alternate measurement order; no collection or setup inside a timed batch.
            if (sample % 2 == 0) {
                indexed[sample] = TimeBatch(detector, queries, indexedRepetitions, false, expected);
                if (compareLinear) { linear[sample] = TimeBatch(detector, queries, linearRepetitions, true, expected); }
            } else {
                if (compareLinear) { linear[sample] = TimeBatch(detector, queries, linearRepetitions, true, expected); }
                indexed[sample] = TimeBatch(detector, queries, indexedRepetitions, false, expected);
            }
        }
        long before = GC.GetAllocatedBytesForCurrentThread();
        int allocationRepetitions = Math.Max(1, 32768 / queries.Length);
        Checksum += Batch(detector, queries, allocationRepetitions, false);
        double bytes = (GC.GetAllocatedBytesForCurrentThread() - before) / (double)(allocationRepetitions * queries.Length);
        double median = Percentile(indexed, 0.5), tail = Percentile(indexed, 0.95);
        double linearMedian = compareLinear ? Percentile(linear, 0.5) : 0;
        Results.Add(new { Kind = "Query", Name = name, Queries = queries.Length, Contacts = expected,
            IndexedRepetitions = indexedRepetitions, LinearRepetitions = linearRepetitions,
            IndexedNanoseconds = indexed, LinearNanoseconds = compareLinear ? linear : null,
            MedianNanoseconds = median, BatchP95Nanoseconds = tail, BytesPerQuery = bytes,
            LinearMedianNanoseconds = linearMedian });
        Console.WriteLine($"Query {name}: contact={expected}/{queries.Length}; median={median:F1} ns; batch-p95={tail:F1} ns; linear={linearMedian:F1} ns; bytes={bytes:F1}");
    }

    private static int Calibrate(TrackCollisionDetector detector, Query[] queries, bool linear) {
        int repetitions = 1;
        while (true) {
            long start = Stopwatch.GetTimestamp();
            Checksum += Batch(detector, queries, repetitions, linear);
            if (Stopwatch.GetElapsedTime(start).TotalMilliseconds >= 15 || repetitions >= 1048576) { return repetitions; }
            repetitions *= 2;
        }
    }

    private static double TimeBatch(TrackCollisionDetector detector, Query[] queries, int repetitions, bool linear, long expected) {
        long start = Stopwatch.GetTimestamp();
        long actual = Batch(detector, queries, repetitions, linear);
        long elapsed = Stopwatch.GetTimestamp() - start;
        Require(actual == expected * repetitions, "Timed checksum mismatch");
        Checksum += actual;
        return elapsed * (1e9 / Stopwatch.Frequency) / ((long)repetitions * queries.Length);
    }

    private static long Batch(TrackCollisionDetector detector, Query[] queries, int repetitions, bool linear) {
        long contacts = 0;
        if (linear) {
            for (int repetition = 0; repetition < repetitions; ++repetition) {
                foreach (Query query in queries) { if (detector.IsCollidingLinear(query.Bounds, query.Pose)) { ++contacts; } }
            }
        } else {
            for (int repetition = 0; repetition < repetitions; ++repetition) {
                foreach (Query query in queries) { if (detector.IsColliding(query.Bounds, query.Pose)) { ++contacts; } }
            }
        }
        return contacts;
    }

    private static void MeasureConstruction(string name, ColliderJson data, RectangleLocalBounds bounds) {
        for (int index = 0; index < 5; ++index) { GC.KeepAlive(new TrackCollisionDetector(data, bounds)); }
        double[] durations = new double[SampleCount];
        long[] allocations = new long[SampleCount];
        for (int index = 0; index < SampleCount; ++index) {
            long before = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            TrackCollisionDetector detector = new(data, bounds);
            durations[index] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            allocations[index] = GC.GetAllocatedBytesForCurrentThread() - before;
            GC.KeepAlive(detector);
        }
        Results.Add(new { Kind = "Construction", Name = name, Milliseconds = durations, AllocatedBytes = allocations });
        Console.WriteLine($"Build {name}: median={Percentile(durations, 0.5):F3} ms; bytes={allocations[0]}");
    }

    private static void DescribeIndex(string name, TrackCollisionDetector detector) {
        Results.Add(new { Kind = "Index", Name = name, detector.EdgeCount, detector.CellSize, detector.UsesDenseGrid,
            detector.GridColumnCount, detector.GridRowCount, detector.OccupiedCellCount, detector.GridCellCount,
            detector.OrdinaryEdgeCount, detector.OutlierEdgeCount, detector.OutlierBvhNodeCount });
        Console.WriteLine($"Index {name}: edges={detector.EdgeCount}; cell={detector.CellSize}; dense={detector.UsesDenseGrid}; grid={detector.GridColumnCount}x{detector.GridRowCount}; occupied={detector.OccupiedCellCount}; outliers={detector.OutlierEdgeCount}; nodes={detector.OutlierBvhNodeCount}");
    }

    private static void ValidateIndependentOracle() {
        Random random = new(97013);
        foreach (int exponent in new[] { -140, -100, -30, 0, 30, 100, 120 }) {
            float scale = MathF.ScaleB(1, exponent);
            for (int iteration = 0; iteration < 1000; ++iteration) {
                ColliderJson data = new() { Outlines = [new Outline { Vertices = [new(-4 * scale, -2 * scale), new(3 * scale, -scale), new(scale, 4 * scale)] }] };
                RectangleLocalBounds bounds = new(-scale, -scale, scale, scale);
                TrackCollisionDetector detector = new(data, bounds);
                CompareOracle(data, detector, new(bounds, new(Next(random, -5, 5) * scale, Next(random, -5, 5) * scale, Next(random, -MathF.PI, MathF.PI))));
            }
        }
        // Axis contacts, both signed zeros, one-ULP gaps, and collapsed float corners.
        foreach (float scale in new[] { float.Epsilon * 1024, 1f, 1e10f, 1e30f }) {
            ColliderJson data = new() { Outlines = [Square(0, 0, scale)] };
            RectangleLocalBounds bounds = new(-scale / 4, -scale / 4, scale / 4, scale / 4);
            TrackCollisionDetector detector = new(data, bounds);
            foreach (float x in new[] { scale, scale * 1.25f, float.BitIncrement(scale * 1.25f), float.BitDecrement(scale * 1.25f), 0f, -0f }) {
                CompareOracle(data, detector, new(bounds, new(x, 0, 0)));
            }
            CompareOracle(data, detector, new(new(-float.Epsilon, -float.Epsilon, float.Epsilon, float.Epsilon), new(scale, scale, 0)));
        }
    }

    private static void ValidateInvalidInputs() {
        RectangleLocalBounds bounds = new(-1, -1, 1, 1);
        List<ColliderJson> invalid = [new(), new() { Outlines = null! }, new() { Outlines = [null!] },
            new() { Outlines = [new() { Vertices = null! }] }, new() { Outlines = [new() { Vertices = [new(0, 0), new(1, 0)] }] },
            new() { Outlines = [new() { Vertices = [new(0, 0), new(1, 0), new(1, 0)] }] },
            new() { Outlines = [new() { Vertices = [new(0, 0), new(1, 0), new() { X = float.NaN, Y = 1 }] }] }];
        foreach (ColliderJson data in invalid) { ExpectArgument(() => _ = new TrackCollisionDetector(data, bounds)); }
        ColliderJson square = new() { Outlines = [Square(0, 0, 5)] };
        ExpectArgument(() => _ = new TrackCollisionDetector(square, default));
        TrackCollisionDetector detector = new(square, bounds);
        ExpectArgument(() => detector.IsColliding(default, default));
        ExpectArgument(() => _ = new RectanglePose(float.PositiveInfinity, 0, 0));
        ExpectArgument(() => detector.IsColliding(new(-float.MaxValue, -1, float.MaxValue, 1), new(float.MaxValue, 0, 0)));
        Console.WriteLine("PASS: 11 invalid-input checks.");
    }

    private static void ExpectArgument(Action action) {
        try { action(); } catch (ArgumentException) { return; }
        throw new InvalidOperationException("Invalid input was accepted");
    }

    private static void CompareOracle(ColliderJson data, TrackCollisionDetector detector, Query query) {
        bool expected = Oracle(data, query);
        bool actual = detector.IsColliding(query.Bounds, query.Pose);
        Require(expected == actual, $"Independent oracle mismatch at {query.Pose.PositionX:R}, {query.Pose.PositionY:R}, {query.Pose.RotationRadians:R}");
        ++OracleComparisons;
    }

    // Fixed common exponent -149 converts every finite binary32 value to an exact
    // integer. No production broad phase or production predicate is reused.
    private static BigInteger Integer(float value) {
        int bits = BitConverter.SingleToInt32Bits(value), exponent = (bits >> 23) & 255;
        int fraction = bits & 0x7fffff;
        BigInteger integer = exponent == 0 ? fraction : new BigInteger(0x800000 | fraction) << (exponent - 1);
        return bits < 0 ? -integer : integer;
    }

    private static bool Oracle(ColliderJson data, Query query) {
        double cosine = Math.Cos(query.Pose.RotationRadians), sine = Math.Sin(query.Pose.RotationRadians);
        RectangleLocalBounds bounds = query.Bounds;
        Point[] rectangle = new[] { (bounds.MinX, bounds.MinY), (bounds.MaxX, bounds.MinY), (bounds.MaxX, bounds.MaxY), (bounds.MinX, bounds.MaxY) }
            .Select(local => new Point((float)((double)query.Pose.PositionX + local.Item1 * cosine + local.Item2 * sine),
                (float)((double)query.Pose.PositionY - local.Item1 * sine + local.Item2 * cosine))).ToArray();
        foreach (Outline outline in data.Outlines) {
            for (int index = 0; index < outline.Vertices.Count; ++index) {
                CoordinateXY a = outline.Vertices[index], b = outline.Vertices[(index + 1) % outline.Vertices.Count];
                for (int side = 0; side < 4; ++side) {
                    if (Intersect(new(a.X, a.Y), new(b.X, b.Y), rectangle[side], rectangle[(side + 1) % 4])) { return true; }
                }
            }
        }
        return false;
    }

    private static bool Intersect(Point a, Point b, Point c, Point d) {
        if (Math.Min(a.X, b.X) > Math.Max(c.X, d.X) || Math.Max(a.X, b.X) < Math.Min(c.X, d.X)
            || Math.Min(a.Y, b.Y) > Math.Max(c.Y, d.Y) || Math.Max(a.Y, b.Y) < Math.Min(c.Y, d.Y)) { return false; }
        int first = Orientation(a, b, c), second = Orientation(a, b, d), third = Orientation(c, d, a), fourth = Orientation(c, d, b);
        if ((first == 0 && On(a, b, c)) || (second == 0 && On(a, b, d)) || (third == 0 && On(c, d, a)) || (fourth == 0 && On(c, d, b))) { return true; }
        return first * second < 0 && third * fourth < 0;
    }

    private static int Orientation(Point a, Point b, Point c) =>
        ((Integer(b.X) - Integer(a.X)) * (Integer(c.Y) - Integer(a.Y)) - (Integer(b.Y) - Integer(a.Y)) * (Integer(c.X) - Integer(a.X))).Sign;
    private static bool On(Point a, Point b, Point c) => c.X >= Math.Min(a.X, b.X) && c.X <= Math.Max(a.X, b.X) && c.Y >= Math.Min(a.Y, b.Y) && c.Y <= Math.Max(a.Y, b.Y);
    private static Outline Square(float x, float y, float half) => new() { Vertices = [new(x - half, y - half), new(x + half, y - half), new(x + half, y + half), new(x - half, y + half)] };
    private static float Next(Random random, float low, float high) => low + (float)random.NextDouble() * (high - low);
    private static double Percentile(double[] values, double fraction) => values.Order().ElementAt((int)Math.Ceiling((values.Length - 1) * fraction));
    private static void Require(bool condition, string message) { if (!condition) { throw new InvalidOperationException(message); } }
    private readonly record struct Query(RectangleLocalBounds Bounds, RectanglePose Pose);
    private readonly record struct Point(float X, float Y);
}
