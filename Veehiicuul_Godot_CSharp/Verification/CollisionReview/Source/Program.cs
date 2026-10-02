using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Veehiicuul_Godot_CSharp;

namespace CollisionReview;

internal static class Program {
    private static int Main(string[] arguments) {
        try {
            if (arguments.Length < 2) { throw new ArgumentException("Expected output folder and collider JSON path."); }
            JsonSerializerOptions options = new() { WriteIndented = true };
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new BoundsConverter());
            ColliderJson data = JsonSerializer.Deserialize<ColliderJson>(File.ReadAllText(arguments[1]))!;
            using JsonDocument engine = JsonDocument.Parse(File.ReadAllText(Path.Combine(arguments[0], "EngineResults.json")));
            Vehicle[] vehicles = JsonSerializer.Deserialize<Vehicle[]>(engine.RootElement.GetProperty("Vehicles").GetRawText(), options)!;
            Stopwatch elapsed = Stopwatch.StartNew();
            object validation = new Validation().Run(data, vehicles);
            File.WriteAllText(Path.Combine(arguments[0], "ValidationResults.json"), JsonSerializer.Serialize(validation, options) + "\n");
            File.WriteAllText(Path.Combine(arguments[0], "AssetAnalysis.json"), JsonSerializer.Serialize(AssetAnalysis.Run(data), options) + "\n");
            if (arguments.Length < 3 || arguments[2] != "--skip-benchmarks") {
                object measurements = new Measurements().Run(data, vehicles);
                File.WriteAllText(Path.Combine(arguments[0], "BenchmarkResults.json"), JsonSerializer.Serialize(measurements, options) + "\n");
            }
            File.WriteAllText(Path.Combine(arguments[0], "Environment.json"), JsonSerializer.Serialize(new {
                Runtime = RuntimeInformation.FrameworkDescription, OperatingSystem = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(), LogicalProcessorCount = Environment.ProcessorCount,
                Priority = Process.GetCurrentProcess().PriorityClass.ToString(), Stopwatch.Frequency,
                elapsed.Elapsed.TotalSeconds
            }, options) + "\n");
            Console.WriteLine("All required validation checks passed.");
            return 0;
        } catch (Exception exception) {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private sealed class BoundsConverter : JsonConverter<RectangleLocalBounds> {
        public override RectangleLocalBounds Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            JsonElement value = document.RootElement;
            return new RectangleLocalBounds(value.GetProperty("MinX").GetSingle(), value.GetProperty("MinY").GetSingle(),
                value.GetProperty("MaxX").GetSingle(), value.GetProperty("MaxY").GetSingle());
        }
        public override void Write(Utf8JsonWriter writer, RectangleLocalBounds value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteNumber("MinX", value.MinX); writer.WriteNumber("MinY", value.MinY);
            writer.WriteNumber("MaxX", value.MaxX); writer.WriteNumber("MaxY", value.MaxY);
            writer.WriteEndObject();
        }
    }
}
