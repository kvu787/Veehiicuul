using System;

namespace Veehiicuul_Godot_CSharp;

/// <summary>
/// Retains the original inactive collision-manager entry point. ZoomTracks had
/// already commented out its physics-overlap loop; CollisionManager2 owns the
/// active mesh-footprint versus track-outline implementation.
/// </summary>
public sealed class CollisionManager {
    public CollisionManager(TrackObjects trackObjects, CarSwitcher carSwitcher) {
        ArgumentNullException.ThrowIfNull(trackObjects);
        ArgumentNullException.ThrowIfNull(carSwitcher);
    }

    public static bool IsCarColliding() {
        return false;
    }
}
