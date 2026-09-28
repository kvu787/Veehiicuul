using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

public static class SceneUtility {
    public static Node Load(Node parent, string resourcePath) {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);

        using PackedScene scene = ResourceLoader.Load<PackedScene>(resourcePath, cacheMode: ResourceLoader.CacheMode.Ignore)
            ?? throw new InvalidOperationException($"Godot couldn't load PackedScene from resourcePath='{resourcePath}'");
        Node sceneInstance = scene.Instantiate()
            ?? throw new InvalidOperationException($"Godot couldn't instantiate PackedScene from resourcePath='{resourcePath}'");
        parent.AddChild(sceneInstance);
        return sceneInstance;
    }
}
