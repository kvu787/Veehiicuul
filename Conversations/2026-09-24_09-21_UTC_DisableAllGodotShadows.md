# Disable All Godot Shadows

## User

how do i disable all shadows in a godot game

## Assistant Commentary

I’ll check Godot’s documentation for the global and per-light shadow controls.

## Assistant

For **Godot 4.x in 3D**, disable shadows on **every light**:

1. Select the light node (`DirectionalLight3D`, `OmniLight3D`, or `SpotLight3D`).
2. In the Inspector, expand **Shadow**.
3. Uncheck **Enabled**.

This disables that light’s real-time shadow rendering while keeping its illumination. [Godot documentation](https://docs.godotengine.org/en/stable/classes/class_light3d.html#class-light3d-property-shadow-enabled)

In C#, the equivalent is:

```csharp
light.ShadowEnabled = false;
```

To apply it to every light currently in the scene tree:

```csharp
private static void DisableShadows(Node node)
{
    if (node is Light3D light)
        light.ShadowEnabled = false;

    foreach (Node child in node.GetChildren())
        DisableShadows(child);
}
```

Call `DisableShadows(GetTree().Root);` after your scene loads. Lights spawned later need the same setting applied.

**Baked shadows and ambient occlusion are separate**—turning off light shadows won’t remove darkening already stored in textures or lightmaps.
