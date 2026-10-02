using Godot;
using System.Text.Json.Serialization;

namespace Veehiicuul_Godot_CSharp;

public sealed class Car {
    public string GameObjectName { get; set; } = string.Empty;
    public CarDynamic Dynamic { get; set; } = new();

    [JsonIgnore]
    public Node3D? Node { get; set; }
}
