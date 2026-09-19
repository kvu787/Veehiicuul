using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Veehiicuul_Godot_CSharp;

/// <summary>Stops the process, rather than letting Godot log a C# exception and continue.</summary>
internal static class QuitOnException {
    private static int Stopping;
    private static string? LogDirectory;

    // Called explicitly by _Ready, never by an autoload or module initializer.
    // FirstChanceException also catches exceptions that a library would otherwise swallow.
    public static void Install(string logDirectory) {
        LogDirectory = logDirectory;
        AppDomain.CurrentDomain.FirstChanceException += OnExceptionThrown;
    }

    private static void OnExceptionThrown(object? sender, FirstChanceExceptionEventArgs arguments) {
        Stop(arguments.Exception);
    }

    public static void Stop(Exception exception) {
        // If reporting itself throws, stop immediately instead of recursively reporting it.
        if (Interlocked.Exchange(ref Stopping, 1) != 0) {
            Terminate();
        }
        try {
            string report = exception.ToString();
            Console.Error.WriteLine(report);
            string directory = LogDirectory ?? SessionLog.CreateDirectory();
            File.WriteAllText(Path.Combine(directory, "Fatal.log"), report + Environment.NewLine);
        } finally {
            Terminate();
        }
    }

    [DoesNotReturn]
    private static void Terminate() {
        // SceneTree.Quit waits for another frame. Environment.Exit tears down .NET while
        // Godot still owns native callbacks and can trigger a CLR shutdown assertion.
        // Killing this process skips both paths, as intended for fatal exceptions.
        AppDomain.CurrentDomain.FirstChanceException -= OnExceptionThrown;
        try {
            using Process process = Process.GetCurrentProcess();
            process.Kill();
        } catch (Exception exception) {
            Environment.FailFast("Could not terminate the process after a fatal exception.", exception);
        }
        Environment.FailFast("The fatal-exception process termination unexpectedly returned.");
    }
}
