using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Veehiicuul_Godot_CSharp;

// The collision manager names three application types. These stand-ins were written
// for this harness and provide only the members the manager uses. Nothing else of the
// application is compiled into this project.

/// <summary>A vehicle as the collision manager sees it: a scene node.</summary>
public sealed class Car {
    public Node3D? Node { get; set; }
}

/// <summary>The list of vehicles and the selected one.</summary>
public sealed class CarSwitcher {
    private readonly List<Car> _cars;

    public CarSwitcher(List<Car> cars) {
        ArgumentNullException.ThrowIfNull(cars);
        this._cars = cars;
    }

    public int CurrentCarIndex { get; set; }
    public IReadOnlyList<Car> AvailableCars => this._cars;
}

/// <summary>
/// Serves collider documents that the harness generated in memory, under the
/// resource paths the collision manager asks for.
/// </summary>
public static class JsonUtility {
    private static readonly Dictionary<string, string> Documents = new(StringComparer.Ordinal);

    public static int RequestCount { get; private set; }

    public static void Register(string resourcePath, string json) {
        Documents[resourcePath] = json;
    }

    public static T Deserialize<T>(string resourcePath) {
        ++RequestCount;
        if (!Documents.TryGetValue(resourcePath, out string? json)) {
            throw new InvalidOperationException($"No document was registered for '{resourcePath}'.");
        }

        return JsonSerializer.Deserialize<T>(json)
            ?? throw new JsonException($"'{resourcePath}' contained null.");
    }
}
