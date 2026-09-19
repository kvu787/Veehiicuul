namespace Veehiicuul_Godot_CSharp;

/// <summary>The source game's car motion settings.</summary>
public sealed class CarDynamic {
    public float VelocityLimiter { get; set; }
    public CarAccelerationMap AccelerationMap { get; set; } = new();

    // Retained from the source data. ZoomTracks does not use this threshold.
    public float MinVelocityForRotation { get; set; }
}
