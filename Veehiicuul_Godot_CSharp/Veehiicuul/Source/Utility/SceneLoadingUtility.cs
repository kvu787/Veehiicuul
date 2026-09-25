using Godot;
using System;
using System.Diagnostics;
using System.IO;

namespace Veehiicuul_Godot_CSharp;

/// <summary>Synchronous replacement for AwaitableUtility and additive Unity scene loading.</summary>
public static class SceneLoadingUtility {
    public static T LoadAndAttach<T>(Node parent, string resourcePath) where T : Node {
        ArgumentNullException.ThrowIfNull(parent);
        if (!ResourceLoader.Exists(resourcePath, "PackedScene")) {
            throw new FileNotFoundException($"Required Godot scene '{resourcePath}' is missing. This code port deliberately does not convert Unity scenes or assets.", resourcePath);
        }
        long started = Stopwatch.GetTimestamp();
        GD.Print($"Load scene '{resourcePath}'...");
        using PackedScene scene = ResourceLoader.Load<PackedScene>(resourcePath)
            ?? throw new InvalidOperationException($"Godot could not load PackedScene '{resourcePath}'.");
        Node instance = scene.Instantiate();
        if (instance is not T result) {
            instance.Free();
            throw new InvalidOperationException($"Scene '{resourcePath}' must have a {typeof(T).Name} root.");
        }
        ValidatePassiveScene(instance, resourcePath);
        parent.AddChild(result);
        GD.Print($"Loaded '{resourcePath}' in {Stopwatch.GetElapsedTime(started).TotalMilliseconds:F2} ms.");
        return result;
    }

    private static void ValidatePassiveScene(Node node, string resourcePath) {
        if (node.GetScript().VariantType != Variant.Type.Nil) {
            throw new InvalidOperationException($"Scene '{resourcePath}' contains a script on '{node.Name}'. Main is the only scripted Godot node in this port.");
        }
        // Every imported scene is passive data. Main controls camera/car/UI changes explicitly.
        node.ProcessMode = Node.ProcessModeEnum.Disabled;
        for (int index = 0; index < node.GetChildCount(); index++) {
            ValidatePassiveScene(node.GetChild(index), resourcePath);
        }
    }
}
