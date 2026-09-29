using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

/// <summary>
/// Queries immutable track geometry using the same managed pose as car movement.
/// Mesh geometry and positive planar scale are static for a track session, so
/// every car footprint is prepared once, including cars not yet selected.
/// Assumes that the ancestor hierarchy of nodes all have identity transforms.
/// </summary>
public sealed class CollisionManager {
    private const float ShortenColliderFront = 0.165f;
    private const float ShortenColliderRear = 0.0f;

    private readonly CarSwitcher _carSwitcher;
    private readonly RectangleLocalBounds[] _vehicleBounds;
    private readonly TrackCollisionDetector _detector;
    private int _previousCarIndex = -1;
    private RectanglePose _previousPose;
    private bool _previousResult;

    public CollisionManager(string trackName, CarSwitcher carSwitcher) {
        ArgumentException.ThrowIfNullOrEmpty(trackName);
        ArgumentNullException.ThrowIfNull(carSwitcher);
        if (carSwitcher.AvailableCars.Count == 0) {
            throw new ArgumentException("At least one vehicle is required.", nameof(carSwitcher));
        }

        this._carSwitcher = carSwitcher;
        this._vehicleBounds = new RectangleLocalBounds[carSwitcher.AvailableCars.Count];
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        for (int index = 0; index < this._vehicleBounds.Length; ++index) {
            Node3D vehicle = carSwitcher.AvailableCars[index].Node!;
            RectangleLocalBounds raw = VehicleCollisionFootprint.FromMeshGeometry(vehicle).GetScaledLocalBounds(vehicle);
            // Model front (+Z) is the minimum collision Y.
            RectangleLocalBounds bounds = new(raw.MinX, raw.MinY + ShortenColliderFront, raw.MaxX, raw.MaxY - ShortenColliderRear);
            this._vehicleBounds[index] = bounds;
            minX = Math.Min(minX, bounds.MinX);
            minY = Math.Min(minY, bounds.MinY);
            maxX = Math.Max(maxX, bounds.MaxX);
            maxY = Math.Max(maxY, bounds.MaxY);
        }

        ColliderJson colliderJson = JsonUtility.Deserialize<ColliderJson>($"res://Tracks/{trackName}/{trackName}_ColliderData.json");
        this._detector = new TrackCollisionDetector(colliderJson, new RectangleLocalBounds(minX, minY, maxX, maxY));
        GD.Print($"Built track collision index: edges={this._detector.EdgeCount}, cellSize={this._detector.CellSize}, "
            + $"gridCells={this._detector.GridCellCount}, occupiedCells={this._detector.OccupiedCellCount}, "
            + $"references={this._detector.StoredGridEdgeReferenceCount}, expandedGrid={this._detector.UsesExpandedGrid}");
    }

    // "Teleporting" errors are intentionally allowed. A sufficiently fast vehicle
    // and/or low fps can cross a collider without a sampled pose intersecting it.
    public bool IsCarColliding(Vector3 position, float rotationRadians) {
        int carIndex = this._carSwitcher.CurrentCarIndex;
        if (carIndex == this._previousCarIndex
            && position.X == this._previousPose.PositionX
            && -position.Z == this._previousPose.PositionY
            && -rotationRadians == this._previousPose.RotationRadians) {
            return this._previousResult;
        }

        RectanglePose pose = new(position.X, -position.Z, -rotationRadians);
        bool result = this._detector.IsColliding(this._vehicleBounds[carIndex], pose);
        this._previousPose = pose;
        this._previousCarIndex = carIndex;
        this._previousResult = result;
        return result;
    }
}
