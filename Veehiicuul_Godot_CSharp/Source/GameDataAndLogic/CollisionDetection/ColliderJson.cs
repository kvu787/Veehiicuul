using System;
using System.Collections.Generic;

namespace Veehiicuul_Godot_CSharp;
/// <summary>
/// Track-outline collision data exported from Blender. Version 1 coordinates
/// are Blender world-space X/Y values. The collision plane retains ZoomTracks'
/// (-X, -Y) mapping; its X/Y axes correspond to Godot world X/negative Z.
/// </summary>
[Serializable]
public class ColliderJson {
    public const int CurrentFormatVersion = 1;
    public const string BlenderWorldXYCoordinateSystem = "BlenderWorldXY";

    public int FormatVersion { get; set; }
    public string CoordinateSystem { get; set; } = string.Empty;
    public List<Outline> Outlines { get; set; } = [];
}
