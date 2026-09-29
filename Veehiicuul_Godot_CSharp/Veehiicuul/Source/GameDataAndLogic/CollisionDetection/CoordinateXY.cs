using System;
using System.Text.Json.Serialization;

namespace Veehiicuul_Godot_CSharp;

[Serializable]
public struct CoordinateXY {
    public CoordinateXY(float x, float y) {
        Guard.ThrowIfNotFinite(x, nameof(x));
        Guard.ThrowIfNotFinite(y, nameof(y));
        this.X = x;
        this.Y = y;
    }

    [JsonRequired]
    public float X { get; set; }

    [JsonRequired]
    public float Y { get; set; }
}
