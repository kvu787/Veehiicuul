using System;
using System.Collections.Generic;

namespace Veehiicuul_Godot_CSharp;
/// <summary>
/// Track-outline collision data exported from Blender. Coordinates
/// are Blender world-space X/Y values. glTF maps these to Godot world X/negative Z,
/// so the collision plane uses the exported X/Y unchanged.
/// </summary>
[Serializable]
public class ColliderJson {
    public List<Outline> Outlines { get; set; } = [];
}
