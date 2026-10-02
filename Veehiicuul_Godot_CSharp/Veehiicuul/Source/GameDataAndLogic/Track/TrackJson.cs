namespace Veehiicuul_Godot_CSharp;

public sealed class TrackJson {
    public required bool CameraFollowsCarLocation { get; set; }
    public required float FollowCameraSize { get; set; }
    public float CarScale { get; set; }
    public float MinVelocityForRotation { get; set; }
    public required int StartCarIndex { get; set; }
    public required Car[] Cars { get; set; }
}
