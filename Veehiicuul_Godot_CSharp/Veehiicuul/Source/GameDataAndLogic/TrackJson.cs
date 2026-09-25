using System.Collections.Generic;

namespace Veehiicuul_Godot_CSharp;

/// <summary>Original track JSON names, exposed as System.Text.Json properties.</summary>
public sealed class TrackJson {
    public bool CameraFollowsCarLocation { get; set; }
    public float FollowCameraSize { get; set; }
    public float CarScale { get; set; }
    public float MinVelocityForRotation { get; set; }
    public int StartCarIndex { get; set; }
    public List<Car> Cars { get; set; } = [];
}
