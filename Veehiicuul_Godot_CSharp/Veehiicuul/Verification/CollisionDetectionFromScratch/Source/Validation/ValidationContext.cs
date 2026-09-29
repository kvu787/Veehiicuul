using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>Outcome of one validation suite. A suite never stops at its first failure.</summary>
internal sealed class SuiteResult {
    private const int MaximumRecordedFailures = 12;
    private readonly Lock _lock = new();
    private long _cases;
    private long _failures;

    public SuiteResult(string name) {
        this.Name = name;
    }

    public string Name { get; }
    public long Cases => Interlocked.Read(ref this._cases);
    public long Failures => Interlocked.Read(ref this._failures);
    public List<string> FailureSamples { get; } = [];
    public SortedDictionary<string, string> Facts { get; } = new(StringComparer.Ordinal);
    public double Seconds { get; set; }

    public void AddCases(long count) {
        Interlocked.Add(ref this._cases, count);
    }

    public void Fail(string description) {
        Interlocked.Increment(ref this._failures);
        lock (this._lock) {
            if (this.FailureSamples.Count < MaximumRecordedFailures) {
                this.FailureSamples.Add(description);
            }
        }
    }

    /// <summary>Counts one case and records a failure when the condition is false.</summary>
    public void Check(bool condition, string description) {
        Interlocked.Increment(ref this._cases);
        if (!condition) {
            this.Fail(description);
        }
    }

    public void Fact(string key, string value) {
        lock (this._lock) {
            this.Facts[key] = value;
        }
    }

    public void Fact(string key, double value) {
        this.Fact(key, value.ToString("R", CultureInfo.InvariantCulture));
    }

    public void Fact(string key, long value) {
        this.Fact(key, value.ToString(CultureInfo.InvariantCulture));
    }
}

internal sealed class ValidationContext {
    public ValidationContext(double workScale, int threads) {
        this.WorkScale = workScale;
        this.Threads = threads;
    }

    /// <summary>Multiplier for randomized case counts. One is the full run.</summary>
    public double WorkScale { get; }
    public int Threads { get; }
    public List<SuiteResult> Results { get; } = [];

    public int Scaled(int count) {
        return Math.Max(1, (int)Math.Round(count * this.WorkScale));
    }

    public void Run(string name, Action<SuiteResult> suite) {
        SuiteResult result = new(name);
        long start = Stopwatch.GetTimestamp();
        try {
            suite(result);
        } catch (Exception exception) {
            result.Fail("The suite stopped with an unexpected exception: " + exception);
        }

        result.Seconds = Stopwatch.GetElapsedTime(start).TotalSeconds;
        this.Results.Add(result);
        string status = result.Failures == 0 ? "PASS" : "FAIL";
        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{status} {result.Name}: cases={result.Cases}, failures={result.Failures}, seconds={result.Seconds:F1}"));
        foreach (KeyValuePair<string, string> fact in result.Facts) {
            Console.WriteLine($"     {fact.Key} = {fact.Value}");
        }

        foreach (string sample in result.FailureSamples) {
            Console.WriteLine("     FAILURE " + sample);
        }
    }
}

/// <summary>Formatting that reproduces a binary32 value exactly.</summary>
internal static class Exact {
    public static string Text(float value) {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{value:R} [0x{BitConverter.SingleToUInt32Bits(value):X8}]");
    }

    public static string Text(RectanglePose pose) {
        return $"pose=({Text(pose.PositionX)}, {Text(pose.PositionY)}, yaw {Text(pose.RotationRadians)})";
    }

    public static string Text(RectangleLocalBounds bounds) {
        return $"bounds=({Text(bounds.MinX)}, {Text(bounds.MinY)}, {Text(bounds.MaxX)}, {Text(bounds.MaxY)})";
    }
}

/// <summary>Vehicle footprints chosen for this harness. None is read from the application.</summary>
internal static class Footprints {
    /// <summary>
    /// A car three units wide and six long, with the 0.165 front shortening that
    /// the collision manager applies to the minimum Y side.
    /// </summary>
    public static readonly RectangleLocalBounds Reference = new(-1.5f, -2.835f, 1.5f, 3f);

    public static readonly RectangleLocalBounds Small = new(-0.75f, -1.5f, 0.75f, 1.5f);
    public static readonly RectangleLocalBounds Narrow = new(-0.5f, -2.835f, 0.5f, 3f);

    /// <summary>Same size as the reference, with the local origin at the rear-left corner.</summary>
    public static readonly RectangleLocalBounds ShiftedOrigin = new(0f, 0f, 3f, 5.835f);

    /// <summary>Larger than the reference in both directions.</summary>
    public static readonly RectangleLocalBounds Oversized = new(-4f, -7f, 4f, 7f);

    /// <summary>Narrower than the reference and longer than it.</summary>
    public static readonly RectangleLocalBounds Long = new(-1.2f, -7f, 1.2f, 7f);

    /// <summary>Wider than the reference and shorter than it.</summary>
    public static readonly RectangleLocalBounds Broad = new(-4f, -2f, 4f, 2f);

    /// <summary>Longer than the reference, with the local origin outside the rectangle.</summary>
    public static readonly RectangleLocalBounds LongShifted = new(2f, 1f, 4.4f, 15f);

    /// <summary>Lattice-friendly footprint for rotations with cosine 3/5 and sine 4/5.</summary>
    public static readonly RectangleLocalBounds Lattice = new(-1.25f, -2.5f, 1.25f, 2.5f);
}
