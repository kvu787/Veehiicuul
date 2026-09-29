using System;
using System.Collections.Generic;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>A generated track and the vehicle footprint that indexes it, before any detector exists.</summary>
internal sealed record TrackDefinition(
    SyntheticTrack Track, RectangleLocalBounds IndexFootprint, double FootprintScale, string Purpose);

/// <summary>A generated track together with its detector.</summary>
internal sealed class TrackCase {
    public TrackCase(SyntheticTrack track, RectangleLocalBounds indexFootprint, double footprintScale, string purpose) {
        this.Track = track;
        this.IndexFootprint = indexFootprint;
        this.FootprintScale = footprintScale;
        this.Purpose = purpose;
        this.Detector = new TrackCollisionDetector(track.Collider, indexFootprint);
        this.Grid = ExpandedGridView.TryCreate(this.Detector);
    }

    public TrackCase(TrackDefinition definition)
        : this(definition.Track, definition.IndexFootprint, definition.FootprintScale, definition.Purpose) {
    }

    public SyntheticTrack Track { get; }
    public RectangleLocalBounds IndexFootprint { get; }

    /// <summary>Factor that maps the unit-scale footprints onto this track's scale.</summary>
    public double FootprintScale { get; }
    public string Purpose { get; }
    public TrackCollisionDetector Detector { get; }
    public ExpandedGridView? Grid { get; }
    public string Name => this.Track.Name;

    public string IndexKind => Describe(this.Detector);

    public static string Describe(TrackCollisionDetector detector) {
        if (detector.UsesExpandedGrid) {
            return "expanded grid";
        }

        string grid = detector.OrdinaryEdgeCount == 0
            ? "no grid"
            : detector.UsesDenseGrid ? "dense center grid" : "sparse center grid";
        string tree = detector.OutlierEdgeCount == 0
            ? "no long edges"
            : detector.OutlierBvhNodeCount > 0 ? "long-edge tree" : "long-edge list";
        return grid + " with " + tree;
    }

    public RectangleLocalBounds Scale(RectangleLocalBounds footprint) {
        float factor = (float)this.FootprintScale;
        return new RectangleLocalBounds(
            footprint.MinX * factor, footprint.MinY * factor, footprint.MaxX * factor, footprint.MaxY * factor);
    }
}

internal static class TrackCatalog {
    public const string Circuit = "Circuit";
    public const string CircuitFine = "CircuitFine";
    public const string CircuitCompact = "CircuitCompact";
    public const string CircuitWide = "CircuitWide";
    public const string CircuitVast = "CircuitVast";
    public const string CircuitFar = "CircuitFar";
    public const string CircuitMinute = "CircuitMinute";
    public const string CircuitGiant = "CircuitGiant";
    public const string Rectangular = "Rectangular";
    public const string RectangularSegmented = "RectangularSegmented";
    public const string Scattered = "Scattered";
    public const string ScatteredRemote = "ScatteredRemote";
    public const string ScatteredAligned = "ScatteredAligned";
    public const string Crowded = "Crowded";

    private static readonly Dictionary<string, Func<TrackDefinition>> Definitions = new(StringComparer.Ordinal) {
        [Circuit] = () => new TrackDefinition(
            SyntheticTrack.Circuit(Circuit, 20260929, 110.0, 9.0, 2.0, 2.0),
            Footprints.Reference, 1.0, "reference circuit, two-unit edges"),
        [CircuitFine] = () => new TrackDefinition(
            SyntheticTrack.Circuit(CircuitFine, 20260930, 110.0, 9.0, 0.3, 0.3),
            Footprints.Reference, 1.0, "same scale, finely tessellated"),
        [CircuitCompact] = () => new TrackDefinition(
            SyntheticTrack.Circuit(CircuitCompact, 20261001, 32.0, 6.0, 0.5, 0.5),
            Footprints.Reference, 1.0, "small circuit, short edges"),
        [CircuitWide] = () => new TrackDefinition(
            SyntheticTrack.Circuit(CircuitWide, 20261002, 200.0, 12.0, 2.0, 2.0),
            Footprints.Reference, 1.0, "extent beyond the expanded-grid cell limit"),
        [CircuitVast] = () => new TrackDefinition(
            SyntheticTrack.Circuit(CircuitVast, 20261003, 1200.0, 14.0, 2.5, 2.5),
            Footprints.Reference, 1.0, "very large circuit"),
        [CircuitFar] = () => new TrackDefinition(
            SyntheticTrack.Circuit(CircuitFar, 20260929, 110.0, 9.0, 2.0, 2.0, 1.0, 50000.0, -80000.0),
            Footprints.Reference, 1.0, "reference circuit far from the origin"),
        [CircuitMinute] = () => new TrackDefinition(
            SyntheticTrack.Circuit(CircuitMinute, 20260929, 110.0, 9.0, 2.0, 2.0, 1.0 / 1024.0),
            ScaleFootprint(Footprints.Reference, 1.0 / 1024.0), 1.0 / 1024.0, "reference circuit scaled by 1/1024"),
        [CircuitGiant] = () => new TrackDefinition(
            SyntheticTrack.Circuit(CircuitGiant, 20260929, 110.0, 9.0, 2.0, 2.0, 1024.0),
            ScaleFootprint(Footprints.Reference, 1024.0), 1024.0, "reference circuit scaled by 1024"),
        [Rectangular] = () => new TrackDefinition(
            SyntheticTrack.Rectangular(Rectangular, 200.0, 120.0, 10.0, 0.0),
            Footprints.Reference, 1.0, "eight long axis-aligned edges"),
        [RectangularSegmented] = () => new TrackDefinition(
            SyntheticTrack.Rectangular(RectangularSegmented, 200.0, 120.0, 10.0, 2.0),
            Footprints.Reference, 1.0, "axis-aligned edges in two-unit pieces"),
        [Scattered] = () => new TrackDefinition(
            SyntheticTrack.Scattered(Scattered, 20261004, 24, 24, 9.0, 0.3, 1.2, 5),
            Footprints.Reference, 1.0, "576 small obstacles"),
        [ScatteredRemote] = () => new TrackDefinition(
            SyntheticTrack.Scattered(ScatteredRemote, 20261005, 12, 12, 900.0, 0.2, 0.6, 3),
            Footprints.Reference, 1.0, "144 tiny obstacles spread over ten thousand units"),
        // Centers 4,096 cells apart in X and 65,536 cells apart in Y: cell coordinates that
        // differ only above their low bits must remain different cells.
        [ScatteredAligned] = () => new TrackDefinition(
            SyntheticTrack.FromOutlines(
                ScatteredAligned, Square(0f, 0f), Square(6144f, 0f), Square(0f, 98304f), Square(6144f, 98304f)),
            Footprints.Reference, 1.0, "four obstacles whose cells differ only in high bits"),
        [Crowded] = () => new TrackDefinition(
            SyntheticTrack.Scattered(Crowded, 20261006, 96, 96, 1.6, 0.25, 0.7, 6),
            Footprints.Reference, 1.0, "55,296 edges packed tightly"),
    };

    public static IReadOnlyCollection<string> Names => Definitions.Keys;

    /// <summary>Generates the track without running any detector code.</summary>
    public static TrackDefinition Define(string name) {
        return Definitions[name]();
    }

    public static TrackCase Build(string name) {
        return new TrackCase(Definitions[name]());
    }

    private static (float X, float Y)[] Square(float centerX, float centerY) {
        const float half = 0.4f;
        return [
            (centerX - half, centerY - half), (centerX + half, centerY - half),
            (centerX + half, centerY + half), (centerX - half, centerY + half),
        ];
    }

    public static RectangleLocalBounds ScaleFootprint(RectangleLocalBounds footprint, double factor) {
        return new RectangleLocalBounds(
            (float)(footprint.MinX * factor), (float)(footprint.MinY * factor),
            (float)(footprint.MaxX * factor), (float)(footprint.MaxY * factor));
    }
}
