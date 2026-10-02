using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Veehiicuul_Godot_CSharp;

namespace CollisionReviewEngine;

/// <summary>Runs verification in an isolated export without changing application callbacks.</summary>
public partial class EngineReview : Node {
    private static readonly JsonSerializerOptions OutputOptions = new() { WriteIndented = true };
    private readonly List<string> _checks = [];
    private readonly List<object> _observations = [];

    public override void _EnterTree() {
        Callable.From(this.Execute).CallDeferred();
    }

    private void Execute() {
        try {
            this.Run();
            this.GetTree().Quit(0);
        } catch (Exception exception) {
            GD.PrintErr(exception.ToString());
            this.GetTree().Quit(1);
        }
    }

    private void Run() {
        string output = System.Environment.GetEnvironmentVariable("COLLISION_REVIEW_OUTPUT")
            ?? throw new InvalidOperationException("Launch through Run.cmd.");
        Stopwatch initialization = Stopwatch.StartNew();
        Veehiicuul_Godot_CSharp.Main application = new(this);
        application.Ready();
        initialization.Stop();
        CollisionManager manager = Field<CollisionManager>(application, "CollisionManager");
        CarSwitcher switcher = Field<CarSwitcher>(application, "CarSwitcher");
        CarStateManager state = Field<CarStateManager>(application, "CarStateManager");
        RectangleLocalBounds[] bounds = Field<RectangleLocalBounds[]>(manager, "_vehicleBounds");
        TrackCollisionDetector detector = Field<TrackCollisionDetector>(manager, "_detector");
        long firstStart = Stopwatch.GetTimestamp();
        bool spawnCollision = manager.IsCarColliding(state.Position, state.Rotation);
        double firstQueryMicroseconds = Stopwatch.GetElapsedTime(firstStart).TotalMicroseconds;
        this.Check(!spawnCollision, "Actual spawn is clear");
        List<object> vehicles = [];
        Random random = new(20261002);
        for (int index = 0; index < bounds.Length; ++index) {
            Node3D vehicle = switcher.AvailableCars[index].Node!;
            VehicleCollisionFootprint footprint = VehicleCollisionFootprint.FromMeshGeometry(vehicle);
            RectangleLocalBounds raw = footprint.GetScaledLocalBounds(vehicle);
            this.Check(Math.Abs(bounds[index].MinY - (raw.MinY + 0.165f)) < 1e-6f,
                $"Front shortening for {vehicle.Name}");
            Vector3 oldRotation = vehicle.Rotation;
            for (int turn = 0; turn < 32; ++turn) {
                float yaw = (float)((random.NextDouble() - 0.5) * 2.0 * Math.PI);
                vehicle.Rotation = new Vector3(0, yaw, 0);
                RectangleLocalBounds rotated = footprint.GetScaledLocalBounds(vehicle);
                this.Check(MaximumDifference(raw, rotated) < 2e-6f, $"Yaw preserves local footprint {index}/{turn}");
                float xScale = vehicle.GlobalBasis.X.Length();
                float zScale = vehicle.GlobalBasis.Z.Length();
                Vector3 native = vehicle.GlobalTransform * new Vector3(raw.MaxX / xScale, 0, -raw.MinY / zScale);
                double x = (double)vehicle.Position.X + raw.MaxX * Math.Cos(-yaw) + raw.MinY * Math.Sin(-yaw);
                double y = -(double)vehicle.Position.Z - raw.MaxX * Math.Sin(-yaw) + raw.MinY * Math.Cos(-yaw);
                this.Check(Math.Abs(native.X - x) < 1e-5 && Math.Abs(-native.Z - y) < 1e-5,
                    $"Godot and collision yaw agree {index}/{turn}");
            }
            vehicle.Rotation = oldRotation;
            vehicles.Add(new { Name = vehicle.Name.ToString(), Bounds = bounds[index], MeshChildCount = vehicle.GetChildCount() });
            typeof(CarSwitcher).GetProperty(nameof(CarSwitcher.CurrentCarIndex))!.SetValue(switcher, index);
            for (int query = 0; query < 3000; ++query) {
                Vector3 position = new((float)(random.NextDouble() * 100 - 50), 0, (float)(random.NextDouble() * 100 - 50));
                float yaw = (float)(random.NextDouble() * Math.Tau - Math.PI);
                bool expected = detector.IsCollidingLinear(bounds[index], new RectanglePose(position.X, -position.Z, -yaw));
                this.Check(manager.IsCarColliding(position, yaw) == expected, $"Manager matches direct scan {index}/{query}");
                this.Check(manager.IsCarColliding(position, yaw) == expected, $"Cached manager matches {index}/{query}");
            }
        }
        this.TestCarSwitchCache(manager, switcher, bounds, detector);
        typeof(CarSwitcher).GetProperty(nameof(CarSwitcher.CurrentCarIndex))!.SetValue(switcher, 3);
        this.TestSyntheticFootprints();
        List<object> benchmarks = [];
        if (System.Environment.GetEnvironmentVariable("COLLISION_REVIEW_SKIP_BENCHMARKS") != "1") {
            object cachedTiming = Measure("Cached manager", 2_000_000, () => {
                int hits = 0;
                for (int index = 0; index < 2_000_000; ++index) { if (manager.IsCarColliding(state.Position, state.Rotation)) { ++hits; } }
                return hits;
            });
            Vector3[] positions = new Vector3[4096];
            float[] rotations = new float[positions.Length];
            for (int index = 0; index < positions.Length; ++index) {
                positions[index] = new Vector3((float)(random.NextDouble() * 100 - 50), 0, (float)(random.NextDouble() * 100 - 50));
                rotations[index] = (float)(random.NextDouble() * Math.Tau - Math.PI);
            }
            object movingTiming = Measure("Moving manager uniform", positions.Length * 100, () => {
                int hits = 0;
                for (int repeat = 0; repeat < 100; ++repeat) {
                    for (int index = 0; index < positions.Length; ++index) { if (manager.IsCarColliding(positions[index], rotations[index])) { ++hits; } }
                }
                return hits;
            });
            benchmarks.Add(cachedTiming);
            benchmarks.Add(movingTiming);
        }
        object result = new {
            GodotVersion = Engine.GetVersionInfo()["string"].AsString(),
            TieredCompilation = System.Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "Default",
            ApplicationInitializationMilliseconds = initialization.Elapsed.TotalMilliseconds,
            FirstManagerQueryMicroseconds = firstQueryMicroseconds,
            CheckCount = this._checks.Count,
            Vehicles = vehicles,
            Index = new { detector.EdgeCount, detector.CellSize, detector.UsesExpandedGrid, detector.GridCellCount, detector.OccupiedCellCount, detector.StoredGridEdgeReferenceCount },
            Observations = this._observations,
            Benchmarks = benchmarks
        };
        File.WriteAllText(Path.Combine(output, "EngineResults.json"), JsonSerializer.Serialize(result, OutputOptions) + "\n");
        GD.Print($"Engine verification passed: {this._checks.Count} checks, {vehicles.Count} real cars.");
    }

    private void TestCarSwitchCache(CollisionManager manager, CarSwitcher switcher, RectangleLocalBounds[] bounds, TrackCollisionDetector detector) {
        CoordinateXY point = JsonUtility.Deserialize<ColliderJson>("res://Tracks/Ribeye/Ribeye_ColliderData.json").Outlines[0].Vertices[0];
        RectangleLocalBounds original = bounds[1];
        Vector3 position = new(500, 0, -500);
        RectanglePose pose = new(500, 500, 0);
        try {
            typeof(CarSwitcher).GetProperty(nameof(CarSwitcher.CurrentCarIndex))!.SetValue(switcher, 0);
            this.Check(!manager.IsCarColliding(position, 0), "Cache starts with a clear far-away pose");
            // Deliberately make this car different: the real six footprints are practically identical.
            bounds[1] = new RectangleLocalBounds(point.X - 501, point.Y - 501, point.X - 499, point.Y - 499);
            this.Check(detector.IsCollidingLinear(bounds[1], pose), "Different car has a verified contact at the same pose");
            typeof(CarSwitcher).GetProperty(nameof(CarSwitcher.CurrentCarIndex))!.SetValue(switcher, 1);
            this.Check(manager.IsCarColliding(position, 0), "Car index invalidates the previous clear cached result");
        } finally { bounds[1] = original; }
    }

    private void TestSyntheticFootprints() {
        Node3D root = new() { Name = "SyntheticVehicle" };
        this.AddChild(root);
        Node3D nested = new() { Position = new Vector3(3, 0, -2), Rotation = new Vector3(0, Mathf.Pi / 2, 0) };
        root.AddChild(nested);
        MeshInstance3D mesh = new() { Mesh = new BoxMesh { Size = new Vector3(2, 1, 4) }, Visible = false };
        nested.AddChild(mesh);
        VehicleCollisionFootprint footprint = VehicleCollisionFootprint.FromMeshGeometry(root);
        this.Check(Math.Abs(footprint.MinX - 1) < 1e-5 && Math.Abs(footprint.MaxX - 5) < 1e-5
            && Math.Abs(footprint.MinZ + 3) < 1e-5 && Math.Abs(footprint.MaxZ + 1) < 1e-5, "Hidden nested rotated mesh bounds");
        mesh.CustomAabb = new Aabb(new Vector3(-1, -1, -1), new Vector3(2, 2, 2));
        footprint = VehicleCollisionFootprint.FromMeshGeometry(root);
        this.Check(Math.Abs(footprint.MinX - 2) < 1e-5 && Math.Abs(footprint.MaxX - 4) < 1e-5, "Custom AABB overrides mesh AABB");
        root.Scale = new Vector3(2, 1, 3);
        RectangleLocalBounds scaled = footprint.GetScaledLocalBounds(root);
        this.Check(Math.Abs(scaled.MinX - 4) < 1e-5 && Math.Abs(scaled.MaxX - 8) < 1e-5
            && Math.Abs(scaled.MinY - 3) < 1e-5 && Math.Abs(scaled.MaxY - 9) < 1e-5, "Positive nonuniform planar scale");
        root.Scale = new Vector3(-1, 1, 1);
        RectangleLocalBounds reflected = footprint.GetScaledLocalBounds(root);
        this._observations.Add(new {
            Scenario = "Negative root X scale is accepted but reflection is lost",
            Actual = reflected,
            ExpectedMinX = -4f,
            ExpectedMaxX = -2f
        });
        root.Scale = Vector3.One;
        nested.Rotation = Vector3.Zero;
        mesh.TopLevel = true;
        mesh.GlobalPosition = new Vector3(20, 0, 0);
        footprint = VehicleCollisionFootprint.FromMeshGeometry(root);
        this._observations.Add(new {
            Scenario = "TopLevel child incorrectly inherits its parent's transform",
            ActualMinX = footprint.MinX,
            ActualMaxX = footprint.MaxX,
            ExpectedMinX = 19f,
            ExpectedMaxX = 21f
        });
        mesh.TopLevel = false;
        mesh.Free();
        Node bridge = new();
        nested.AddChild(bridge);
        MeshInstance3D separated = new() { Mesh = new BoxMesh { Size = Vector3.One * 2 }, Position = new Vector3(20, 0, 0) };
        bridge.AddChild(separated);
        footprint = VehicleCollisionFootprint.FromMeshGeometry(root);
        this._observations.Add(new {
            Scenario = "Plain Node breaks Godot transform inheritance but footprint traversal retains it",
            ActualMinX = footprint.MinX,
            ActualMaxX = footprint.MaxX,
            NativeMinimumX = (separated.GlobalTransform * separated.GetAabb().GetEndpoint(0)).X,
            NativeMaximumX = (separated.GlobalTransform * separated.GetAabb().GetEndpoint(7)).X
        });
        root.Free();
        Node3D empty = new();
        try {
            _ = VehicleCollisionFootprint.FromMeshGeometry(empty);
            throw new InvalidOperationException("An empty vehicle must be rejected.");
        } catch (InvalidOperationException exception) when (exception.Message.Contains("no mesh geometry", StringComparison.Ordinal)) {
            this.Check(true, "Empty mesh footprint rejected");
        } finally { empty.Free(); }
    }

    private void Check(bool condition, string label) {
        if (!condition) { throw new InvalidOperationException(label); }
        this._checks.Add(label);
    }

    private static T Field<T>(object owner, string name) {
        return (T)(owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!);
    }

    private static float MaximumDifference(RectangleLocalBounds first, RectangleLocalBounds second) {
        return Math.Max(Math.Max(Math.Abs(first.MinX - second.MinX), Math.Abs(first.MaxX - second.MaxX)),
            Math.Max(Math.Abs(first.MinY - second.MinY), Math.Abs(first.MaxY - second.MaxY)));
    }

    private static object Measure(string name, int queryCount, Func<int> action) {
        for (int warmup = 0; warmup < 4; ++warmup) { _ = action(); }
        double[] samples = new double[9];
        long allocated = 0;
        int checksum = 0;
        for (int trial = 0; trial < samples.Length; ++trial) {
            long before = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            checksum ^= action();
            long elapsed = Stopwatch.GetTimestamp() - start;
            allocated += GC.GetAllocatedBytesForCurrentThread() - before;
            samples[trial] = elapsed * (1e9 / Stopwatch.Frequency) / queryCount;
        }
        Array.Sort(samples);
        return new { Name = name, MedianNanoseconds = samples[4], MinimumNanoseconds = samples[0], MaximumNanoseconds = samples[8], AllocatedBytes = allocated, QueryCount = (long)queryCount * samples.Length, Checksum = checksum };
    }
}
