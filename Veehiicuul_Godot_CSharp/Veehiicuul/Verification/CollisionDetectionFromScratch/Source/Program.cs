using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace CollisionDetectionFromScratch;

internal static class Program {
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static int Main(string[] arguments) {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        try {
            Options options = new(arguments);
            Console.WriteLine(
                $"Environment: {RuntimeInformation.FrameworkDescription}; {RuntimeInformation.OSDescription}; "
                + $"{RuntimeInformation.ProcessArchitecture}; logical processors={Environment.ProcessorCount}");
            return options.Command switch {
                "validate" => Validate(options),
                "measure" => PerformanceCommands.Measure(options),
                "coldstart" => PerformanceCommands.ColdStart(options),
                "simulate" => SimulationCommands.Simulate(options),
                "placement" => ShowPlacement(options),
                _ => Usage(),
            };
        } catch (Exception exception) {
            Console.Error.WriteLine("ERROR: " + exception);
            return 2;
        }
    }

    private static int Usage() {
        Console.WriteLine("Commands: validate, measure, coldstart, simulate, placement. See Readme.md.");
        return 2;
    }

    /// <summary>Tells the launcher which logical processors an affinity request names.</summary>
    private static int ShowPlacement(Options options) {
        Placement placement = ProcessorTopology.Select(options.Text("affinity", "performance"));
        Console.WriteLine("PLACEMENT " + JsonSerializer.Serialize(new {
            placement.TimingProcessor, placement.Mask, placement.Description,
        }));
        return 0;
    }

    private static int Validate(Options options) {
        double workScale = options.Number("scale", 1.0);
        int threads = (int)options.Number("threads", Math.Max(1, Environment.ProcessorCount - 2));
        ValidationContext context = new(workScale, threads);
        Console.WriteLine($"Validation: work scale {workScale}, threads {threads}");
        HashSet<string> only = options.List("only");
        bool Wanted(string group) => only.Count == 0 || only.Contains(group);

        if (Wanted("oracle")) {
            OracleSelfChecks.Run(context);
        }

        if (Wanted("predicate")) {
            PredicateChecks.Run(context);
        }

        if (Wanted("transform")) {
            TransformChecks.Run(context);
        }

        if (Wanted("input")) {
            InputChecks.Run(context);
        }

        if (Wanted("contact")) {
            ContactChecks.Run(context);
        }

        if (Wanted("boundary")) {
            BoundaryChecks.Run(context);
        }

        List<TrackCase> tracks = [];
        if (Wanted("structure") || Wanted("differential") || Wanted("metamorphic") || Wanted("concurrency")) {
            foreach (string name in TrackCatalog.Names) {
                tracks.Add(TrackCatalog.Build(name));
            }
        }

        if (Wanted("structure")) {
            StructureChecks.Run(context, tracks);
        }

        if (Wanted("differential")) {
            DifferentialChecks.Run(context, tracks);
        }

        if (Wanted("metamorphic")) {
            MetamorphicChecks.Run(context, tracks);
        }

        if (Wanted("concurrency")) {
            ConcurrencyChecks.Run(context, tracks);
        }

        long cases = 0, failures = 0;
        foreach (SuiteResult result in context.Results) {
            cases += result.Cases;
            failures += result.Failures;
        }

        string output = options.Text("output", string.Empty);
        if (output.Length > 0) {
            File.WriteAllText(output, JsonSerializer.Serialize(new {
                Framework = RuntimeInformation.FrameworkDescription,
                OperatingSystem = RuntimeInformation.OSDescription,
                WorkScale = workScale,
                Threads = threads,
                Cases = cases,
                Failures = failures,
                Suites = context.Results.ConvertAll(result => new {
                    result.Name, result.Cases, result.Failures, result.Seconds, result.Facts, result.FailureSamples,
                }),
            }, JsonOptions));
        }

        Console.WriteLine(failures == 0
            ? $"VALIDATION PASSED: {context.Results.Count} suites, {cases} cases."
            : $"VALIDATION FAILED: {failures} failures in {cases} cases.");
        return failures == 0 ? 0 : 1;
    }
}

/// <summary>Arguments of the form "command --name=value --flag".</summary>
internal sealed class Options {
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public Options(string[] arguments) {
        this.Command = arguments.Length > 0 ? arguments[0] : string.Empty;
        for (int index = 1; index < arguments.Length; ++index) {
            string argument = arguments[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal)) {
                throw new ArgumentException("Unrecognized argument: " + argument);
            }

            int separator = argument.IndexOf('=');
            if (separator < 0) {
                this._values[argument[2..]] = "true";
            } else {
                this._values[argument[2..separator]] = argument[(separator + 1)..];
            }
        }
    }

    public string Command { get; }

    public string Text(string name, string fallback) {
        return this._values.TryGetValue(name, out string? value) ? value : fallback;
    }

    public double Number(string name, double fallback) {
        return this._values.TryGetValue(name, out string? value)
            ? double.Parse(value, CultureInfo.InvariantCulture)
            : fallback;
    }

    public bool Flag(string name) {
        return this._values.ContainsKey(name);
    }

    public HashSet<string> List(string name) {
        HashSet<string> items = new(StringComparer.Ordinal);
        foreach (string item in this.Text(name, string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries)) {
            items.Add(item.Trim());
        }

        return items;
    }
}
