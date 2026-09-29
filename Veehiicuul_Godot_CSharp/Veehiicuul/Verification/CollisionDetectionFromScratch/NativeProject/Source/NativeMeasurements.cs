using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>Calls the collision manager with the arguments the application passes each frame.</summary>
internal readonly struct ManagerQueries : IWorkload {
    private readonly CollisionManager _manager;
    private readonly Vector3[] _positions;
    private readonly float[] _rotations;

    public ManagerQueries(CollisionManager manager, RectanglePose[] poses) {
        this._manager = manager;
        this._positions = new Vector3[poses.Length];
        this._rotations = new float[poses.Length];
        for (int index = 0; index < poses.Length; ++index) {
            this._positions[index] = new Vector3(poses[index].PositionX, 0f, -poses[index].PositionY);
            this._rotations[index] = -poses[index].RotationRadians;
        }
    }

    public int Length => this._positions.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Invoke(int index) {
        return this._manager.IsCarColliding(this._positions[index], this._rotations[index]);
    }
}

internal static class NativeMeasurements {
    private const int PoseCount = 4096;

    public static void Run(NativeReport report, Node3D scene) {
        SyntheticTrack circuit = SyntheticTrack.Circuit("MeasuredCircuit", 20260929, 110.0, 9.0, 2.0, 2.0);

        // Construction of the manager: document parsing, footprints, and the index.
        double[] construction = new double[12];
        ManagerFixture? fixture = null;
        for (int index = 0; index < construction.Length; ++index) {
            long start = Stopwatch.GetTimestamp();
            fixture = ManagerFixture.Create(scene, circuit);
            construction[index] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }

        report.Measurement(
            "ManagerConstruction",
            new { Milliseconds = construction },
            string.Create(
                CultureInfo.InvariantCulture,
                $"first={construction[0]:F3} ms; second={construction[1]:F3} ms; "
                + $"median of later={Statistics.Percentile(construction[2..], 0.5):F3} ms"));

        ManagerFixture ready = fixture!;
        ready.Cars.CurrentCarIndex = 0;
        RectangleLocalBounds bounds = ready.ManagerBounds[0];
        TrackCollisionDetector detector = new(circuit.Collider, bounds);
        RectanglePose[] lap = circuit.LapPoses(PoseCount, 0.25, 0.0);
        RectanglePose[] stationary = new RectanglePose[PoseCount];
        Array.Fill(stationary, lap[0]);
        RectanglePose[] nearMiss = Filter(
            detector, bounds, PoseSampler.NearBarrier(circuit.Oracle, bounds, PoseCount * 16, 8603, 1e-3, 0.5), false);
        RectanglePose[] contact = Filter(
            detector, bounds, PoseSampler.NearBarrier(circuit.Oracle, bounds, PoseCount * 8, 8604, 1e-3, 1.0), true);

        (string Name, RectanglePose[] Poses)[] workloads = [
            ("LapCenter", lap), ("Stationary", stationary), ("NearMiss", nearMiss), ("Contact", contact),
        ];
        foreach ((string name, RectanglePose[] poses) in workloads) {
            Add(report, Bench.Run($"Native/Manager/{name}", new ManagerQueries(ready.Manager, poses)));
            Add(report, Bench.Run($"Native/Detector/{name}", new IndexedQueries(detector, bounds, poses)));
        }
    }

    private static void Add(NativeReport report, Measurement measurement) {
        report.Measurement(measurement.Name, measurement, measurement.Summary());
    }

    private static RectanglePose[] Filter(
        TrackCollisionDetector detector, RectangleLocalBounds bounds, RectanglePose[] poses, bool wantContact) {
        List<RectanglePose> selected = new(PoseCount);
        foreach (RectanglePose pose in poses) {
            if (detector.IsColliding(bounds, pose) == wantContact) {
                selected.Add(pose);
                if (selected.Count == PoseCount) {
                    break;
                }
            }
        }

        return [.. selected];
    }
}

/// <summary>
/// One query per rendered frame, timed alone, while the engine does its own work
/// between frames. The vehicle node is moved each frame as the application moves it.
/// </summary>
internal sealed class FrameMeasurement {
    private readonly ManagerFixture _fixture;
    private readonly RectanglePose[] _poses;
    private readonly double[] _queryNanoseconds;
    private readonly double[] _timerNanoseconds;
    private readonly double[] _frameMilliseconds;
    private readonly int _skippedFrames;
    private int _frame;
    private long _contacts;

    public FrameMeasurement(Node3D scene, int frames, int skippedFrames) {
        SyntheticTrack circuit = SyntheticTrack.Circuit("FrameCircuit", 20260929, 110.0, 9.0, 2.0, 2.0);
        this._fixture = ManagerFixture.Create(scene, circuit);
        this._fixture.Cars.CurrentCarIndex = 0;
        this._poses = circuit.LapPoses(frames + skippedFrames, 0.25, 0.0);
        this._queryNanoseconds = new double[frames];
        this._timerNanoseconds = new double[frames];
        this._frameMilliseconds = new double[frames];
        this._skippedFrames = skippedFrames;
        Scenery.Build(scene, circuit, this._fixture.Nodes[0]);
    }

    public bool IsComplete => this._frame >= this._poses.Length;

    public void Frame(double delta) {
        if (this.IsComplete) {
            return;
        }

        RectanglePose pose = this._poses[this._frame];
        Vector3 position = new(pose.PositionX, 0f, -pose.PositionY);
        float rotation = -pose.RotationRadians;
        long start = Stopwatch.GetTimestamp();
        bool colliding = this._fixture.Manager.IsCarColliding(position, rotation);
        long end = Stopwatch.GetTimestamp();
        long emptyStart = Stopwatch.GetTimestamp();
        long emptyEnd = Stopwatch.GetTimestamp();
        Node3D node = this._fixture.Nodes[0];
        node.Position = position;
        node.Rotation = new Vector3(0f, rotation, 0f);
        int sample = this._frame - this._skippedFrames;
        if (sample >= 0) {
            double tick = 1e9 / Stopwatch.Frequency;
            this._queryNanoseconds[sample] = (end - start) * tick;
            this._timerNanoseconds[sample] = (emptyEnd - emptyStart) * tick;
            this._frameMilliseconds[sample] = delta * 1000.0;
            if (colliding) {
                ++this._contacts;
            }
        }

        ++this._frame;
    }

    public void Report(NativeReport report, string name) {
        double mean = Statistics.Mean(this._queryNanoseconds);
        double timer = Statistics.Mean(this._timerNanoseconds);
        report.Measurement(
            name,
            new {
                Frames = this._queryNanoseconds.Length,
                SkippedFrames = this._skippedFrames,
                MeanNanoseconds = mean,
                TimerMeanNanoseconds = timer,
                NetMeanNanoseconds = mean - timer,
                MedianNanoseconds = Statistics.Percentile(this._queryNanoseconds, 0.5),
                Percentile90Nanoseconds = Statistics.Percentile(this._queryNanoseconds, 0.9),
                Percentile99Nanoseconds = Statistics.Percentile(this._queryNanoseconds, 0.99),
                MaximumNanoseconds = Statistics.Percentile(this._queryNanoseconds, 1.0),
                MedianFrameMilliseconds = Statistics.Percentile(this._frameMilliseconds, 0.5),
                Contacts = this._contacts,
            },
            string.Create(
                CultureInfo.InvariantCulture,
                $"frames={this._queryNanoseconds.Length}; mean={mean:F1} ns; timer={timer:F1} ns; "
                + $"net={mean - timer:F1} ns; median={Statistics.Percentile(this._queryNanoseconds, 0.5):F0}; "
                + $"p90={Statistics.Percentile(this._queryNanoseconds, 0.9):F0}; "
                + $"p99={Statistics.Percentile(this._queryNanoseconds, 0.99):F0}; "
                + $"max={Statistics.Percentile(this._queryNanoseconds, 1.0):F0}; "
                + $"median frame={Statistics.Percentile(this._frameMilliseconds, 0.5):F3} ms"));
    }
}

/// <summary>Something for the renderer to draw: the barriers as walls, a ground plane, a light, a camera.</summary>
internal static class Scenery {
    public static void Build(Node3D scene, SyntheticTrack track, Node3D vehicle) {
        OracleTrack oracle = track.Oracle;
        SurfaceTool tool = new();
        tool.Begin(Mesh.PrimitiveType.Triangles);
        const float height = 2f;
        for (int edge = 0; edge < oracle.EdgeCount; ++edge) {
            // Collision Y is negative Z.
            Vector3 a = new(oracle.Ax[edge], 0f, -oracle.Ay[edge]);
            Vector3 b = new(oracle.Bx[edge], 0f, -oracle.By[edge]);
            Vector3 up = new(0f, height, 0f);
            Vector3 normal = (b - a).Cross(up).Normalized();
            foreach (Vector3 vertex in new[] { a, b, b + up, a, b + up, a + up, b, a, a + up, b, a + up, b + up }) {
                tool.SetNormal(normal);
                tool.AddVertex(vertex);
            }
        }

        scene.AddChild(new MeshInstance3D { Name = "Barriers", Mesh = tool.Commit() });
        scene.AddChild(new MeshInstance3D {
            Name = "Ground",
            Mesh = new PlaneMesh { Size = new Vector2(400f, 400f) },
            Position = new Vector3(0f, -0.01f, 0f),
        });
        scene.AddChild(new DirectionalLight3D {
            Name = "Light",
            Rotation = new Vector3(-1f, 0.5f, 0f),
            ShadowEnabled = true,
        });
        Camera3D camera = new() {
            Name = "Camera",
            Projection = Camera3D.ProjectionType.Orthogonal,
            Size = 300f,
            Position = new Vector3(0f, 200f, 200f),
            Far = 1000f,
        };
        scene.AddChild(camera);
        camera.LookAt(Vector3.Zero, Vector3.Up);
        camera.Current = true;
        vehicle.Visible = true;
    }
}
