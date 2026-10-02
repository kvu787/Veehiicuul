namespace Veehiicuul_Godot_CSharp;

public struct CoordinateXY {
    public CoordinateXY(float x, float y) {
        Guard.ThrowIfNotFinite(x, nameof(x));
        Guard.ThrowIfNotFinite(y, nameof(y));
        this.X = x;
        this.Y = y;
    }

    public required float X { get; set; }

    public required float Y { get; set; }
}
