namespace Veehiicuul_Godot_CSharp;

public sealed class CarDynamic {
    public float VelocityLimiter { get; set; }
    public CarAccelerationMap AccelerationMap { get; set; } = new();
}
