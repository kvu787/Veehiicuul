using Godot;
using System;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>
/// Checks the footprint that the application derives from a vehicle's meshes.
/// Expected values come from the sizes and offsets that build each vehicle,
/// and, for arbitrary transforms, from the engine's own global transforms.
/// </summary>
internal static class FootprintChecks {
    private const double Tolerance = 1e-5;

    public static void Run(NativeReport report, Node3D scene) {
        report.Begin("Footprint: mesh bounds in vehicle space");
        report.Guarded("single mesh", () => Single(report, scene));
        report.Guarded("nested meshes", () => Nested(report, scene));
        report.Guarded("transformed meshes", () => Transformed(report, scene));
        report.Guarded("refused vehicles", () => Refused(report, scene));
        report.Guarded("engine transforms", () => EngineTransforms(report, scene));
        report.Guarded("mesh below a plain node", () => PlainNode(report, scene));
        report.End();

        report.Begin("Footprint: scaled collision bounds");
        report.Guarded("scaled bounds", () => Scaled(report, scene));
        report.End();
    }

    internal static MeshInstance3D Box(string name, Vector3 size) {
        return new MeshInstance3D { Name = name, Mesh = new BoxMesh { Size = size } };
    }

    /// <summary>Derives the footprint and compares it; a refusal counts as a failed check.</summary>
    private static void Expect(
        NativeReport report, Node3D vehicle,
        double minX, double maxX, double minZ, double maxZ, string name) {
        VehicleCollisionFootprint footprint;
        try {
            footprint = VehicleCollisionFootprint.FromMeshGeometry(vehicle);
        } catch (InvalidOperationException exception) {
            report.Check(false, name + ": " + exception.Message);
            return;
        }

        report.Near(footprint.MinX, minX, Tolerance, name + " MinX");
        report.Near(footprint.MaxX, maxX, Tolerance, name + " MaxX");
        report.Near(footprint.MinZ, minZ, Tolerance, name + " MinZ");
        report.Near(footprint.MaxZ, maxZ, Tolerance, name + " MaxZ");
    }

    private static void Single(NativeReport report, Node3D scene) {
        MeshInstance3D box = Box("SingleBox", new Vector3(3f, 1.2f, 6f));
        scene.AddChild(box);
        Expect(report, box, -1.5, 1.5, -3.0, 3.0, "box as vehicle root");

        // The root's own placement must not enter its local footprint.
        box.Position = new Vector3(100f, 5f, -50f);
        box.Rotation = new Vector3(0f, 1f, 0f);
        box.Scale = new Vector3(2f, 2f, 2f);
        Expect(report, box, -1.5, 1.5, -3.0, 3.0, "moved, turned, scaled root");
        box.QueueFree();
    }

    private static void Nested(NativeReport report, Node3D scene) {
        Node3D root = new() { Name = "NestedRoot" };
        Node3D parent = new() { Name = "Parent", Position = new Vector3(2f, 0f, 1f) };
        MeshInstance3D child = Box("Child", new Vector3(2f, 2f, 4f));
        child.Position = new Vector3(3f, 0f, -2f);
        scene.AddChild(root);
        root.AddChild(parent);
        parent.AddChild(child);
        Expect(report, root, 4.0, 6.0, -3.0, 1.0, "nested child");

        child.Visible = false;
        Expect(report, root, 4.0, 6.0, -3.0, 1.0, "hidden nested child");
        parent.Visible = false;
        Expect(report, root, 4.0, 6.0, -3.0, 1.0, "hidden parent");

        // Position and size of a custom box replace the mesh's own bounds.
        child.CustomAabb = new Aabb(new Vector3(-2f, -1f, -1f), new Vector3(4f, 2f, 2f));
        Expect(report, root, 3.0, 7.0, -2.0, 0.0, "custom bounds");
        child.CustomAabb = default;
        Expect(report, root, 4.0, 6.0, -3.0, 1.0, "custom bounds cleared");

        MeshInstance3D second = Box("Second", new Vector3(1f, 1f, 8f));
        second.Position = new Vector3(-4f, 0f, 1f);
        root.AddChild(second);
        Expect(report, root, -4.5, 6.0, -3.0, 5.0, "two meshes");

        root.QueueFree();
    }

    /// <summary>
    /// A mesh below a node that is not spatial. The engine does not pass the vehicle's
    /// transform through such a node, so the mesh stays where it is when the vehicle
    /// moves. This records how the footprint treats it; it asserts nothing.
    /// </summary>
    private static void PlainNode(NativeReport report, Node3D scene) {
        Node3D root = new() { Name = "PlainRoot" };
        MeshInstance3D body = Box("Body", new Vector3(2f, 1f, 2f));
        Node plain = new() { Name = "Plain" };
        MeshInstance3D beneath = Box("Beneath", new Vector3(2f, 1f, 2f));
        beneath.Position = new Vector3(0f, 0f, 9f);
        scene.AddChild(root);
        root.AddChild(body);
        root.AddChild(plain);
        plain.AddChild(beneath);
        VehicleCollisionFootprint footprint = VehicleCollisionFootprint.FromMeshGeometry(root);
        root.Position = new Vector3(100f, 0f, 0f);
        Vector3 rendered = beneath.GlobalPosition;
        report.Fact(
            "Mesh below a non-spatial node",
            string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"footprint Z reaches {footprint.MaxZ:R}; after moving the vehicle to X=100 the engine "
                + $"renders that mesh at X={rendered.X:R}"));
        root.QueueFree();
    }

    private static void Transformed(NativeReport report, Node3D scene) {
        Node3D root = new() { Name = "TransformedRoot" };
        MeshInstance3D child = Box("Turned", new Vector3(2f, 1f, 6f));
        scene.AddChild(root);
        root.AddChild(child);
        child.Rotation = new Vector3(0f, MathF.PI / 2f, 0f);
        Expect(report, root, -3.0, 3.0, -1.0, 1.0, "quarter turn");

        child.Rotation = Vector3.Zero;
        child.Scale = new Vector3(2f, 1f, 0.5f);
        Expect(report, root, -2.0, 2.0, -1.5, 1.5, "scaled child");

        // An eighth of a turn: the footprint is the box around the turned box.
        child.Scale = Vector3.One;
        child.Rotation = new Vector3(0f, MathF.PI / 4f, 0f);
        double half = (1.0 + 3.0) / Math.Sqrt(2.0);
        Expect(report, root, -half, half, -half, half, "eighth of a turn");
        root.QueueFree();
    }

    private static void Refused(NativeReport report, Node3D scene) {
        Node3D empty = new() { Name = "Empty" };
        empty.AddChild(new Node3D { Name = "NoMesh" });
        scene.AddChild(empty);
        report.Throws<InvalidOperationException>(
            () => VehicleCollisionFootprint.FromMeshGeometry(empty), "vehicle without meshes");

        MeshInstance3D unset = new() { Name = "Unset" };
        scene.AddChild(unset);
        report.Throws<InvalidOperationException>(
            () => VehicleCollisionFootprint.FromMeshGeometry(unset), "mesh node without a mesh");

        // A quad facing +Z has no extent along Z.
        MeshInstance3D flat = new() { Name = "Flat", Mesh = new QuadMesh { Size = new Vector2(2f, 2f) } };
        scene.AddChild(flat);
        report.Throws<InvalidOperationException>(
            () => VehicleCollisionFootprint.FromMeshGeometry(flat), "footprint without area");
        report.Throws<ArgumentNullException>(
            () => VehicleCollisionFootprint.FromMeshGeometry(null!), "null vehicle");
        empty.QueueFree();
        unset.QueueFree();
        flat.QueueFree();
    }

    /// <summary>
    /// Arbitrary nested transforms, judged by the engine: every mesh corner is carried
    /// into vehicle space by the engine's global transforms.
    /// </summary>
    private static void EngineTransforms(NativeReport report, Node3D scene) {
        Random random = new(9701);
        for (int vehicle = 0; vehicle < 40; ++vehicle) {
            Node3D root = new() {
                Name = "Engine" + vehicle,
                Position = RandomVector(random, 50.0),
                Rotation = new Vector3(0f, (float)(random.NextDouble() * Math.Tau), 0f),
                Scale = Vector3.One * (float)(0.5 + random.NextDouble() * 2.0),
            };
            scene.AddChild(root);
            Node3D parent = root;
            MeshInstance3D[] meshes = new MeshInstance3D[1 + random.Next(4)];
            for (int index = 0; index < meshes.Length; ++index) {
                if (random.Next(2) == 0) {
                    Node3D group = new() {
                        Position = RandomVector(random, 2.0),
                        Rotation = RandomVector(random, Math.PI),
                        Scale = new Vector3(
                            (float)(0.5 + random.NextDouble()), (float)(0.5 + random.NextDouble()),
                            (float)(0.5 + random.NextDouble())),
                    };
                    parent.AddChild(group);
                    parent = group;
                }

                MeshInstance3D mesh = Box(
                    "Mesh" + index,
                    new Vector3(
                        (float)(0.5 + random.NextDouble() * 3.0), (float)(0.2 + random.NextDouble()),
                        (float)(0.5 + random.NextDouble() * 6.0)));
                mesh.Position = RandomVector(random, 2.0);
                mesh.Rotation = RandomVector(random, Math.PI);
                parent.AddChild(mesh);
                meshes[index] = mesh;
            }

            double minX = double.PositiveInfinity, maxX = double.NegativeInfinity;
            double minZ = double.PositiveInfinity, maxZ = double.NegativeInfinity;
            Transform3D toVehicle = root.GlobalTransform.AffineInverse();
            foreach (MeshInstance3D mesh in meshes) {
                Transform3D meshToVehicle = toVehicle * mesh.GlobalTransform;
                Aabb bounds = mesh.GetAabb();
                for (int endpoint = 0; endpoint < 8; ++endpoint) {
                    Vector3 point = meshToVehicle * bounds.GetEndpoint(endpoint);
                    minX = Math.Min(minX, point.X);
                    maxX = Math.Max(maxX, point.X);
                    minZ = Math.Min(minZ, point.Z);
                    maxZ = Math.Max(maxZ, point.Z);
                }
            }

            VehicleCollisionFootprint footprint = VehicleCollisionFootprint.FromMeshGeometry(root);
            const double engineTolerance = 2e-4;
            report.Near(footprint.MinX, minX, engineTolerance, $"engine vehicle {vehicle} MinX");
            report.Near(footprint.MaxX, maxX, engineTolerance, $"engine vehicle {vehicle} MaxX");
            report.Near(footprint.MinZ, minZ, engineTolerance, $"engine vehicle {vehicle} MinZ");
            report.Near(footprint.MaxZ, maxZ, engineTolerance, $"engine vehicle {vehicle} MaxZ");
            root.QueueFree();
        }
    }

    private static void Scaled(NativeReport report, Node3D scene) {
        MeshInstance3D box = Box("ScaledBox", new Vector3(3f, 1.2f, 6f));
        scene.AddChild(box);
        VehicleCollisionFootprint footprint = VehicleCollisionFootprint.FromMeshGeometry(box);

        void ExpectBounds(double minX, double minY, double maxX, double maxY, string name) {
            RectangleLocalBounds bounds = footprint.GetScaledLocalBounds(box);
            report.Near(bounds.MinX, minX, Tolerance, name + " MinX");
            report.Near(bounds.MinY, minY, Tolerance, name + " MinY");
            report.Near(bounds.MaxX, maxX, Tolerance, name + " MaxX");
            report.Near(bounds.MaxY, maxY, Tolerance, name + " MaxY");
        }

        ExpectBounds(-1.5, -3.0, 1.5, 3.0, "unit scale");
        box.Scale = new Vector3(2f, 1f, 3f);
        ExpectBounds(-3.0, -9.0, 3.0, 9.0, "scale two by three");
        box.Rotation = new Vector3(0f, 0.7f, 0f);
        ExpectBounds(-3.0, -9.0, 3.0, 9.0, "scale two by three, turned");
        box.Rotation = new Vector3(0f, -2.9f, 0f);
        box.Position = new Vector3(40f, 3f, -70f);
        ExpectBounds(-3.0, -9.0, 3.0, 9.0, "scale two by three, turned and moved");
        box.Scale = new Vector3(1f, 50f, 1f);
        ExpectBounds(-1.5, -3.0, 1.5, 3.0, "height scale is ignored");
        box.Scale = new Vector3(0f, 1f, 1f);
        report.Throws<InvalidOperationException>(() => footprint.GetScaledLocalBounds(box), "zero X scale");
        report.Throws<ArgumentNullException>(() => footprint.GetScaledLocalBounds(null!), "null vehicle");
        box.QueueFree();

        // Collision Y is negative Z: an offset toward +Z moves the bounds toward -Y.
        Node3D root = new() { Name = "OffsetRoot" };
        MeshInstance3D child = Box("OffsetChild", new Vector3(2f, 2f, 4f));
        child.Position = new Vector3(5f, 0f, -1f);
        scene.AddChild(root);
        root.AddChild(child);
        VehicleCollisionFootprint offset = VehicleCollisionFootprint.FromMeshGeometry(root);
        report.Near(offset.MinZ, -3.0, Tolerance, "offset footprint MinZ");
        report.Near(offset.MaxZ, 1.0, Tolerance, "offset footprint MaxZ");
        root.Scale = new Vector3(2f, 1f, 3f);
        RectangleLocalBounds bounds = offset.GetScaledLocalBounds(root);
        report.Near(bounds.MinX, 8.0, Tolerance, "offset bounds MinX");
        report.Near(bounds.MaxX, 12.0, Tolerance, "offset bounds MaxX");
        report.Near(bounds.MinY, -3.0, Tolerance, "offset bounds MinY");
        report.Near(bounds.MaxY, 9.0, Tolerance, "offset bounds MaxY");

        // Scale inherited from an ancestor counts.
        Node3D ancestor = new() { Name = "Ancestor", Scale = new Vector3(1.5f, 1.5f, 1.5f) };
        scene.AddChild(ancestor);
        root.Scale = Vector3.One;
        root.Reparent(ancestor, keepGlobalTransform: false);
        bounds = offset.GetScaledLocalBounds(root);
        report.Near(bounds.MinX, 6.0, Tolerance, "inherited scale MinX");
        report.Near(bounds.MaxX, 9.0, Tolerance, "inherited scale MaxX");
        report.Near(bounds.MinY, -1.5, Tolerance, "inherited scale MinY");
        report.Near(bounds.MaxY, 4.5, Tolerance, "inherited scale MaxY");
        ancestor.QueueFree();
    }

    private static Vector3 RandomVector(Random random, double magnitude) {
        return new Vector3(
            (float)((random.NextDouble() * 2.0 - 1.0) * magnitude),
            (float)((random.NextDouble() * 2.0 - 1.0) * magnitude),
            (float)((random.NextDouble() * 2.0 - 1.0) * magnitude));
    }
}
