using System;
using System.Collections.Generic;

namespace Veehiicuul_Godot_CSharp;

[Serializable]
public class Outline {
    public List<CoordinateXY> Vertices { get; set; } = [];
}
