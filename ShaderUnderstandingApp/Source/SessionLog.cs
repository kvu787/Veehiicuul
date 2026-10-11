using System.IO;
using System.Reflection;
using System.Text.Json;

namespace ShaderUnderstanding;

internal sealed class SessionLog {
    public string DirectoryPath { get; }
    private readonly string filePath;
    public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SessionLog(string? suppliedDirectory) {
        if (suppliedDirectory is not null) {
            this.DirectoryPath = Path.GetFullPath(suppliedDirectory);
        } else {
            string applicationDirectory = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(attribute => attribute.Key == "ApplicationDirectory").Value!;
            string parent = Path.Combine(applicationDirectory, "LogOutput");
            Directory.CreateDirectory(parent);
            string candidate = Path.Combine(parent, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            // Separate sessions started in the same second without changing the
            // repository's required timestamp format. This is startup only.
            while (Directory.Exists(candidate)) {
                Thread.Sleep(30);
                candidate = Path.Combine(parent, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            }
            this.DirectoryPath = candidate;
        }
        Directory.CreateDirectory(this.DirectoryPath);
        this.filePath = Path.Combine(this.DirectoryPath, "Application.log");
        this.Write($"Started ShaderUnderstandingApp; process {Environment.ProcessId}; .NET {Environment.Version}; Windows {Environment.OSVersion.Version}.");
    }

    public void Write(string text) => File.AppendAllText(this.filePath, $"{DateTimeOffset.Now:O} {text}{Environment.NewLine}");
    public void SaveParameters(PaintParameters parameters) => File.WriteAllText(Path.Combine(this.DirectoryPath, "Parameters.json"), JsonSerializer.Serialize(parameters, JsonOptions));
}
