using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>An operation that a timed loop invokes by index.</summary>
internal interface IWorkload {
    int Length { get; }
    bool Invoke(int index);
}

internal readonly struct IndexedQueries(
    TrackCollisionDetector detector, RectangleLocalBounds footprint, RectanglePose[] poses) : IWorkload {
    private readonly TrackCollisionDetector _detector = detector;
    private readonly RectangleLocalBounds _footprint = footprint;
    private readonly RectanglePose[] _poses = poses;

    public int Length => this._poses.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Invoke(int index) {
        return this._detector.IsColliding(this._footprint, this._poses[index]);
    }
}

internal readonly struct LinearQueries(
    TrackCollisionDetector detector, RectangleLocalBounds footprint, RectanglePose[] poses) : IWorkload {
    private readonly TrackCollisionDetector _detector = detector;
    private readonly RectangleLocalBounds _footprint = footprint;
    private readonly RectanglePose[] _poses = poses;

    public int Length => this._poses.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Invoke(int index) {
        return this._detector.IsCollidingLinear(this._footprint, this._poses[index]);
    }
}

/// <summary>Reads the pose and does nothing else; measures the cost of the loop itself.</summary>
internal readonly struct EmptyLoop(RectanglePose[] poses) : IWorkload {
    private readonly RectanglePose[] _poses = poses;

    public int Length => this._poses.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Invoke(int index) {
        return this._poses[index].PositionX > 1e30f;
    }
}

internal readonly struct OrientationCalls(float[] values, OrientationPredicate predicate) : IWorkload {
    private readonly float[] _values = values;
    private readonly OrientationPredicate _predicate = predicate;

    public int Length => this._values.Length / 6;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Invoke(int index) {
        int start = index * 6;
        float[] values = this._values;
        return this._predicate(
            values[start], values[start + 1], values[start + 2],
            values[start + 3], values[start + 4], values[start + 5]) > 0;
    }
}

internal sealed class Measurement {
    public required string Name { get; init; }
    public required int Calls { get; init; }
    public required long Contacts { get; init; }
    public required int Repetitions { get; init; }
    public required double WarmupMilliseconds { get; init; }
    public required int WarmupBatches { get; init; }
    public required bool WarmupSettled { get; init; }
    public required double[] NanosecondsPerCall { get; init; }
    public required double BytesPerCall { get; init; }
    public SortedDictionary<string, string> Facts { get; } = new(StringComparer.Ordinal);

    public double Median => Statistics.Percentile(this.NanosecondsPerCall, 0.5);
    public double Minimum => Statistics.Percentile(this.NanosecondsPerCall, 0.0);
    public double Maximum => Statistics.Percentile(this.NanosecondsPerCall, 1.0);
    public double Tenth => Statistics.Percentile(this.NanosecondsPerCall, 0.1);
    public double Ninetieth => Statistics.Percentile(this.NanosecondsPerCall, 0.9);

    public string Summary() {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{this.Name}: median={this.Median:F2} ns; p10={this.Tenth:F2}; p90={this.Ninetieth:F2}; "
            + $"min={this.Minimum:F2}; max={this.Maximum:F2}; contacts={this.Contacts}/{this.Calls}; "
            + $"bytes={this.BytesPerCall:F3}; warmup={this.WarmupMilliseconds:F0} ms"
            + $"{(this.WarmupSettled ? string.Empty : " (did not settle)")}");
    }
}

internal static class Statistics {
    /// <summary>Linear interpolation between order statistics.</summary>
    public static double Percentile(double[] values, double fraction) {
        double[] ordered = (double[])values.Clone();
        Array.Sort(ordered);
        double position = fraction * (ordered.Length - 1);
        int lower = (int)Math.Floor(position);
        int upper = Math.Min(ordered.Length - 1, lower + 1);
        return ordered[lower] + (ordered[upper] - ordered[lower]) * (position - lower);
    }

    public static double Mean(double[] values) {
        double sum = 0.0;
        foreach (double value in values) {
            sum += value;
        }

        return sum / values.Length;
    }
}

/// <summary>
/// Timing of a workload in a loop. Warmup continues until consecutive batches
/// agree, because the runtime replaces code while it runs: a fixed warmup time
/// can end before the final code is installed.
/// </summary>
internal static class Bench {
    public const int SampleCount = 25;
    private const double MinimumWarmupMilliseconds = 400.0;
    private const double MaximumWarmupMilliseconds = 6000.0;
    private const double TargetBatchMilliseconds = 25.0;
    private const int SettleWindow = 10;
    private const double SettleSpread = 1.04;

    public static long Sink;

    public static Measurement Run<T>(string name, T workload) where T : struct, IWorkload {
        if (workload.Length == 0) {
            throw new ArgumentException("The workload is empty: " + name);
        }

        long contacts = Batch(ref workload, 1);

        // Warmup in batches of a few milliseconds so that the method is entered often.
        int repetitions = 1;
        long start = Stopwatch.GetTimestamp();
        Queue<double> recent = new();
        int batches = 0;
        bool settled = false;
        double elapsed = 0.0;
        while (elapsed < MaximumWarmupMilliseconds) {
            long batchStart = Stopwatch.GetTimestamp();
            Sink += Batch(ref workload, repetitions);
            double batchMilliseconds = Stopwatch.GetElapsedTime(batchStart).TotalMilliseconds;
            ++batches;
            elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (batchMilliseconds < 2.0 && repetitions < (1 << 24)) {
                repetitions *= 2;
                recent.Clear();
                continue;
            }

            recent.Enqueue(batchMilliseconds / repetitions);
            if (recent.Count > SettleWindow) {
                _ = recent.Dequeue();
            }

            if (elapsed >= MinimumWarmupMilliseconds && recent.Count == SettleWindow) {
                double low = double.PositiveInfinity, high = 0.0;
                foreach (double value in recent) {
                    low = Math.Min(low, value);
                    high = Math.Max(high, value);
                }

                if (high <= low * SettleSpread) {
                    settled = true;
                    break;
                }
            }
        }

        double perRepetition = 0.0;
        foreach (double value in recent) {
            perRepetition += value;
        }

        perRepetition /= Math.Max(1, recent.Count);
        int timedRepetitions = Math.Max(1, (int)Math.Round(TargetBatchMilliseconds / Math.Max(perRepetition, 1e-6)));
        double[] samples = new double[SampleCount];
        for (int sample = 0; sample < SampleCount; ++sample) {
            long batchStart = Stopwatch.GetTimestamp();
            long count = Batch(ref workload, timedRepetitions);
            long ticks = Stopwatch.GetTimestamp() - batchStart;
            if (count != contacts * timedRepetitions) {
                throw new InvalidOperationException("The contact count changed between batches: " + name);
            }

            Sink += count;
            samples[sample] = ticks * (1e9 / Stopwatch.Frequency) / ((double)timedRepetitions * workload.Length);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        Sink += Batch(ref workload, timedRepetitions);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        return new Measurement {
            Name = name,
            Calls = workload.Length,
            Contacts = contacts,
            Repetitions = timedRepetitions,
            WarmupMilliseconds = elapsed,
            WarmupBatches = batches,
            WarmupSettled = settled,
            NanosecondsPerCall = samples,
            BytesPerCall = allocated / ((double)timedRepetitions * workload.Length),
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Batch<T>(ref T workload, int repetitions) where T : struct, IWorkload {
        long contacts = 0;
        int length = workload.Length;
        for (int repetition = 0; repetition < repetitions; ++repetition) {
            for (int index = 0; index < length; ++index) {
                if (workload.Invoke(index)) {
                    ++contacts;
                }
            }
        }

        return contacts;
    }
}
