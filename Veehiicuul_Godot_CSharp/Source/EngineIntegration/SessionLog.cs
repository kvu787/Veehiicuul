using Godot;
using System;
using System.Globalization;
using System.IO;
using Environment = System.Environment;

namespace Veehiicuul_Godot_CSharp;

internal static class SessionLog {
    private static string? DirectoryPath;

    public static string CreateDirectory() {
        if (DirectoryPath is not null) {
            return DirectoryPath;
        }
        string? launcherDirectory = Environment.GetEnvironmentVariable("VEEHIICUUL_LOG_DIRECTORY");
        DirectoryPath = launcherDirectory ?? Path.Combine(ProjectSettings.GlobalizePath("res://MyLogOutput"),
            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture));
        _ = Directory.CreateDirectory(DirectoryPath);
        return DirectoryPath;
    }
}
