using Godot;
using System;
using Veehiicuul_Godot_CSharp;

const float tolerance = 0.00001f;
AssertNear(Vector3.Forward.Rotated(Vector3.Up, Mathf.Pi / 2f), Vector3.Left, tolerance, "Positive camera yaw");
AssertNear(Vector3.Forward.Rotated(Vector3.Up, -Mathf.Pi / 2f), Vector3.Right, tolerance, "Negative camera yaw");
AssertNear(Vector3.Forward.Rotated(Vector3.Up, Mathf.Pi), Vector3.Back, tolerance, "Reverse heading");
Require(Vector3.ModelFront.Get2DRotation() == 0f, "Model front heading");
Require(Vector3.ModelRight.Get2DRotation() == -Mathf.Pi / 2f, "Model right heading");
Require(Vector3.ModelLeft.Get2DRotation() == Mathf.Pi / 2f, "Model left heading");
Require(Mathf.Abs(Vector3.ModelRear.Get2DRotation()) == Mathf.Pi, "Model rear heading");
AssertNear(Vector3.ModelFront.Rotated(Vector3.Up, -Mathf.Pi / 2f), Vector3.ModelRight, tolerance, "Model right turn");
AssertNear(Vector3.ModelFront.Rotated(Vector3.Up, Mathf.Pi / 2f), Vector3.ModelLeft, tolerance, "Model left turn");

int comparisons = 0;
Vector3[] vectors = [new(0f, 0f, 1f), new(1f, 0f, 0f), new(-1.5f, 0f, 2.5f), new(2f, 0f, -3f)];
foreach (Vector3 vector in vectors) {
    for (int sample = -240; sample <= 240; sample++) {
        // Compare Godot's axis-angle and quaternion rotations in the XZ plane.
        float radians = sample * (Mathf.Tau / 120f);
        Quaternion rotation = new(Vector3.Up, radians);
        Vector3 actual = vector.Rotated(Vector3.Up, radians);
        AssertNear(actual, rotation * vector, tolerance, "Native Godot quaternion");
        AssertNear(actual.Rotated(Vector3.Up, -radians), vector, tolerance, "Rotation inverse");
        float heading = actual.Get2DRotation();
        AssertNear(new Quaternion(Vector3.Up, heading) * Vector3.ModelFront, actual.Normalized(), tolerance, "Model heading quaternion");
        AssertNear(Vector3.ModelFront.Rotated(Vector3.Up, heading), actual.Normalized(), tolerance, "Model heading angle");
        Require(actual.Y == 0f, "Planar Y invariant");
        comparisons += 5;
    }
}

bool invalidVectorRejected = false;
try {
    _ = Vector3.Up.Get2DRotation();
} catch (ArgumentException) {
    invalidVectorRejected = true;
}
Require(invalidVectorRejected, "Nonplanar input rejection");
bool zeroVectorRejected = false;
try {
    _ = Vector3.Zero.Get2DRotation();
} catch (ArgumentException) {
    zeroVectorRejected = true;
}
Require(zeroVectorRejected, "Zero heading rejection");
Console.WriteLine($"Passed cardinal headings and {comparisons} coordinate, quaternion, inverse, and planar comparisons.");
CarStateVerification.Run();
CameraStateVerification.Run();

static void AssertNear(Vector3 actual, Vector3 expected, float tolerance, string operation) {
    Require((actual - expected).Length() <= tolerance, $"{operation}: expected {expected}, got {actual}.");
}

static void Require(bool condition, string message) {
    if (!condition) {
        throw new InvalidOperationException(message);
    }
}
