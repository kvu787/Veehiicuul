using Godot;
using System.Text.Json.Serialization;

namespace Veehiicuul_Godot_CSharp;

/// <summary>A JSON car description and its instantiated Godot node.</summary>
public sealed class Car {
    // Retain the authored JSON name; the object it names is now a Node3D.
    public string GameObjectName { get; set; } = string.Empty;
    public CarDynamic Dynamic { get; set; } = new();

    [JsonIgnore]
    public Node3D? Node { get; set; }
}
