using System;

namespace Veehiicuul_Godot_CSharp;

[Serializable]
public sealed class Outline {
    public required CoordinateXY[] Vertices { get; set; }
}
