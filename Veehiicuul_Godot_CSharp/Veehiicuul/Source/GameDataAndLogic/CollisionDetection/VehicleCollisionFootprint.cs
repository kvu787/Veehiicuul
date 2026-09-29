using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

/// <summary>
/// The axis-aligned footprint of a vehicle's render meshes in Godot vehicle-local
/// X/Z space. Derive it once per track session, including hidden mesh children.
/// </summary>
public readonly struct VehicleCollisionFootprint {
    private VehicleCollisionFootprint(float minX, float minZ, float maxX, float maxZ) {
        this.MinX = minX;
        this.MinZ = minZ;
        this.MaxX = maxX;
        this.MaxZ = maxZ;
    }

    public float MinX { get; }
    public float MinZ { get; }
    public float MaxX { get; }
    public float MaxZ { get; }

    public static VehicleCollisionFootprint FromMeshGeometry(Node3D vehicleRoot) {
        ArgumentNullException.ThrowIfNull(vehicleRoot);

        bool foundBounds = false;
        float minX = float.PositiveInfinity;
        float minZ = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxZ = float.NegativeInfinity;

        // MeshInstance3D represents both static meshes and meshes using a Skeleton3D.
        // Walk every child, independent of visibility, just like includeInactive.
        IncludeNode(vehicleRoot, Transform3D.Identity, ref foundBounds, ref minX, ref minZ, ref maxX, ref maxZ);

        if (!foundBounds) {
            throw new InvalidOperationException(
                $"Vehicle '{vehicleRoot.Name}' has no mesh geometry from which to derive collision bounds.");
        }

        if (!(minX < maxX) || !(minZ < maxZ)) {
            throw new InvalidOperationException(
                $"Vehicle '{vehicleRoot.Name}' does not have a positive-area X/Z mesh footprint.");
        }

        return new VehicleCollisionFootprint(minX, minZ, maxX, maxZ);
    }

    public RectangleLocalBounds GetScaledLocalBounds(Node3D vehicleRoot) {
        ArgumentNullException.ThrowIfNull(vehicleRoot);

        // CarState applies pure yaw and positive, axis-aligned vehicle scale.
        // Basis-vector lengths retain scale without turning yaw into AABB growth.
        Basis basis = vehicleRoot.GlobalBasis;
        float xScale = basis.X.Length();
        float zScale = basis.Z.Length();
        if (!Guard.IsFinite(xScale)
            || !Guard.IsFinite(zScale)
            || !(xScale > 0f)
            || !(zScale > 0f)) {
            throw new InvalidOperationException(
                $"Vehicle '{vehicleRoot.Name}' must have finite positive planar scale.");
        }

        float x0 = this.MinX * xScale;
        float x1 = this.MaxX * xScale;
        // Collision Y is negative Godot Z, so negate and reverse the interval.
        // Model front (+Z) is therefore at the minimum collision Y.
        float y0 = -this.MaxZ * zScale;
        float y1 = -this.MinZ * zScale;
        return new RectangleLocalBounds(
            Math.Min(x0, x1),
            Math.Min(y0, y1),
            Math.Max(x0, x1),
            Math.Max(y0, y1));
    }

    private static void IncludeNode(Node node, Transform3D nodeToVehicle,
        ref bool foundBounds, ref float minX, ref float minZ, ref float maxX, ref float maxZ) {
        if (node is MeshInstance3D meshInstance && meshInstance.Mesh is not null) {
            Aabb customBounds = meshInstance.CustomAabb;
            Aabb bounds = customBounds == default ? meshInstance.GetAabb() : customBounds;
            IncludeBounds(bounds, nodeToVehicle, ref foundBounds, ref minX, ref minZ, ref maxX, ref maxZ);
        }

        int childCount = node.GetChildCount();
        for (int index = 0; index < childCount; ++index) {
            Node child = node.GetChild(index);
            Transform3D childToVehicle = child is Node3D child3D ? nodeToVehicle * child3D.Transform : nodeToVehicle;
            IncludeNode(child, childToVehicle, ref foundBounds, ref minX, ref minZ, ref maxX, ref maxZ);
        }
    }

    private static void IncludeBounds(
        Aabb bounds,
        Transform3D meshToVehicle,
        ref bool foundBounds,
        ref float minX,
        ref float minZ,
        ref float maxX,
        ref float maxZ) {
        for (int endpoint = 0; endpoint < 8; ++endpoint) {
            Vector3 vehiclePoint = meshToVehicle * bounds.GetEndpoint(endpoint);
            if (!Guard.IsFinite(vehiclePoint.X) || !Guard.IsFinite(vehiclePoint.Z)) {
                throw new InvalidOperationException(
                    "Vehicle mesh bounds produced a nonfinite local footprint.");
            }

            minX = Math.Min(minX, vehiclePoint.X);
            minZ = Math.Min(minZ, vehiclePoint.Z);
            maxX = Math.Max(maxX, vehiclePoint.X);
            maxZ = Math.Max(maxZ, vehiclePoint.Z);
            foundBounds = true;
        }
    }
}
