using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace VeehiicuulExceptionHandlingVerification;

internal static class Program {
    private const string ProbeMessage = "Exception handling verification probe";

    public static int Main(string[] arguments) {
        if (arguments.Length == 3 && arguments[0] == "--probe") {
            return RunProbe(arguments[1], arguments[2]);
        }

        string logDirectory = Path.Combine(FindApplicationDirectory(), "MyLogOutput",
            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"), "ExceptionHandling");
        _ = Directory.CreateDirectory(logDirectory);
        StringBuilder report = new();
        try {
            RunCase("CaughtException", logDirectory, report);
            RunCase("DirectStop", logDirectory, report);
            RunCase("LoggingFailure", logDirectory, report);
            _ = report.AppendLine("PASS: All three fatal-exception probes exited with a failure status before continuation.");
            Console.Write(report);
            return 0;
        } catch (Exception exception) {
            _ = report.AppendLine("FAIL: " + exception);
            Console.Error.Write(report);
            return 1;
        } finally {
            File.WriteAllText(Path.Combine(logDirectory, "Verification.log"), report.ToString());
        }
    }

    private static void RunCase(string name, string logDirectory, StringBuilder report) {
        string caseDirectory = Path.Combine(logDirectory, name);
        _ = Directory.CreateDirectory(caseDirectory);
        if (name == "LoggingFailure") {
            // A directory at the file destination makes writing Fatal.log fail
            // deterministically without depending on platform ACL behavior.
            _ = Directory.CreateDirectory(Path.Combine(caseDirectory, "Fatal.log"));
        }

        ProcessStartInfo start = new() {
            FileName = Environment.ProcessPath
                ?? throw new InvalidOperationException("The verifier executable path is unavailable."),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (string.Equals(Path.GetFileNameWithoutExtension(start.FileName), "dotnet", StringComparison.OrdinalIgnoreCase)) {
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        }
        start.ArgumentList.Add("--probe");
        start.ArgumentList.Add(name);
        start.ArgumentList.Add(caseDirectory);
        start.Environment["VEEHIICUUL_LOG_DIRECTORY"] = caseDirectory;

        using Process child = Process.Start(start)
            ?? throw new InvalidOperationException($"Could not start probe '{name}'.");
        bool exited = child.WaitForExit(10_000);
        if (!exited) {
            child.Kill(entireProcessTree: true);
            child.WaitForExit();
        }
        string output = child.StandardOutput.ReadToEnd();
        string error = child.StandardError.ReadToEnd();
        File.WriteAllText(Path.Combine(caseDirectory, "Process.log"),
            $"ExitCode={child.ExitCode}{Environment.NewLine}{output}{error}");
        Require(exited, $"{name} did not exit within ten seconds.");
        Require(child.ExitCode != 0, $"{name} exited successfully, expected a failure status. See '{caseDirectory}'.");
        Require(File.Exists(Path.Combine(caseDirectory, "BeforeFailure.txt")),
            $"{name} failed before reaching the intended probe.");
        Require(!File.Exists(Path.Combine(caseDirectory, "AfterFailure.txt")),
            $"{name} continued execution after its fatal exception.");
        Require(!File.Exists(Path.Combine(caseDirectory, "CatchExecuted.txt")),
            $"{name} entered a catch block after its fatal exception.");
        Require(error.Contains(ProbeMessage, StringComparison.Ordinal),
            $"{name} did not report the intended exception to standard error.");
        if (name != "LoggingFailure") {
            string fatalLog = Path.Combine(caseDirectory, "Fatal.log");
            Require(File.Exists(fatalLog), $"{name} did not create Fatal.log.");
            Require(File.ReadAllText(fatalLog).Contains(ProbeMessage, StringComparison.Ordinal),
                $"{name} did not log the intended exception.");
        }
        _ = report.AppendLine($"PASS: {name}; exit={child.ExitCode}; no catch or continuation.");
    }

    private static int RunProbe(string name, string logDirectory) {
        Assembly application = Assembly.Load("Veehiicuul_Godot_CSharp");
        Type handler = application.GetType("Veehiicuul_Godot_CSharp.QuitOnException", throwOnError: true)
            ?? throw new InvalidOperationException("The application's exception handler was not found.");
        Action<string> install = GetMethod(handler, "Install").CreateDelegate<Action<string>>();
        Action<Exception> stop = GetMethod(handler, "Stop").CreateDelegate<Action<Exception>>();

        install(logDirectory);
        File.WriteAllText(Path.Combine(logDirectory, "BeforeFailure.txt"), "Reached the intended probe.");
        if (name == "CaughtException") {
            try {
                throw new InvalidOperationException(ProbeMessage);
            } catch (InvalidOperationException) {
                File.WriteAllText(Path.Combine(logDirectory, "CatchExecuted.txt"), "The catch block ran.");
            }
        } else {
            stop(new InvalidOperationException(ProbeMessage));
        }
        File.WriteAllText(Path.Combine(logDirectory, "AfterFailure.txt"), "Execution continued.");
        return 0;
    }

    private static MethodInfo GetMethod(Type type, string name) {
        return type.GetMethod(name, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Exception handler method '{name}' was not found.");
    }

    private static string FindApplicationDirectory() {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null) {
            if (File.Exists(Path.Combine(current.FullName, "project.godot"))) {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the Godot application's project.godot.");
    }

    private static void Require(bool condition, string message) {
        if (!condition) {
            throw new InvalidOperationException(message);
        }
    }
}
