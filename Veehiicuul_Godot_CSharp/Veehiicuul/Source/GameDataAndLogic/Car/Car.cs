using Godot;
using System.Text.Json.Serialization;

namespace Veehiicuul_Godot_CSharp;

public sealed class Car {
    public required string GameObjectName { get; set; }
    public required CarDynamic Dynamic { get; set; }

    [JsonIgnore]
    public Node3D? Node { get; set; }
}
