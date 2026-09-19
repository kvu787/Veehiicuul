using Godot;
using System;
using System.IO;
using System.Text.Json;

namespace Veehiicuul_Godot_CSharp;

public static class JsonUtility {
    private static readonly JsonSerializerOptions Options = new() { IncludeFields = true };

    public static T Deserialize<T>(string relativePath) {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        // Resource paths work both in the editor and in an exported Godot package.
        string resourcePath = $"res://TrackData/{relativePath.Replace('\\', '/')}";
        using Godot.FileAccess file = Godot.FileAccess.Open(resourcePath, Godot.FileAccess.ModeFlags.Read)
            ?? throw new FileNotFoundException($"Cannot read JSON '{resourcePath}': {Godot.FileAccess.GetOpenError()}.", resourcePath);
        string contents = file.GetAsText();
        Error error = file.GetError();
        if (error != Error.Ok && error != Error.FileEof) {
            throw new IOException($"Failed reading JSON '{resourcePath}': {error}.");
        }
        return JsonSerializer.Deserialize<T>(contents, Options)
            ?? throw new JsonException($"JSON '{resourcePath}' contained null instead of {typeof(T).Name}.");
    }
}
