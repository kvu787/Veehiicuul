using Godot;
using System;
using System.IO;
using System.Text.Json;

namespace Veehiicuul_Godot_CSharp;

public static class JsonUtility {
    private static readonly JsonSerializerOptions Options = new() {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = null,
    };

    public static T Deserialize<T>(string resourcePath) {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);
        // Resource paths work both in the editor and in an exported Godot package.
        using Godot.FileAccess file = Godot.FileAccess.Open(resourcePath, Godot.FileAccess.ModeFlags.Read)
            ?? throw new FileNotFoundException($"Cannot read JSON '{resourcePath}': {Godot.FileAccess.GetOpenError()}.", resourcePath);
        Error error = file.GetError();
        if (error != Error.Ok) {
            throw new IOException($"Failed reading JSON '{resourcePath}': {error}.");
        }
        string contents = file.GetAsText();
        return JsonSerializer.Deserialize<T>(contents, Options)
            ?? throw new JsonException($"JSON '{resourcePath}' contained null instead of {typeof(T).Name}.");
    }
}
