using Godot;
using System;
using Veehiicuul_Godot_CSharp;

const float tolerance = 0.00001f;
AssertNear(Vector3.Forward.Rotate2D(90f), Vector3.Right, tolerance, "Right turn");
AssertNear(Vector3.Forward.Rotate2D(-90f), Vector3.Left, tolerance, "Left turn");
AssertNear(Vector3.Forward.Rotate2D(180f), Vector3.Back, tolerance, "Reverse heading");
Require(Vector3.Zero.Get2DRotation() == 0f, "Zero heading");
Require(Vector3.Right.Get2DRotation() == 90f, "Right heading");
Require(Vector3.Left.Get2DRotation() == -90f, "Left heading");

int comparisons = 0;
Vector3[] sourceVectors = [new(0f, 0f, 1f), new(1f, 0f, 0f), new(-1.5f, 0f, 2.5f), new(2f, 0f, -3f)];
foreach (Vector3 source in sourceVectors) {
    for (int degrees = -720; degrees <= 720; degrees += 3) {
        // This is the original Unity formula evaluated independently,
        // followed by reflection into Godot space: (x,y,z) -> (x,y,-z).
        float radians = Mathf.DegToRad(degrees);
        float cosine = Mathf.Cos(radians);
        float sine = Mathf.Sin(radians);
        Vector3 expected = new(source.X * cosine + source.Z * sine, 0f, source.X * sine - source.Z * cosine);
        Vector3 reflected = new(source.X, 0f, -source.Z);
        Vector3 actual = reflected.Rotate2D(degrees);
        AssertNear(actual, expected, tolerance, "Source reflection");
        AssertNear(actual.Rotate2D(-degrees), reflected, tolerance, "Rotation inverse");
        AssertNear(new Quaternion(Vector3.Up, -radians) * reflected, actual, tolerance, "Native Godot quaternion");
        AssertNear(reflected.Rotate2D(new Quaternion(Vector3.Up, -radians)), actual, tolerance, "Quaternion overload");
        AssertNear(actual.Get2DRotationQuaternion() * Vector3.Forward, actual.Normalized(), tolerance, "Heading quaternion");
        Require(actual.Y == 0f, "Planar Y invariant");
        comparisons += 6;
    }
}

bool invalidVectorRejected = false;
try {
    _ = Vector3.Up.Rotate2D(90f);
} catch (ArgumentException) {
    invalidVectorRejected = true;
}
Require(invalidVectorRejected, "Nonplanar input rejection");
Console.WriteLine($"Passed cardinal headings and {comparisons} coordinate, quaternion, inverse, and planar comparisons.");

static void AssertNear(Vector3 actual, Vector3 expected, float tolerance, string operation) {
    Require((actual - expected).Length() <= tolerance, $"{operation}: expected {expected}, got {actual}.");
}

static void Require(bool condition, string message) {
    if (!condition) {
        throw new InvalidOperationException(message);
    }
}
