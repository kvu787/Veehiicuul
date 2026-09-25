using Godot;
using System;
using System.Diagnostics;
using System.IO;

namespace Veehiicuul_Godot_CSharp;

public sealed class StutterLogger : IDisposable {
    private readonly StreamWriter? Writer;
    private readonly TimeManager TimeManager;
    private readonly double FrameDurationThresholdMilliseconds;
    private long PreviousFrameTimeTicks;

    public StutterLogger(string sessionLogDirectory, TimeManager timeManager) {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionLogDirectory);
        ArgumentNullException.ThrowIfNull(timeManager);
        this.TimeManager = timeManager;
        if (timeManager.UseTimeDeltaTime) {
            GD.Print("Using variable delta, so StutterLogger will not report stutters.");
            return;
        }
        _ = Directory.CreateDirectory(sessionLogDirectory);
        this.Writer = new StreamWriter(Path.Combine(sessionLogDirectory, "Stutter.log"));
        this.PreviousFrameTimeTicks = Stopwatch.GetTimestamp();
        this.FrameDurationThresholdMilliseconds = 1000.0 * 1.2 / timeManager.RefreshRate;
    }

    public void Update() {
        if (this.Writer is null) {
            return;
        }
        long currentFrameTimeTicks = Stopwatch.GetTimestamp();
        long frameDurationTicks = currentFrameTimeTicks - this.PreviousFrameTimeTicks;
        double frameDurationMilliseconds = frameDurationTicks * 1000.0 / Stopwatch.Frequency;
        if (frameDurationMilliseconds > this.FrameDurationThresholdMilliseconds) {
            this.Writer.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] Frame {this.TimeManager.FrameCount} took {frameDurationMilliseconds:F4} ms ({frameDurationTicks} ticks)");
            this.Writer.Flush();
        }
        this.PreviousFrameTimeTicks = currentFrameTimeTicks;
    }

    public void InsertSpacer() {
        if (this.Writer is null) {
            return;
        }
        this.Writer.WriteLine();
        this.Writer.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] Spacer ########################################");
        this.Writer.WriteLine();
        this.Writer.Flush();
    }

    public void Dispose() {
        this.Writer?.Dispose();
    }
}
