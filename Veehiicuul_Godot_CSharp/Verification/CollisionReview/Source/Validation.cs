using System;
using System.Collections.Generic;
using System.Reflection;
using Veehiicuul_Godot_CSharp;

namespace CollisionReview;

internal sealed class Validation {
    private static readonly RectangleLocalBounds StandardBounds = new(-0.5f, -1, 0.5f, 1);
    private readonly Random _random = new(20261002);
    private long _checks;
    private long _indexedComparisons;
    private long _exactComparisons;
    private int _exactDisagreements;
    private readonly List<object> _cases = [];
    private readonly List<object> _observations = [];

    internal object Run(ColliderJson realTrack, Vehicle[] vehicles) {
        this.InvalidInputs();
        this.ContactContracts();
        RectangleLocalBounds offset = new(10000, 10000, 10001, 10002);
        List<Outline> shortOutlines = [];
        for (int y = 0; y < 10; ++y) {
            for (int x = 0; x < 10; ++x) { shortOutlines.Add(Box(x, y, x + 0.2f, y + 0.2f)); }
        }
        ColliderJson compact = new() { Outlines = shortOutlines.ToArray() };
        this.CompareCase("Expanded", compact, StandardBounds, StandardBounds, 0.5, "Expanded", 6000);
        this.CompareCase("Expanded recentered", compact, StandardBounds, new RectangleLocalBounds(5, 8, 5.5f, 9), 0.5, "Expanded", 6000);
        this.CompareCase("Expanded oversized rectangle", compact, StandardBounds, new RectangleLocalBounds(-12, -13, 15, 16), 0.5, "Expanded", 3000);
        this.CompareCase("Dense center", compact, offset, StandardBounds, 0.5, "Dense", 6000);
        shortOutlines.Add(Box(-15, -15, 25, 25));
        ColliderJson mixed = new() { Outlines = shortOutlines.ToArray() };
        this.CompareCase("Dense plus four outliers", mixed, offset, StandardBounds, 0.5, "Dense", 6000);
        this.CompareCase("Broad center query", mixed, offset, new RectangleLocalBounds(-18, -19, 18, 19), 0.5, "Dense", 3000);
        ColliderJson sparse = new() { Outlines = [Box(0, 0, 0.2f, 0.2f), Box(10000, 10000, 10000.2f, 10000.2f)] };
        this.CompareCase("Sparse center", sparse, StandardBounds, StandardBounds, 0.5, "Sparse", 6000);
        List<Outline> longOutlines = [];
        for (int index = 0; index < 64; ++index) { longOutlines.Add(Box(index * 100, -5, index * 100 + 30, 5)); }
        this.CompareCase("Outlier BVH", new ColliderJson { Outlines = longOutlines.ToArray() }, StandardBounds, StandardBounds, 0.5, "BVH", 6000);
        this.CompareCase("Coincident BVH centers", new ColliderJson { Outlines = [.. Repeat(Box(-10, -10, 10, 10), 32)] }, offset, StandardBounds, 0.5, "BVH", 3000);
        ColliderJson hugeCellSpan = new() { Outlines = [Box(0, 0, 0.2f, 1e-13f), Box(10000, 0, 10000.2f, 1e-13f), Box(20000, 0, 20000.2f, 1e-13f)] };
        this.CompareCase("Unrepresentable center grid", hugeCellSpan, StandardBounds, StandardBounds, 1e-12, "BVH", 3000);
        float tiny = MathF.ScaleB(1, -140);
        this.CompareCase("Subnormal geometry", new ColliderJson { Outlines = [Box(-4 * tiny, -4 * tiny, 4 * tiny, 4 * tiny)] },
            new RectangleLocalBounds(-tiny, -tiny, tiny, tiny), new RectangleLocalBounds(-tiny, -tiny, tiny, tiny), 0.5, "Expanded", 3000);
        float enormous = 1e20f, step = MathF.BitIncrement(enormous) - enormous;
        this.CompareCase("Huge finite coordinates", new ColliderJson { Outlines = [Box(enormous, enormous, enormous + step * 8, enormous + step * 8)] },
            StandardBounds, new RectangleLocalBounds(-step, -step, step, step), 0.5, "LinearOutliers", 3000);
        RectangleLocalBounds representative = Union(vehicles);
        foreach (Vehicle vehicle in vehicles) {
            this.CompareCase("Ribeye " + vehicle.Name, realTrack, representative, vehicle.Bounds, 0.5, "Expanded", 10000);
        }
        this.CellBoundaries(realTrack, representative, vehicles[3].Bounds);
        this.PredicateStress();
        return new {
            Seed = 20261002, Checks = this._checks, IndexedLinearComparisons = this._indexedComparisons,
            ExactOracleComparisons = this._exactComparisons, ExactOracleDisagreements = this._exactDisagreements,
            Cases = this._cases, Observations = this._observations
        };
    }

    private void InvalidInputs() {
        ColliderJson valid = new() { Outlines = [Box(-5, -5, 5, 5)] };
        this.Throws<ArgumentNullException>(() => _ = new TrackCollisionDetector(null!, StandardBounds));
        this.Throws<ArgumentException>(() => _ = new TrackCollisionDetector(valid, default));
        this.Throws<ArgumentException>(() => _ = new TrackCollisionDetector(new ColliderJson { Outlines = null! }, StandardBounds));
        this.Throws<ArgumentException>(() => _ = new TrackCollisionDetector(new ColliderJson { Outlines = [] }, StandardBounds));
        this.Throws<ArgumentException>(() => _ = new TrackCollisionDetector(new ColliderJson { Outlines = [null!] }, StandardBounds));
        this.Throws<ArgumentException>(() => _ = new TrackCollisionDetector(new ColliderJson { Outlines = [new Outline { Vertices = null! }] }, StandardBounds));
        this.Throws<ArgumentException>(() => _ = new TrackCollisionDetector(new ColliderJson { Outlines = [new Outline { Vertices = [Coordinate(0, 0), Coordinate(1, 1)] }] }, StandardBounds));
        this.Throws<ArgumentException>(() => _ = new TrackCollisionDetector(new ColliderJson { Outlines = [new Outline { Vertices = [Coordinate(0, 0), Coordinate(0, 0), Coordinate(1, 1)] }] }, StandardBounds));
        this.Throws<ArgumentException>(() => _ = new TrackCollisionDetector(new ColliderJson { Outlines = [new Outline { Vertices = [Coordinate(0, 0), Coordinate(1, 1), Coordinate(0, 0)] }] }, StandardBounds));
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }) {
            this.Throws<ArgumentOutOfRangeException>(() => _ = new RectangleLocalBounds(invalid, -1, 1, 1));
            this.Throws<ArgumentOutOfRangeException>(() => _ = new RectanglePose(invalid, 0, 0));
            this.Throws<ArgumentOutOfRangeException>(() => _ = new RectanglePose(0, invalid, 0));
            this.Throws<ArgumentOutOfRangeException>(() => _ = new RectanglePose(0, 0, invalid));
            this.Throws<ArgumentException>(() => _ = new TrackCollisionDetector(new ColliderJson { Outlines = [new Outline { Vertices = [Coordinate(invalid, 0), Coordinate(1, 1), Coordinate(0, 1)] }] }, StandardBounds));
        }
        this.Throws<ArgumentException>(() => _ = new RectangleLocalBounds(1, -1, -1, 1));
        this.Throws<ArgumentException>(() => _ = new RectangleLocalBounds(-1, 1, 1, 1));
        foreach (double invalid in new[] { 0, -1, double.NaN, double.PositiveInfinity }) {
            this.Throws<ArgumentException>(() => _ = new TrackCollisionDetector(valid, StandardBounds, invalid));
        }
        TrackCollisionDetector detector = new(valid, StandardBounds);
        this.Throws<ArgumentException>(() => detector.IsColliding(default, default));
        this.Throws<ArgumentOutOfRangeException>(() => detector.IsColliding(new RectangleLocalBounds(-float.MaxValue, -1, float.MaxValue, 1), new RectanglePose(float.MaxValue, 0, 0)));
    }

    private void ContactContracts() {
        ColliderJson data = new() { Outlines = [Box(-5, -5, 5, 5)] };
        TrackCollisionDetector detector = new(data, StandardBounds);
        this.Check(!detector.IsColliding(StandardBounds, default), "Vehicle wholly inside outer outline");
        this.Check(detector.IsColliding(StandardBounds, new RectanglePose(4.5f, 0, 0)), "Closed edge contact");
        this.Check(!detector.IsColliding(StandardBounds, new RectanglePose(MathF.BitDecrement(4.5f), 0, 0)), "One ULP gap");
        this.Check(detector.IsColliding(StandardBounds, new RectanglePose(MathF.BitIncrement(4.5f), 0, 0)), "One ULP penetration");
        this.Check(detector.IsColliding(StandardBounds, new RectanglePose(4.5f, 4, 0)), "Corner and collinear contact");
        this.Check(!detector.IsColliding(StandardBounds, new RectanglePose(20, 20, 0)), "Vehicle outside all outlines");
        this.Check(!detector.IsColliding(new RectangleLocalBounds(-10, -10, 10, 10), default), "Outline wholly inside vehicle");
        bool original = detector.IsColliding(StandardBounds, new RectanglePose(4.5f, 0, 0));
        data.Outlines[0].Vertices[0] = Coordinate(-100, -100);
        data.Outlines = [];
        this.Check(detector.IsColliding(StandardBounds, new RectanglePose(4.5f, 0, 0)) == original, "Immutable construction snapshot");
        TrackCollisionDetector wall = new(new ColliderJson { Outlines = [Box(0, -10, 0.1f, 10)] }, StandardBounds);
        this.Check(!wall.IsColliding(StandardBounds, new RectanglePose(-2, 0, 0)) && !wall.IsColliding(StandardBounds, new RectanglePose(2, 0, 0))
            && wall.IsColliding(StandardBounds, default), "Documented tunneling across sampled poses");
        float epsilon = MathF.ScaleB(1, -60);
        ColliderJson difficult = new() { Outlines = [new Outline { Vertices = [Coordinate(-1, -1), Coordinate(1, 1), Coordinate(1, 2)] }] };
        RectangleLocalBounds narrow = new(epsilon, -1, 2 * epsilon, 0);
        bool approximate = new TrackCollisionDetector(difficult, StandardBounds).IsColliding(narrow, default);
        bool exact = new IndependentGeometry(difficult).IsColliding(narrow, default);
        this.Check(approximate && !exact, "Known subvisual approximate false positive reproduced");
        this._observations.Add(new { Scenario = "Accepted approximate predicate", Approximate = approximate, Exact = exact, Gap = epsilon });
    }

    private void CompareCase(string name, ColliderJson data, RectangleLocalBounds representative,
        RectangleLocalBounds bounds, double scale, string expectedMode, int count) {
        TrackCollisionDetector detector = new(data, representative, scale);
        string mode = Mode(detector);
        this.Check(mode == expectedMode, $"Expected {expectedMode} for {name}, got {mode}");
        IndependentGeometry reference = new(data);
        int hits = 0, exactDisagreements = 0;
        for (int index = 0; index < count; ++index) {
            Outline outline = data.Outlines[this._random.Next(data.Outlines.Length)];
            int edge = this._random.Next(outline.Vertices.Length);
            CoordinateXY first = outline.Vertices[edge], second = outline.Vertices[(edge + 1) % outline.Vertices.Length];
            double interpolation = this._random.NextDouble();
            double radius = Math.Max((double)bounds.MaxX - bounds.MinX, (double)bounds.MaxY - bounds.MinY);
            float x = (float)(first.X + ((double)second.X - first.X) * interpolation + (this._random.NextDouble() - 0.5) * radius * 5);
            float y = (float)(first.Y + ((double)second.Y - first.Y) * interpolation + (this._random.NextDouble() - 0.5) * radius * 5);
            RectanglePose pose = new(x, y, (float)((this._random.NextDouble() - 0.5) * Math.Tau));
            bool indexed = detector.IsColliding(bounds, pose), linear = detector.IsCollidingLinear(bounds, pose);
            this.Check(indexed == linear, $"Index coverage: {name}/{index} at {x:R},{y:R}");
            ++this._indexedComparisons;
            bool exact = reference.IsColliding(bounds, pose);
            ++this._exactComparisons;
            if (indexed != exact) {
                ++exactDisagreements; ++this._exactDisagreements;
                if (this._observations.Count < 12) { this._observations.Add(new { Case = name, Index = index, Bounds = bounds, Pose = pose, Actual = indexed, Exact = exact }); }
            }
            if (indexed) { ++hits; }
        }
        this._cases.Add(new {
            Name = name, Queries = count, Hits = hits, ExactDisagreements = exactDisagreements, Mode = mode,
            detector.EdgeCount, detector.CellSize, detector.GridCellCount, detector.OccupiedCellCount, detector.StoredGridEdgeReferenceCount,
            detector.OutlierEdgeCount, detector.OutlierBvhNodeCount
        });
        Console.WriteLine($"Validated {name}: {count} indexed/direct/exact queries; exact differences={exactDisagreements}.");
    }

    private void CellBoundaries(ColliderJson data, RectangleLocalBounds representative, RectangleLocalBounds bounds) {
        TrackCollisionDetector detector = new(data, representative);
        object grid = typeof(TrackCollisionDetector).GetField("_expandedGrid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(detector)!;
        double originX = (double)grid.GetType().GetField("_originX", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(grid)!;
        double originY = (double)grid.GetType().GetField("_originY", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(grid)!;
        for (int index = 0; index < 10000; ++index) {
            float x = (float)(originX + this._random.Next(detector.GridColumnCount + 1) * detector.CellSize);
            float y = (float)(originY + this._random.Next(detector.GridRowCount + 1) * detector.CellSize);
            x = index % 3 == 0 ? MathF.BitDecrement(x) : index % 3 == 1 ? MathF.BitIncrement(x) : x;
            y = index % 3 == 0 ? MathF.BitIncrement(y) : index % 3 == 1 ? MathF.BitDecrement(y) : y;
            RectanglePose pose = new(x, y, (float)(this._random.NextDouble() * Math.Tau));
            this.Check(detector.IsColliding(bounds, pose) == detector.IsCollidingLinear(bounds, pose), "Expanded grid boundary coverage");
            ++this._indexedComparisons;
        }
    }

    private void PredicateStress() {
        Type pointType = typeof(TrackCollisionDetector).GetNestedType("PointF", BindingFlags.NonPublic)!;
        ConstructorInfo constructor = pointType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(float), typeof(float)], null)!;
        Type predicates = typeof(TrackCollisionDetector).GetNestedType("CollisionPredicates", BindingFlags.NonPublic)!;
        MethodInfo method = predicates.GetMethod("SegmentsIntersect", BindingFlags.Static | BindingFlags.NonPublic)!;
        int moderateDifferences = 0, extremeDifferences = 0, extremeReversalDifferences = 0;
        for (int index = 0; index < 20000; ++index) {
            bool extreme = index >= 10000;
            Point a = RandomPoint(), b = RandomPoint(), c = RandomPoint(), d = RandomPoint();
            bool exact = IndependentGeometry.Intersects(a, b, c, d);
            bool actual = (bool)method.Invoke(null, [BoxPoint(a), BoxPoint(b), BoxPoint(c), BoxPoint(d)])!;
            if (actual != exact) { if (extreme) { ++extremeDifferences; } else { ++moderateDifferences; } }
            if (!extreme) { this.Check(actual == exact, "Moderate-coordinate exact segment predicate"); }
            bool reversed = (bool)method.Invoke(null, [BoxPoint(b), BoxPoint(a), BoxPoint(d), BoxPoint(c)])!;
            if (!extreme) { this.Check(reversed == actual, "Moderate segment endpoint reversal"); } else if (reversed != actual) { ++extremeReversalDifferences; }
            Point RandomPoint() {
                float x = (float)(this._random.NextDouble() * 2 - 1), y = (float)(this._random.NextDouble() * 2 - 1);
                return extreme ? new Point(MathF.ScaleB(x, this._random.Next(-140, 125)), MathF.ScaleB(y, this._random.Next(-140, 125))) : new Point(x * 1000, y * 1000);
            }
        }
        this._observations.Add(new { Scenario = "Exact segment stress", ModerateCases = 10000, ModerateDifferences = moderateDifferences, ExtremeCases = 10000, ExtremeDifferences = extremeDifferences, ExtremeReversalDifferences = extremeReversalDifferences });
        object BoxPoint(Point point) { return constructor.Invoke([point.X, point.Y]); }
    }

    private void Throws<T>(Action action) where T : Exception {
        try { action(); } catch (T) { ++this._checks; return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private void Check(bool condition, string message) {
        ++this._checks;
        if (!condition) { throw new InvalidOperationException(message); }
    }

    internal static RectangleLocalBounds Union(Vehicle[] vehicles) {
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity, maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        foreach (Vehicle vehicle in vehicles) {
            minX = Math.Min(minX, vehicle.Bounds.MinX); minY = Math.Min(minY, vehicle.Bounds.MinY);
            maxX = Math.Max(maxX, vehicle.Bounds.MaxX); maxY = Math.Max(maxY, vehicle.Bounds.MaxY);
        }
        return new RectangleLocalBounds(minX, minY, maxX, maxY);
    }

    internal static string Mode(TrackCollisionDetector detector) {
        if (detector.UsesExpandedGrid) { return "Expanded"; }
        if (detector.OrdinaryEdgeCount != 0) { return detector.UsesDenseGrid ? "Dense" : "Sparse"; }
        return detector.OutlierBvhNodeCount == 0 ? "LinearOutliers" : "BVH";
    }

    internal static Outline Box(float minX, float minY, float maxX, float maxY) {
        return new Outline { Vertices = [Coordinate(minX, minY), Coordinate(maxX, minY), Coordinate(maxX, maxY), Coordinate(minX, maxY)] };
    }

    private static CoordinateXY Coordinate(float x, float y) { return new CoordinateXY { X = x, Y = y }; }
    private static IEnumerable<Outline> Repeat(Outline value, int count) {
        for (int index = 0; index < count; ++index) { yield return value; }
    }
}

internal sealed class Vehicle {
    public required string Name { get; init; }
    public required RectangleLocalBounds Bounds { get; init; }
}
