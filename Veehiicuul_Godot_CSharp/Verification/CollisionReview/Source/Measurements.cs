using System;
using System.Collections.Generic;
using System.Diagnostics;
using Veehiicuul_Godot_CSharp;

namespace CollisionReview;

internal sealed class Measurements {
    private readonly List<object> _rows = [];
    private readonly List<object> _construction = [];
    private readonly Random _random = new(20261003);
    private static int _observableChecksum;

    internal object Run(ColliderJson data, Vehicle[] vehicles) {
        RectangleLocalBounds representative = Validation.Union(vehicles);
        TrackCollisionDetector detector = new(data, representative);
        this.Construction("Ribeye shared", data, representative, 0.5);
        foreach (Vehicle vehicle in vehicles) {
            foreach ((string name, RectanglePose[] poses) in this.Workloads(data, detector, vehicle.Bounds)) {
                this.Compare("Ribeye " + vehicle.Name + " " + name, detector, vehicle.Bounds, poses);
            }
        }
        RectanglePose[] sweepPoses = this.Workloads(data, detector, vehicles[3].Bounds)[0].Poses;
        foreach (double scale in new[] { 0.25, 0.5, 1.0, 2.0, 4.0 }) {
            TrackCollisionDetector candidate = new(data, representative, scale);
            this.Construction("Ribeye cell scale " + scale, data, representative, scale);
            this.Compare("Ribeye scale " + scale, candidate, vehicles[3].Bounds, sweepPoses);
        }
        foreach (int edgeCount in new[] { 800, 8000, 80000 }) {
            CoordinateXY[] vertices = new CoordinateXY[edgeCount];
            for (int index = 0; index < vertices.Length; ++index) {
                double angle = index * Math.Tau / vertices.Length;
                vertices[index] = new CoordinateXY { X = (float)(30 * Math.Cos(angle)), Y = (float)(30 * Math.Sin(angle)) };
            }
            ColliderJson synthetic = new() { Outlines = [new Outline { Vertices = vertices }] };
            TrackCollisionDetector candidate = new(synthetic, representative);
            this.Construction("Circular outline " + edgeCount, synthetic, representative, 0.5);
            RectanglePose[] near = this.Workloads(synthetic, candidate, vehicles[3].Bounds)[1].Poses;
            this.Compare("Circular outline " + edgeCount, candidate, vehicles[3].Bounds, near);
        }
        return new {
            WarmupPasses = 32, Trials = 9, MinimumTrialMilliseconds = 25,
            TieredCompilation = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"),
            Measurements = this._rows, Construction = this._construction, ObservableChecksum = _observableChecksum
        };
    }

    private List<(string Name, RectanglePose[] Poses)> Workloads(ColliderJson data, TrackCollisionDetector detector, RectangleLocalBounds bounds) {
        List<CoordinateXY> vertices = [];
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity, maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        foreach (Outline outline in data.Outlines) {
            foreach (CoordinateXY point in outline.Vertices) {
                vertices.Add(point);
                minX = Math.Min(minX, point.X); minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X); maxY = Math.Max(maxY, point.Y);
            }
        }
        RectanglePose[] uniform = new RectanglePose[1024], far = new RectanglePose[1024];
        List<RectanglePose> hits = [], nearMisses = [];
        float radius = Math.Max(bounds.MaxX - bounds.MinX, bounds.MaxY - bounds.MinY);
        for (int index = 0; index < uniform.Length; ++index) {
            uniform[index] = new RectanglePose((float)(minX + this._random.NextDouble() * (maxX - minX)),
                (float)(minY + this._random.NextDouble() * (maxY - minY)), (float)(this._random.NextDouble() * Math.Tau));
            far[index] = new RectanglePose(maxX + 10 + index * 0.1f, maxY + 10, index * 0.01f);
        }
        for (int attempt = 0; (hits.Count < 1024 || nearMisses.Count < 1024) && attempt < 100000; ++attempt) {
            CoordinateXY vertex = vertices[this._random.Next(vertices.Count)];
            RectanglePose pose = new(vertex.X + (float)((this._random.NextDouble() - 0.5) * radius * 4),
                vertex.Y + (float)((this._random.NextDouble() - 0.5) * radius * 4), (float)(this._random.NextDouble() * Math.Tau));
            bool result = detector.IsColliding(bounds, pose);
            if (result && hits.Count < 1024) { hits.Add(pose); }
            if (!result && nearMisses.Count < 1024) { nearMisses.Add(pose); }
        }
        if (hits.Count != 1024 || nearMisses.Count != 1024) { throw new InvalidOperationException("Insufficient benchmark samples."); }
        return [("Uniform", uniform), ("Hits", hits.ToArray()), ("Near misses", nearMisses.ToArray()), ("Far away", far)];
    }

    private void Compare(string name, TrackCollisionDetector detector, RectangleLocalBounds bounds, RectanglePose[] poses) {
        int indexedHits = Queries(detector, bounds, poses, 1, false), linearHits = Queries(detector, bounds, poses, 1, true);
        if (indexedHits != linearHits) { throw new InvalidOperationException("Benchmark result mismatch: " + name); }
        Timing indexed = Measure(detector, bounds, poses, false), linear = Measure(detector, bounds, poses, true);
        this._rows.Add(new {
            Name = name, PoseCount = poses.Length, Hits = indexedHits, Mode = Validation.Mode(detector), Indexed = indexed, Linear = linear,
            Speedup = linear.MedianNanoseconds / indexed.MedianNanoseconds
        });
        Console.WriteLine($"Measured {name}: indexed={indexed.MedianNanoseconds:F1} ns, linear={linear.MedianNanoseconds:F1} ns, mode={Validation.Mode(detector)}.");
    }

    private static Timing Measure(TrackCollisionDetector detector, RectangleLocalBounds bounds, RectanglePose[] poses, bool linear) {
        _observableChecksum ^= Queries(detector, bounds, poses, 32, linear);
        long calibrationStart = Stopwatch.GetTimestamp();
        _observableChecksum ^= Queries(detector, bounds, poses, 8, linear);
        double calibrationMilliseconds = Stopwatch.GetElapsedTime(calibrationStart).TotalMilliseconds;
        int repeats = Math.Clamp((int)Math.Ceiling(8 * 25 / Math.Max(calibrationMilliseconds, 0.001)), 1, 32768);
        double[] samples = new double[9];
        long allocated = 0;
        long queryCount = (long)poses.Length * repeats;
        for (int trial = 0; trial < samples.Length; ++trial) {
            long before = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            _observableChecksum ^= Queries(detector, bounds, poses, repeats, linear);
            long elapsed = Stopwatch.GetTimestamp() - start;
            allocated += GC.GetAllocatedBytesForCurrentThread() - before;
            samples[trial] = elapsed * (1e9 / Stopwatch.Frequency) / queryCount;
        }
        Array.Sort(samples);
        return new Timing(samples[4], samples[0], samples[8], allocated, queryCount * samples.Length);
    }

    private static int Queries(TrackCollisionDetector detector, RectangleLocalBounds bounds, RectanglePose[] poses, int repeats, bool linear) {
        int hits = 0;
        if (linear) {
            for (int repeat = 0; repeat < repeats; ++repeat) {
                for (int index = 0; index < poses.Length; ++index) { if (detector.IsCollidingLinear(bounds, poses[index])) { ++hits; } }
            }
        } else {
            for (int repeat = 0; repeat < repeats; ++repeat) {
                for (int index = 0; index < poses.Length; ++index) { if (detector.IsColliding(bounds, poses[index])) { ++hits; } }
            }
        }
        return hits;
    }

    private void Construction(string name, ColliderJson data, RectangleLocalBounds representative, double scale) {
        _ = new TrackCollisionDetector(data, representative, scale);
        double[] samples = new double[15];
        long allocation = 0;
        TrackCollisionDetector? detector = null;
        for (int index = 0; index < samples.Length; ++index) {
            long before = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            detector = new TrackCollisionDetector(data, representative, scale);
            samples[index] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            allocation += GC.GetAllocatedBytesForCurrentThread() - before;
        }
        Array.Sort(samples);
        TrackCollisionDetector[] retained = new TrackCollisionDetector[8];
        long memoryBefore = GC.GetTotalMemory(true);
        for (int index = 0; index < retained.Length; ++index) { retained[index] = new TrackCollisionDetector(data, representative, scale); }
        long retainedBytes = (GC.GetTotalMemory(true) - memoryBefore) / retained.Length;
        GC.KeepAlive(retained);
        this._construction.Add(new {
            Name = name, MedianMilliseconds = samples[7], MinimumMilliseconds = samples[0], MaximumMilliseconds = samples[14],
            MeanAllocatedBytes = allocation / samples.Length, ApproximateRetainedBytes = retainedBytes, Mode = Validation.Mode(detector!),
            detector!.EdgeCount, detector.CellSize, detector.GridCellCount, detector.StoredGridEdgeReferenceCount, detector.OutlierBvhNodeCount
        });
    }
}

internal sealed record Timing(double MedianNanoseconds, double MinimumNanoseconds, double MaximumNanoseconds, long AllocatedBytes, long QueryCount);
