using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class TimeManager {
    public float RefreshRate { get; }
    public bool UseTimeDeltaTime { get; }
    public float DeltaTime { get; private set; }
    public double FrameDeltaTime { get; private set; }
    public ulong FrameCount { get; private set; }

    public TimeManager(float? refreshRate, bool useTimeDeltaTime) {
        if (!useTimeDeltaTime && (!refreshRate.HasValue || !float.IsFinite(refreshRate.Value) || refreshRate.Value <= 0f)) {
            throw new ArgumentOutOfRangeException(nameof(refreshRate), "A fixed timestep requires a finite positive refresh rate.");
        }
        this.UseTimeDeltaTime = useTimeDeltaTime;
        this.RefreshRate = refreshRate.GetValueOrDefault();
    }

    /// <summary>Receives the sole frame callback's delta; fixed-step mode does not create a physics loop.</summary>
    public void Update(double delta) {
        if (!double.IsFinite(delta) || delta < 0 || delta > float.MaxValue) {
            throw new ArgumentOutOfRangeException(nameof(delta));
        }
        this.FrameDeltaTime = delta;
        this.DeltaTime = this.UseTimeDeltaTime ? (float)delta : 1f / this.RefreshRate;
        this.FrameCount++;
    }
}
