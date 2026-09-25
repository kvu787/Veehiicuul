using System;

namespace Veehiicuul_Godot_CSharp;

[Serializable]
public struct CoordinateXY {
    public CoordinateXY(float x, float y) {
        Guard.ThrowIfNotFinite(x, nameof(x));
        Guard.ThrowIfNotFinite(y, nameof(y));
        this.X = x;
        this.Y = y;
    }

    public float X { get; set; }

    public float Y { get; set; }
}
