using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

/// <summary>
/// Connects the active Godot vehicle to the immutable track-outline index.
/// Track geometry is rebuilt only when a track is initialized; vehicle mesh
/// bounds are refreshed only when the active car changes.
/// </summary>
public sealed class CollisionManager2 {
    // For standard-height barriers. The source's zero-height alternatives were
    // 0.7 at the front and 1.3 at the rear.
    private const float ShortenColliderFront = 0.165f;
    private const float ShortenColliderRear = 0.0f;

    private readonly CarSwitcher _carSwitcher;
    private readonly ColliderJson _colliderJson;

    private Node3D? _currentVehicle;
    private VehicleCollisionFootprint _currentFootprint;
    private TrackCollisionDetector? _detector;

    public CollisionManager2(string trackName, CarSwitcher carSwitcher) {
        ArgumentException.ThrowIfNullOrEmpty(trackName);
        ArgumentNullException.ThrowIfNull(carSwitcher);

        this._carSwitcher = carSwitcher;
        this._colliderJson = JsonUtility.Deserialize<ColliderJson>(
            $"{trackName}_ColliderData.json");
        _ = this.RefreshCurrentVehicleIfNeeded();
    }

    public bool IsCarColliding() {
        Node3D vehicle = this.RefreshCurrentVehicleIfNeeded();
        RectangleLocalBounds bounds = this.GetCurrentVehicleBounds(vehicle);
        Vector3 position = vehicle.GlobalPosition;
        // Godot forward is -Z and positive yaw is counterclockwise viewed from
        // above. The original collision plane is forward-positive and clockwise.
        RectanglePose pose = new(
            position.X,
            -position.Z,
            -Mathf.RadToDeg(vehicle.GlobalRotation.Y));
        TrackCollisionDetector detector = this._detector
            ?? throw new InvalidOperationException("The track collision index was not initialized.");
        return detector.IsColliding(bounds, pose);
    }

    private Node3D RefreshCurrentVehicleIfNeeded() {
        Node3D currentVehicle = this._carSwitcher.CurrentCarTransform;
        if (ReferenceEquals(currentVehicle, this._currentVehicle)) {
            return currentVehicle;
        }

        this._currentVehicle = currentVehicle;
        this._currentFootprint = VehicleCollisionFootprint.FromMeshGeometry(currentVehicle);
        RectangleLocalBounds representativeBounds = this.GetCurrentVehicleBounds(currentVehicle);

        // Keep index cell scale aligned with materially different vehicles while
        // ignoring tiny importer roundoff differences between identical cars.
        float shortExtent = Mathf.Min(
            representativeBounds.MaxX - representativeBounds.MinX,
            representativeBounds.MaxY - representativeBounds.MinY);
        if (this._detector is null
            || shortExtent < this._detector.CellSize * 0.75f
            || shortExtent > this._detector.CellSize * 1.25f) {
            this._detector = new TrackCollisionDetector(
                this._colliderJson,
                representativeBounds);
            GD.Print(
                $"Built track collision index: edges={this._detector.EdgeCount}, "
                + $"cellSize={this._detector.CellSize}, "
                + $"gridCells={this._detector.GridCellCount}, "
                + $"occupiedCells={this._detector.OccupiedCellCount}, "
                + $"oversizedEdges={this._detector.OversizedEdgeCount}, "
                + $"denseGrid={this._detector.UsesDenseGrid}");
        }

        return currentVehicle;
    }

    private RectangleLocalBounds GetCurrentVehicleBounds(Node3D currentVehicle) {
        RectangleLocalBounds bounds = this._currentFootprint.GetScaledLocalBounds(currentVehicle);
        return new RectangleLocalBounds(
            bounds.MinX,
            bounds.MinY + ShortenColliderRear,
            bounds.MaxX,
            bounds.MaxY - ShortenColliderFront);
    }
}
