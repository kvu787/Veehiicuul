using Godot;
using System;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.InteropServices;

namespace CollisionDetectionFromScratch;

/// <summary>
/// Entry point of the engine-side harness. Arguments follow "--" on the command line:
/// --mode=checks, --mode=measure, or --mode=frames, and --output=path.
/// </summary>
public partial class Main : Node3D {
    private readonly NativeReport _report = new();
    private FrameMeasurement? _frames;
    private string _output = string.Empty;
    private string _mode = "checks";

    public override void _Ready() {
        try {
            Dictionary<string, string> arguments = ParseArguments(OS.GetCmdlineUserArgs());
            this._mode = arguments.GetValueOrDefault("mode", "checks");
            this._output = arguments.GetValueOrDefault("output", string.Empty);
            GD.Print($"Mode: {this._mode}; display server: {DisplayServer.GetName()}; "
                + $"runtime: {RuntimeInformation.FrameworkDescription}; "
                + $"debug build of the engine: {OS.IsDebugBuild()}");
            switch (this._mode) {
            case "checks":
                FootprintChecks.Run(this._report, this);
                ManagerChecks.Run(this._report, this);
                this.Finish();
                break;
            case "measure":
                NativeMeasurements.Run(this._report, this);
                this.Finish();
                break;
            case "frames":
                int frames = int.Parse(
                    arguments.GetValueOrDefault("frames", "3000"), System.Globalization.CultureInfo.InvariantCulture);
                this._frames = new FrameMeasurement(this, frames, 600);
                break;
            default:
                throw new ArgumentException("Unknown mode: " + this._mode);
            }
        } catch (Exception exception) {
            GD.PrintErr(exception.ToString());
            this.GetTree().Quit(2);
        }
    }

    public override void _Process(double delta) {
        if (this._frames is null) {
            return;
        }

        try {
            this._frames.Frame(delta);
            if (this._frames.IsComplete) {
                this._frames.Report(this._report, "Native/Frames/" + DisplayServer.GetName());
                this._frames = null;
                this.Finish();
            }
        } catch (Exception exception) {
            GD.PrintErr(exception.ToString());
            this._frames = null;
            this.GetTree().Quit(2);
        }
    }

    private static Dictionary<string, string> ParseArguments(string[] arguments) {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        foreach (string argument in arguments) {
            if (!argument.StartsWith("--", StringComparison.Ordinal)) {
                continue;
            }

            int separator = argument.IndexOf('=', StringComparison.Ordinal);
            if (separator < 0) {
                values[argument[2..]] = "true";
            } else {
                values[argument[2..separator]] = argument[(separator + 1)..];
            }
        }

        return values;
    }

    private void Finish() {
        this._report.Write(this._output, new {
            Mode = this._mode,
            Engine = Engine.GetVersionInfo()["string"].AsString(),
            DebugBuild = OS.IsDebugBuild(),
            DisplayServer = DisplayServer.GetName(),
            Framework = RuntimeInformation.FrameworkDescription,
            ServerCollection = GCSettings.IsServerGC,
            TieredCompilation = System.Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "default",
            TieredPGO = System.Environment.GetEnvironmentVariable("DOTNET_TieredPGO") ?? "default",
        });
        bool passed = this._report.TotalFailures == 0;
        GD.Print(passed
            ? $"NATIVE PASSED: {this._report.TotalCases} cases."
            : $"NATIVE FAILED: {this._report.TotalFailures} failures in {this._report.TotalCases} cases.");
        this.GetTree().Quit(passed ? 0 : 1);
    }
}
