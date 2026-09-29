using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace CollisionDetectionFromScratch;

/// <summary>Collects check results and measurements and writes them as one JSON document.</summary>
internal sealed class NativeReport {
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly List<object> _suites = [];
    private readonly List<object> _measurements = [];
    private string _suite = string.Empty;
    private int _cases;
    private int _failures;
    private List<string> _samples = [];
    private SortedDictionary<string, string> _facts = new(StringComparer.Ordinal);

    public int TotalCases { get; private set; }
    public int TotalFailures { get; private set; }

    public void Begin(string suite) {
        this._suite = suite;
        this._cases = 0;
        this._failures = 0;
        this._samples = [];
        this._facts = new SortedDictionary<string, string>(StringComparer.Ordinal);
    }

    public void Check(bool condition, string description) {
        ++this._cases;
        if (!condition) {
            ++this._failures;
            if (this._samples.Count < 12) {
                this._samples.Add(description);
            }
        }
    }

    public void Near(double actual, double expected, double tolerance, string description) {
        this.Check(
            Math.Abs(actual - expected) <= tolerance,
            string.Create(
                CultureInfo.InvariantCulture,
                $"{description}: {actual:R}, expected {expected:R} within {tolerance:R}"));
    }

    /// <summary>Expects an exception of the given type or of a type derived from it.</summary>
    public void Throws<T>(Action action, string description) where T : Exception {
        try {
            action();
            this.Check(false, description + ": no exception.");
        } catch (T) {
            this.Check(true, description);
        } catch (Exception exception) {
            this.Check(false, $"{description}: {exception.GetType().Name} instead of {typeof(T).Name}.");
        }
    }

    /// <summary>Runs a group of checks; an exception that escapes it counts as one failed check.</summary>
    public void Guarded(string name, Action checks) {
        try {
            checks();
        } catch (Exception exception) {
            this.Check(false, $"{name} stopped with {exception.GetType().Name}: {exception.Message}");
        }
    }

    public void Fact(string key, string value) {
        this._facts[key] = value;
    }

    public void End() {
        this.TotalCases += this._cases;
        this.TotalFailures += this._failures;
        GD.Print($"{(this._failures == 0 ? "PASS" : "FAIL")} {this._suite}: cases={this._cases}, failures={this._failures}");
        foreach (KeyValuePair<string, string> fact in this._facts) {
            GD.Print($"     {fact.Key} = {fact.Value}");
        }

        foreach (string sample in this._samples) {
            GD.Print("     FAILURE " + sample);
        }

        this._suites.Add(new {
            Name = this._suite, Cases = this._cases, Failures = this._failures,
            Facts = this._facts, FailureSamples = this._samples,
        });
    }

    public void Measurement(string name, object value, string summary) {
        GD.Print($"MEASURE {name}: {summary}");
        this._measurements.Add(new { Name = name, Value = value });
    }

    public void Write(string path, object environment) {
        if (path.Length == 0) {
            return;
        }

        File.WriteAllText(path, JsonSerializer.Serialize(new {
            Environment = environment,
            Cases = this.TotalCases,
            Failures = this.TotalFailures,
            Suites = this._suites,
            Measurements = this._measurements,
        }, JsonOptions));
    }
}
