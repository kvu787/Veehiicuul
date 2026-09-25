using Godot;

namespace Veehiicuul_Godot_CSharp;

public static class QuaternionUtility {
    /// <summary>Prints the same yaw floating-point error experiment using Godot quaternions.</summary>
    public static void DemonstrateFloatingPointError() {
        Vector3 vector = new(1.5f, 0f, -2.5f);
        float maximumVerticalError = 0f;
        float maximumMagnitudeError = 0f;
        float maximumRoundTripMagnitudeError = 0f;

        for (int angle = 1; angle <= 360; angle++) {
            Quaternion rotation = new(Vector3.Up, -Mathf.DegToRad(angle));
            Vector3 rotatedVector = rotation * vector;
            maximumVerticalError = Mathf.Max(maximumVerticalError, Mathf.Abs(rotatedVector.Y));
            maximumMagnitudeError = Mathf.Max(maximumMagnitudeError, Mathf.Abs(rotatedVector.Length() - vector.Length()));
            Vector3 roundTripVector = new Quaternion(Vector3.Up, Mathf.DegToRad(angle)) * rotatedVector;
            maximumRoundTripMagnitudeError = Mathf.Max(maximumRoundTripMagnitudeError, (roundTripVector - vector).Length());
        }

        Vector3 accumulatedVector = vector;
        Quaternion step = new(Vector3.Up, -Mathf.DegToRad(1f));
        for (int index = 0; index < 360; index++) {
            accumulatedVector = step * accumulatedVector;
        }
        float accumulatedDrift = (accumulatedVector - vector).Length();

        GD.Print(
            $"Floating-point errors:\n" +
            $"Max |y| error (provably 0):                {maximumVerticalError.ToExactDecimalString()}\n" +
            $"Max magnitude error for (single rotation): {maximumMagnitudeError.ToExactDecimalString()}\n" +
            $"Max round-trip drift R(-a)*R(a)*v vs v:    {maximumRoundTripMagnitudeError.ToExactDecimalString()}\n" +
            $"Accumulated drift after 360 x 1 deg steps: {accumulatedDrift.ToExactDecimalString()}");
    }
}
