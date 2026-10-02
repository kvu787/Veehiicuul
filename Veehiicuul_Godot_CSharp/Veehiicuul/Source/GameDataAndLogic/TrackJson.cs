using System.Collections.Generic;

namespace Veehiicuul_Godot_CSharp;

public sealed class TrackJson {
    public bool CameraFollowsCarLocation { get; set; }
    public float FollowCameraSize { get; set; }
    public float CarScale { get; set; }
    public float MinVelocityForRotation { get; set; }
    public int StartCarIndex { get; set; }
    public List<Car> Cars { get; set; } = [];
}
