using System;
using System.Collections.Generic;
using System.Text.Json;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>
/// Checks how construction and queries treat invalid and extreme inputs.
/// Invalid input must be refused with an ArgumentException (or a type derived
/// from it); extreme but valid input must be answered exactly.
/// </summary>
internal static class InputChecks {
    private static readonly RectangleLocalBounds Unit = new(-1f, -1f, 1f, 1f);

    public static void Run(ValidationContext context) {
        context.Run("Input: invalid construction is refused", InvalidConstruction);
        context.Run("Input: invalid queries are refused", InvalidQueries);
        context.Run("Input: extreme valid input is answered exactly", ExtremeValid);
        context.Run("Input: JSON text reaches the detector", JsonText);
    }

    private static Outline Loop(params (float X, float Y)[] points) {
        List<CoordinateXY> vertices = [];
        foreach ((float x, float y) in points) {
            vertices.Add(new CoordinateXY { X = x, Y = y });
        }

        return new Outline { Vertices = vertices };
    }

    private static ColliderJson Collider(params Outline[] outlines) {
        return new ColliderJson { Outlines = [.. outlines] };
    }

    private static void ExpectRefused(SuiteResult result, string name, Action action) {
        try {
            action();
            result.Check(false, name + ": accepted.");
        } catch (ArgumentException) {
            result.Check(true, name);
        } catch (Exception exception) {
            result.Check(false, $"{name}: refused with {exception.GetType().Name} instead of an ArgumentException.");
        }
    }

    private static void InvalidConstruction(SuiteResult result) {
        Outline triangle = Loop((0f, 0f), (4f, 0f), (0f, 4f));
        ExpectRefused(result, "null collider", () => _ = new TrackCollisionDetector(null!, Unit));
        ExpectRefused(result, "null outline list", () =>
            _ = new TrackCollisionDetector(new ColliderJson { Outlines = null! }, Unit));
        ExpectRefused(result, "no outlines", () => _ = new TrackCollisionDetector(new ColliderJson(), Unit));
        ExpectRefused(result, "null outline", () => _ = new TrackCollisionDetector(Collider(triangle, null!), Unit));
        ExpectRefused(result, "null vertex list", () =>
            _ = new TrackCollisionDetector(Collider(new Outline { Vertices = null! }), Unit));
        ExpectRefused(result, "outline without vertices", () =>
            _ = new TrackCollisionDetector(Collider(new Outline()), Unit));
        ExpectRefused(result, "outline with one vertex", () =>
            _ = new TrackCollisionDetector(Collider(Loop((0f, 0f))), Unit));
        ExpectRefused(result, "outline with two vertices", () =>
            _ = new TrackCollisionDetector(Collider(Loop((0f, 0f), (1f, 0f))), Unit));
        ExpectRefused(result, "valid outline followed by a short one", () =>
            _ = new TrackCollisionDetector(Collider(triangle, Loop((0f, 0f), (1f, 0f))), Unit));
        ExpectRefused(result, "repeated neighboring vertex", () =>
            _ = new TrackCollisionDetector(Collider(Loop((0f, 0f), (4f, 0f), (4f, 0f), (0f, 4f))), Unit));
        ExpectRefused(result, "last vertex repeats the first", () =>
            _ = new TrackCollisionDetector(Collider(Loop((0f, 0f), (4f, 0f), (0f, 4f), (0f, 0f))), Unit));
        ExpectRefused(result, "positive and negative zero are the same vertex", () =>
            _ = new TrackCollisionDetector(Collider(Loop((0f, 0f), (-0f, -0f), (4f, 0f), (0f, 4f))), Unit));
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }) {
            ExpectRefused(result, $"X coordinate {invalid}", () =>
                _ = new TrackCollisionDetector(Collider(Loop((0f, 0f), (invalid, 0f), (0f, 4f))), Unit));
            ExpectRefused(result, $"Y coordinate {invalid}", () =>
                _ = new TrackCollisionDetector(Collider(Loop((0f, 0f), (4f, invalid), (0f, 4f))), Unit));
            ExpectRefused(result, $"vertex constructor with {invalid}", () => _ = new CoordinateXY(invalid, 0f));
            ExpectRefused(result, $"bounds constructor with {invalid}", () =>
                _ = new RectangleLocalBounds(invalid, -1f, 1f, 1f));
            ExpectRefused(result, $"pose constructor with position {invalid}", () =>
                _ = new RectanglePose(invalid, 0f, 0f));
            ExpectRefused(result, $"pose constructor with yaw {invalid}", () =>
                _ = new RectanglePose(0f, 0f, invalid));
        }

        ExpectRefused(result, "default index footprint", () =>
            _ = new TrackCollisionDetector(Collider(triangle), default));
        ExpectRefused(result, "bounds with equal X limits", () => _ = new RectangleLocalBounds(1f, -1f, 1f, 1f));
        ExpectRefused(result, "bounds with reversed Y limits", () => _ = new RectangleLocalBounds(-1f, 1f, 1f, -1f));
        foreach (double scale in new[] { 0.0, -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity }) {
            ExpectRefused(result, $"cell size scale {scale}", () =>
                _ = new TrackCollisionDetector(Collider(triangle), Unit, scale));
        }
    }

    private static void InvalidQueries(SuiteResult result) {
        TrackCollisionDetector compact = new(Collider(Loop((0f, 0f), (4f, 0f), (0f, 4f))), Unit);
        TrackCollisionDetector spread = new(
            Collider(Loop((0f, 0f), (4f, 0f), (0f, 4f)), Loop((1e6f, 0f), (1e6f + 4f, 0f), (1e6f, 4f))), Unit);
        result.Check(compact.UsesExpandedGrid, "The compact fixture should use the expanded grid.");
        result.Check(!spread.UsesExpandedGrid, "The spread fixture should not use the expanded grid.");
        foreach ((string name, TrackCollisionDetector detector) in new[] { ("compact", compact), ("spread", spread) }) {
            ExpectRefused(result, name + ": default footprint", () =>
                detector.IsColliding(default, new RectanglePose(0f, 0f, 0f)));
            ExpectRefused(result, name + ": default footprint, linear", () =>
                detector.IsCollidingLinear(default, new RectanglePose(0f, 0f, 0f)));
            ExpectRefused(result, name + ": corner coordinates overflow", () =>
                detector.IsColliding(
                    new RectangleLocalBounds(-float.MaxValue, -1f, float.MaxValue, 1f),
                    new RectanglePose(float.MaxValue, 0f, 0f)));
            ExpectRefused(result, name + ": corner coordinates overflow after rotation", () =>
                detector.IsColliding(
                    new RectangleLocalBounds(-1f, -float.MaxValue, 1f, float.MaxValue),
                    new RectanglePose(float.MaxValue, 0f, MathF.PI / 2f)));
        }
    }

    private static void ExtremeValid(SuiteResult result) {
        float[] corners = new float[8];
        void Agree(string name, ColliderJson collider, RectangleLocalBounds index,
            RectangleLocalBounds footprint, RectanglePose pose, bool? expected) {
            try {
                TrackCollisionDetector detector = new(collider, index);
                OracleTrack oracle = new(collider);
                bool indexed = detector.IsColliding(footprint, pose);
                bool linear = detector.IsCollidingLinear(footprint, pose);
                DetectorInternals.Transform(footprint, pose, corners);
                bool exact = oracle.Intersects(corners, null, useBoundsFilter: false);
                result.Check(indexed == exact, $"{name}: indexed={indexed}, exact={exact}.");
                result.Check(linear == exact, $"{name}: linear={linear}, exact={exact}.");
                if (expected is bool known) {
                    result.Check(exact == known, $"{name}: expected {known}, exact={exact}.");
                }
            } catch (Exception exception) {
                result.Check(false, $"{name}: unexpected {exception.GetType().Name}: {exception.Message}");
            }
        }

        ColliderJson square = Collider(Loop((0f, 0f), (10f, 0f), (10f, 10f), (0f, 10f)));
        Agree("zero pose on a vertex", square, Unit, Unit, default, true);
        Agree("negative zero position", square, Unit, Unit, new RectanglePose(-0f, -0f, -0f), true);
        Agree("contained rectangle is clear", square, Unit, Unit, new RectanglePose(5f, 5f, 0.3f), false);
        Agree("containing rectangle is clear", square, Unit,
            new RectangleLocalBounds(-20f, -20f, 20f, 20f), new RectanglePose(5f, 5f, 0.3f), false);
        Agree("yaw of 1e30", square, Unit, Unit, new RectanglePose(10f, 5f, 1e30f), null);
        Agree("largest finite yaw", square, Unit, Unit, new RectanglePose(10f, 5f, float.MaxValue), null);
        Agree("smallest positive yaw", square, Unit, Unit, new RectanglePose(10f, 5f, float.Epsilon), true);
        Agree("footprint one step wide", square, Unit,
            new RectangleLocalBounds(0f, -1f, float.Epsilon, 1f), new RectanglePose(10f, 5f, 0f), true);
        Agree("position beyond one trillion", square, Unit, Unit, new RectanglePose(2e12f, 5f, 0f), false);
        Agree("position at the largest finite value", square, Unit,
            new RectangleLocalBounds(-1f, -1f, 1f, 1f), new RectanglePose(float.MaxValue, 0f, 0f), false);
        Agree("distant footprint origin returns to the track", square, Unit,
            new RectangleLocalBounds(1e6f, 1e6f, 1e6f + 2f, 1e6f + 2f),
            new RectanglePose(10f - 1e6f - 1f, 5f - 1e6f - 1f, 0f), true);

        float huge = float.MaxValue;
        ColliderJson enormous = Collider(Loop((-huge, -huge), (huge, -huge), (0f, huge)));
        Agree("outline at the largest finite coordinates, clear", enormous, Unit, Unit,
            new RectanglePose(0f, 0f, 0.5f), false);
        Agree("outline at the largest finite coordinates, contact", enormous, Unit, Unit,
            new RectanglePose(0f, -huge, 0f), true);

        float tiny = float.Epsilon * 64f;
        ColliderJson minute = Collider(Loop((0f, 0f), (tiny, 0f), (0f, tiny)));
        RectangleLocalBounds minuteFootprint = new(-tiny, -tiny, tiny, tiny);
        Agree("subnormal outline, subnormal footprint", minute, minuteFootprint, minuteFootprint,
            new RectanglePose(tiny, tiny, 0f), true);
        Agree("subnormal outline, unit footprint", minute, Unit, Unit, new RectanglePose(1f, 0f, 0f), true);
        Agree("subnormal outline, unit footprint, clear", minute, Unit, Unit, new RectanglePose(3f, 0f, 0f), false);

        ColliderJson mixed = Collider(
            Loop((0f, 0f), (tiny, 0f), (0f, tiny)),
            Loop((1e10f, 0f), (1e10f, 1e10f), (2e10f, 0f)),
            Loop((-1e30f, -1e30f), (-1e30f, -2e30f), (-2e30f, -1e30f)));
        Agree("outlines from subnormal to 1e30, near the origin", mixed, Unit, Unit,
            new RectanglePose(0f, 1f, 0f), true);
        Agree("outlines from subnormal to 1e30, at 1e10", mixed, Unit,
            new RectangleLocalBounds(-4096f, -4096f, 4096f, 4096f), new RectanglePose(1e10f, 5e9f, 0f), true);
        Agree("outlines from subnormal to 1e30, at 1e30", mixed, Unit,
            new RectangleLocalBounds(-1e29f, -1e29f, 1e29f, 1e29f), new RectanglePose(-1e30f, -1.5e30f, 0.25f), true);

        // Footprints much smaller and much larger than the barriers.
        ColliderJson ring = SyntheticTrack.Circuit("ring", 5, 60.0, 8.0, 1.0, 1.0).Collider;
        Agree("index footprint of one thousandth", ring, new RectangleLocalBounds(-0.001f, -0.001f, 0.001f, 0.001f),
            Footprints.Reference, new RectanglePose(68f, 0f, 0.2f), null);
        Agree("index footprint of one thousand", ring, new RectangleLocalBounds(-1000f, -1000f, 1000f, 1000f),
            Footprints.Reference, new RectanglePose(68f, 0f, 0.2f), null);
    }

    private static void JsonText(SuiteResult result) {
        // Match the application's JsonUtility options, including case-sensitive names.
        JsonSerializerOptions applicationOptions = new() { IncludeFields = true };
        const string wellFormed = """
            { "Outlines": [ { "Vertices": [
                { "X": 0, "Y": 0 }, { "X": 10.5, "Y": 0 }, { "X": 1e1, "Y": 10.25 }, { "X": -0.0, "Y": 1e1 } ] } ] }
            """;
        ColliderJson? collider = JsonSerializer.Deserialize<ColliderJson>(wellFormed, applicationOptions);
        result.Check(collider is not null && collider.Outlines.Count == 1, "Well-formed text yields one outline.");
        if (collider is not null && collider.Outlines.Count == 1) {
            List<CoordinateXY> vertices = collider.Outlines[0].Vertices;
            result.Check(vertices.Count == 4, "Well-formed text yields four vertices.");
            result.Check(
                vertices.Count == 4 && vertices[1].X == 10.5f && vertices[2].Y == 10.25f && vertices[2].X == 10f,
                "Vertex values survive deserialization.");
            TrackCollisionDetector detector = new(collider, Unit);
            result.Check(detector.EdgeCount == 4, "The detector indexes four edges.");
            result.Check(detector.IsColliding(Unit, new RectanglePose(10.5f, 0f, 0f)), "Contact at a parsed vertex.");
        }

        ColliderJson? extraKeys = JsonSerializer.Deserialize<ColliderJson>("""
            { "FormatVersion": 3, "Note": "ignored", "Outlines": [ { "Closed": true, "Vertices": [
                { "X": 0, "Y": 0, "Z": 5 }, { "X": 4, "Y": 0 }, { "X": 0, "Y": 4 } ] } ] }
            """, applicationOptions);
        result.Check(
            extraKeys is not null && extraKeys.Outlines.Count == 1 && extraKeys.Outlines[0].Vertices.Count == 3,
            "Unknown keys are ignored.");

        void ExpectMissingCoordinateRefused(string name, string json) {
            try {
                _ = JsonSerializer.Deserialize<ColliderJson>(json, applicationOptions);
                result.Check(false, name + ": deserialization accepted missing coordinates.");
            } catch (JsonException exception) {
                result.Check(
                    exception.Message.Contains("required properties", StringComparison.OrdinalIgnoreCase),
                    name + ": expected a missing-coordinate error, got " + exception.Message);
            }
        }

        ExpectMissingCoordinateRefused("lower-case vertex keys", """
            { "Outlines": [ { "Vertices": [ { "x": 0, "y": 0 }, { "x": 4, "y": 0 }, { "x": 0, "y": 4 } ] } ] }
            """);
        ExpectMissingCoordinateRefused("missing Y coordinate", """
            { "Outlines": [ { "Vertices": [ { "X": 0 }, { "X": 4, "Y": 0 }, { "X": 0, "Y": 4 } ] } ] }
            """);

        ColliderJson? missing = JsonSerializer.Deserialize<ColliderJson>("""{ "outlines": [] }""", applicationOptions);
        result.Check(missing is not null, "A misspelled outline key deserializes without an exception.");
        if (missing is not null) {
            ExpectRefused(result, "misspelled outline key leaves no outlines", () =>
                _ = new TrackCollisionDetector(missing, Unit));
        }

        string behavior;
        try {
            ColliderJson? overflow = JsonSerializer.Deserialize<ColliderJson>("""
                { "Outlines": [ { "Vertices": [ { "X": 1e39, "Y": 0 }, { "X": 4, "Y": 0 }, { "X": 0, "Y": 4 } ] } ] }
                """, applicationOptions);
            float parsed = overflow!.Outlines[0].Vertices[0].X;
            behavior = "deserialized as " + parsed.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            ExpectRefused(result, "value beyond the binary32 range", () =>
                _ = new TrackCollisionDetector(overflow, Unit));
        } catch (JsonException exception) {
            behavior = "refused by the deserializer: " + exception.Message;
            result.Check(true, "value beyond the binary32 range");
        }

        result.Fact("A JSON number of 1e39", behavior);
    }
}
