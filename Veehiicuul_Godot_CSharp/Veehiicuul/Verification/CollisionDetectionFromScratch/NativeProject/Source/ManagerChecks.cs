using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>Vehicles, a generated track, and the collision manager built from them.</summary>
internal sealed class ManagerFixture {
    /// <summary>The manager moves the front limit, which is the minimum collision Y, by this amount.</summary>
    public const float FrontShortening = 0.165f;

    private ManagerFixture(
        SyntheticTrack track, CarSwitcher cars, CollisionManager manager,
        Node3D[] nodes, RectangleLocalBounds[] expectedBounds, RectangleLocalBounds[] managerBounds) {
        this.Track = track;
        this.Cars = cars;
        this.Manager = manager;
        this.Nodes = nodes;
        this.ExpectedBounds = expectedBounds;
        this.ManagerBounds = managerBounds;
    }

    public SyntheticTrack Track { get; }
    public CarSwitcher Cars { get; }
    public CollisionManager Manager { get; }
    public Node3D[] Nodes { get; }

    /// <summary>Bounds derived from the sizes and offsets that build each vehicle.</summary>
    public RectangleLocalBounds[] ExpectedBounds { get; }

    /// <summary>Bounds the manager holds, read from its private field.</summary>
    public RectangleLocalBounds[] ManagerBounds { get; }

    public static string ResourcePath(string trackName) {
        return $"res://Tracks/{trackName}/{trackName}_ColliderData.json";
    }

    public static ManagerFixture Create(Node3D scene, SyntheticTrack track) {
        JsonUtility.Register(ResourcePath(track.Name), JsonSerializer.Serialize(track.Collider));

        MeshInstance3D plain = FootprintChecks.Box(track.Name + "Plain", new Vector3(3f, 1.2f, 6f));
        MeshInstance3D compact = FootprintChecks.Box(track.Name + "Compact", new Vector3(2f, 1f, 4.5f));
        Node3D winged = new() { Name = track.Name + "Winged" };
        MeshInstance3D body = FootprintChecks.Box("Body", new Vector3(3f, 1f, 6f));
        body.Position = new Vector3(0f, 0f, 0.25f);
        MeshInstance3D wing = FootprintChecks.Box("Wing", new Vector3(3.4f, 0.2f, 0.5f));
        wing.Position = new Vector3(0f, 0.8f, -2.9f);
        wing.Visible = false;
        winged.AddChild(body);
        winged.AddChild(wing);
        MeshInstance3D enlarged = FootprintChecks.Box(track.Name + "Enlarged", new Vector3(3f, 1.2f, 6f));
        enlarged.Scale = new Vector3(1.5f, 1.5f, 1.5f);
        Node3D[] nodes = [plain, compact, winged, enlarged];
        List<Car> cars = [];
        for (int index = 0; index < nodes.Length; ++index) {
            scene.AddChild(nodes[index]);
            // Parked apart and turned, as vehicles waiting for selection would be.
            nodes[index].Position = new Vector3(500f + 20f * index, 0f, 300f);
            nodes[index].Rotation = new Vector3(0f, 0.4f * index, 0f);
            cars.Add(new Car { Node = nodes[index] });
        }

        RectangleLocalBounds[] expected = [
            new(-1.5f, -3f + FrontShortening, 1.5f, 3f),
            new(-1f, -2.25f + FrontShortening, 1f, 2.25f),
            new(-1.7f, -3.25f + FrontShortening, 1.7f, 3.15f),
            new(-2.25f, -4.5f + FrontShortening, 2.25f, 4.5f),
        ];
        CarSwitcher switcher = new(cars);
        CollisionManager manager = new(track.Name, switcher);
        FieldInfo field = typeof(CollisionManager).GetField(
            "_vehicleBounds", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingFieldException(nameof(CollisionManager), "_vehicleBounds");
        RectangleLocalBounds[] held = (RectangleLocalBounds[])field.GetValue(manager)!;
        return new ManagerFixture(track, switcher, manager, nodes, expected, held);
    }

    /// <summary>The manager's arguments for a pose given in collision coordinates.</summary>
    public bool Query(RectanglePose pose, float height = 0f) {
        return this.Manager.IsCarColliding(
            new Vector3(pose.PositionX, height, -pose.PositionY), -pose.RotationRadians);
    }
}

internal static class ManagerChecks {
    public static void Run(NativeReport report, Node3D scene) {
        SyntheticTrack circuit = SyntheticTrack.Circuit("NativeCircuit", 20260929, 110.0, 9.0, 2.0, 2.0);
        SyntheticTrack wall = SyntheticTrack.FromOutlines(
            "NativeWall", [(50f, -100f), (60f, -100f), (60f, 100f), (50f, 100f)]);
        ManagerFixture? created = null, createdWall = null;
        int requestsBefore = JsonUtility.RequestCount, requestsAfter = -1;
        report.Begin("Manager: construction");
        report.Guarded("circuit manager", () => {
            created = ManagerFixture.Create(scene, circuit);
            requestsAfter = JsonUtility.RequestCount;
        });
        report.Guarded("wall manager", () => createdWall = ManagerFixture.Create(scene, wall));
        report.Check(created is not null && createdWall is not null, "Both managers were constructed.");
        report.Check(requestsAfter == requestsBefore + 1, "The collider document is requested once.");
        report.End();
        if (created is null || createdWall is null) {
            return;
        }

        ManagerFixture fixture = created, wallFixture = createdWall;
        report.Begin("Manager: vehicle bounds");
        report.Check(fixture.ManagerBounds.Length == 4, "One set of bounds per vehicle.");
        for (int index = 0; index < fixture.ExpectedBounds.Length; ++index) {
            RectangleLocalBounds expected = fixture.ExpectedBounds[index], actual = fixture.ManagerBounds[index];
            report.Near(actual.MinX, expected.MinX, 1e-5, $"vehicle {index} MinX");
            report.Near(actual.MinY, expected.MinY, 1e-5, $"vehicle {index} MinY (front, shortened)");
            report.Near(actual.MaxX, expected.MaxX, 1e-5, $"vehicle {index} MaxX");
            report.Near(actual.MaxY, expected.MaxY, 1e-5, $"vehicle {index} MaxY (rear)");
            report.Fact(
                $"Vehicle {index}",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"({actual.MinX:R}, {actual.MinY:R}, {actual.MaxX:R}, {actual.MaxY:R})"));
        }

        report.Throws<ArgumentException>(() => _ = new CollisionManager(string.Empty, fixture.Cars), "empty track name");
        report.Throws<ArgumentNullException>(() => _ = new CollisionManager("NativeCircuit", null!), "null vehicle list");
        report.Throws<InvalidOperationException>(
            () => _ = new CollisionManager("Unregistered", fixture.Cars), "track without a document");
        report.Throws<ArgumentException>(
            () => _ = new CollisionManager("NativeCircuit", new CarSwitcher([])), "no vehicles");
        report.Throws<ArgumentNullException>(
            () => _ = new CollisionManager("NativeCircuit", new CarSwitcher([new Car()])), "vehicle without a node");
        report.End();

        report.Begin("Manager: poses reach the detector unchanged");
        report.Guarded("pose mapping", () => PoseMapping(report, fixture));
        report.End();

        report.Begin("Manager: rectangle matches the engine's transform of the mesh");
        report.Guarded("engine corners", () => EngineCorners(report, fixture));
        report.End();

        report.Begin("Manager: contact as seen in the engine");
        report.Guarded("visible contact", () => VisibleContact(report, wallFixture));
        report.End();

        report.Begin("Manager: repeated and changed queries");
        report.Guarded("repetition", () => Repetition(report, wallFixture));
        report.End();
    }

    private static void PoseMapping(NativeReport report, ManagerFixture fixture) {
        OracleTrack oracle = fixture.Track.Oracle;
        float[] corners = new float[8];
        int contacts = 0, queries = 0;
        for (int vehicle = 0; vehicle < fixture.ManagerBounds.Length; ++vehicle) {
            fixture.Cars.CurrentCarIndex = vehicle;
            RectangleLocalBounds bounds = fixture.ManagerBounds[vehicle];
            List<RectanglePose> poses = [];
            poses.AddRange(PoseSampler.Uniform(oracle, 6000, 9800 + vehicle, 10.0));
            poses.AddRange(PoseSampler.NearBarrier(oracle, bounds, 6000, 9810 + vehicle, 1e-6, 1.5));
            poses.AddRange(fixture.Track.LapPoses(2000, 0.37, 0.5));
            foreach (RectanglePose pose in poses) {
                DetectorInternals.Transform(bounds, pose, corners);
                bool expected = oracle.Intersects(corners);
                bool actual = fixture.Query(pose, (queries % 7) * 3f);
                bool repeated = fixture.Query(pose, -1f);
                ++queries;
                if (expected) {
                    ++contacts;
                }

                report.Check(
                    actual == expected && repeated == expected,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"vehicle {vehicle}: manager={actual}, repeated={repeated}, exact={expected} at "
                        + $"({pose.PositionX:R}, {pose.PositionY:R}, {pose.RotationRadians:R})"));
            }
        }

        report.Fact("Queries", queries.ToString(CultureInfo.InvariantCulture));
        report.Fact("Contacts", contacts.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Places each vehicle node and asks the engine where the corners of its mesh
    /// footprint are. The detector's rectangle for the corresponding pose, before the
    /// front is shortened, must be the same quadrilateral.
    /// </summary>
    private static void EngineCorners(NativeReport report, ManagerFixture fixture) {
        Random random = new(9820);
        float[] corners = new float[8];
        double largest = 0.0;
        for (int vehicle = 0; vehicle < fixture.Nodes.Length; ++vehicle) {
            Node3D node = fixture.Nodes[vehicle];
            VehicleCollisionFootprint footprint = VehicleCollisionFootprint.FromMeshGeometry(node);
            RectangleLocalBounds bounds = footprint.GetScaledLocalBounds(node);
            // Detector corner order against mesh corners in vehicle space.
            Vector3[] local = [
                new(footprint.MinX, 0f, footprint.MaxZ),
                new(footprint.MaxX, 0f, footprint.MaxZ),
                new(footprint.MaxX, 0f, footprint.MinZ),
                new(footprint.MinX, 0f, footprint.MinZ),
            ];
            for (int iteration = 0; iteration < 500; ++iteration) {
                Vector3 position = new(
                    (float)((random.NextDouble() * 2.0 - 1.0) * 150.0), 0f,
                    (float)((random.NextDouble() * 2.0 - 1.0) * 150.0));
                float yaw = (float)((random.NextDouble() * 2.0 - 1.0) * Math.PI);
                node.Position = position;
                node.Rotation = new Vector3(0f, yaw, 0f);
                DetectorInternals.Transform(bounds, new RectanglePose(position.X, -position.Z, -yaw), corners);
                Transform3D transform = node.GlobalTransform;
                for (int corner = 0; corner < 4; ++corner) {
                    Vector3 world = transform * local[corner];
                    double difference = Math.Max(
                        Math.Abs((double)world.X - corners[2 * corner]),
                        Math.Abs((double)-world.Z - corners[2 * corner + 1]));
                    largest = Math.Max(largest, difference);
                    report.Check(
                        difference <= 2e-4,
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"vehicle {vehicle} corner {corner}: engine ({world.X:R}, {-world.Z:R}), detector "
                            + $"({corners[2 * corner]:R}, {corners[2 * corner + 1]:R})"));
                }
            }
        }

        report.Fact("Largest difference", largest.ToString("R", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// A wall whose near face is at X = 50. The first vehicle is three wide and six long.
    /// In the engine a yaw of a quarter turn points the model front, local +Z, along +X.
    /// </summary>
    private static void VisibleContact(NativeReport report, ManagerFixture fixture) {
        fixture.Cars.CurrentCarIndex = 0;
        CollisionManager manager = fixture.Manager;
        const float quarter = MathF.PI / 2f;

        bool At(float x, float yaw) {
            return manager.IsCarColliding(new Vector3(x, 0f, 0f), yaw);
        }

        // Front toward the wall: the mesh front is three units ahead of the position.
        report.Check(!At(46.99f, quarter), "front face 0.01 short of the wall is clear");
        report.Check(!At(47.10f, quarter), "front face 0.10 inside the wall is still clear: the front is shortened");
        report.Check(!At(47.16f, quarter), "front face 0.16 inside the wall is still clear");
        report.Check(At(47.17f, quarter), "front face 0.17 inside the wall is a contact");

        // Rear toward the wall: the rear is not shortened.
        report.Check(!At(46.99f, -quarter), "rear face 0.01 short of the wall is clear");
        report.Check(At(47.01f, -quarter), "rear face 0.01 inside the wall is a contact");

        // Side toward the wall: half the width is 1.5.
        report.Check(!At(48.49f, 0f), "side 0.01 short of the wall is clear");
        report.Check(At(48.51f, 0f), "side 0.01 inside the wall is a contact");
        report.Check(!At(48.49f, MathF.PI), "other side 0.01 short of the wall is clear");
        report.Check(At(48.51f, MathF.PI), "other side 0.01 inside the wall is a contact");

        // The same statements, with the mesh position taken from the engine.
        Node3D node = fixture.Nodes[0];
        node.Position = new Vector3(47.10f, 0f, 0f);
        node.Rotation = new Vector3(0f, quarter, 0f);
        Vector3 front = node.GlobalTransform * new Vector3(0f, 0f, 3f);
        report.Near(front.X, 50.10, 1e-4, "engine position of the mesh front");
        report.Fact(
            "Front overlap that is not reported",
            string.Create(CultureInfo.InvariantCulture, $"mesh front at X={front.X:R}, wall at X=50"));

        // Entirely inside the wall, which is ten thick and two hundred long: nothing is reported.
        report.Check(!At(55f, 0f), "vehicle enclosed by the barrier outline is clear");
        // Entirely beyond every barrier.
        report.Check(!At(500f, 0.3f), "vehicle far outside is clear");
    }

    private static void Repetition(NativeReport report, ManagerFixture fixture) {
        CollisionManager manager = fixture.Manager;
        CarSwitcher cars = fixture.Cars;
        const float quarter = MathF.PI / 2f;
        cars.CurrentCarIndex = 0;
        Vector3 beside = new(48f, 0f, 0f);

        report.Check(!manager.IsCarColliding(beside, 0f), "parallel to the wall: clear");
        report.Check(!manager.IsCarColliding(beside, 0f), "same pose again: clear");
        report.Check(manager.IsCarColliding(beside, quarter), "same position, front turned to the wall: contact");
        report.Check(manager.IsCarColliding(beside, quarter), "same pose again: contact");
        report.Check(!manager.IsCarColliding(beside, 0f), "turned back: clear");

        // The wall ends at engine Z = -100 and Z = 100. Only one coordinate changes at a time.
        report.Check(manager.IsCarColliding(beside, quarter), "front turned to the wall: contact");
        report.Check(!manager.IsCarColliding(new Vector3(48f, 0f, -200f), quarter), "same X and yaw, beyond the wall's end: clear");
        report.Check(manager.IsCarColliding(beside, quarter), "back beside the wall: contact");
        report.Check(!manager.IsCarColliding(new Vector3(48f, 0f, 200f), quarter), "same X and yaw, beyond the other end: clear");
        report.Check(manager.IsCarColliding(beside, quarter), "back beside the wall again: contact");
        report.Check(!manager.IsCarColliding(new Vector3(40f, 0f, 0f), quarter), "same Z and yaw, farther from the wall: clear");
        report.Check(manager.IsCarColliding(beside, quarter), "back beside the wall once more: contact");

        // The fourth vehicle is one and a half times as large.
        cars.CurrentCarIndex = 3;
        report.Check(manager.IsCarColliding(beside, 0f), "same pose, wider vehicle: contact");
        cars.CurrentCarIndex = 0;
        report.Check(!manager.IsCarColliding(beside, 0f), "same pose, first vehicle again: clear");

        report.Check(!manager.IsCarColliding(new Vector3(48f, 1000f, 0f), 0f), "height does not matter: clear");
        report.Check(manager.IsCarColliding(new Vector3(48f, -1000f, 0f), quarter), "height does not matter: contact");
        report.Check(
            manager.IsCarColliding(new Vector3(48.51f, 0f, -0f), -0f)
                == manager.IsCarColliding(new Vector3(48.51f, 0f, 0f), 0f),
            "negative zero equals zero");

        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }) {
            report.Throws<ArgumentException>(
                () => manager.IsCarColliding(new Vector3(invalid, 0f, 0f), 0f), $"X position {invalid}");
            report.Throws<ArgumentException>(
                () => manager.IsCarColliding(new Vector3(0f, 0f, invalid), 0f), $"Z position {invalid}");
            report.Throws<ArgumentException>(
                () => manager.IsCarColliding(beside, invalid), $"yaw {invalid}");
            report.Check(manager.IsCarColliding(beside, quarter), "valid query after a refused one: contact");
            report.Check(!manager.IsCarColliding(beside, 0f), "valid query after a refused one: clear");
        }

        report.Check(!manager.IsCarColliding(new Vector3(48f, float.NaN, 0f), 0f), "height that is not a number is ignored");

        // Bounds are captured when the manager is built.
        Node3D first = fixture.Nodes[0];
        Vector3 scale = first.Scale;
        first.Scale = new Vector3(3f, 3f, 3f);
        bool afterScaling = manager.IsCarColliding(new Vector3(48.2f, 0f, 0f), 0f);
        first.Scale = scale;
        report.Fact(
            "Vehicle scaled after construction",
            afterScaling ? "the manager follows the new scale" : "the manager keeps the bounds it captured");

        cars.CurrentCarIndex = 99;
        report.Throws<Exception>(() => manager.IsCarColliding(beside, 0f), "vehicle index out of range");
        cars.CurrentCarIndex = 0;
    }
}
