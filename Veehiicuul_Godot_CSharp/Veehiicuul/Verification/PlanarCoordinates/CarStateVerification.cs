using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

internal static class CarStateVerification {
    public static void Run() {
        (string Name, Action Verify)[] checks = [
            ("Neutral input preserves motion", VerifyCoasting),
            ("Applied heading follows velocity", VerifyHeading),
            ("Rotated spawn uses forward acceleration", VerifyRotatedSpawn),
            ("Camera pivot yaw rotates input independently of camera children", VerifyCameraYaw),
            ("Braking slows without reversing and preserves heading", VerifyBraking),
            ("Neutral input still enforces the speed limit", VerifySpeedLimit),
            ("Reset restores the authored pose and clears velocity", VerifyReset),
            ("Spawn pose includes parent transforms", VerifyWorldSpawn)
        ];
        int failures = 0;
        foreach ((string name, Action verify) in checks) {
            try {
                verify();
                Console.WriteLine($"PASS: {name}");
            } catch (InvalidOperationException exception) {
                failures++;
                Console.WriteLine($"FAIL: {name}: {exception.Message}");
            }
        }
        if (failures != 0) {
            throw new InvalidOperationException($"{failures} car state checks failed.");
        }
        Console.WriteLine($"Passed {checks.Length} car state checks.");
    }

    private static void VerifyCoasting() {
        (CarStateManager manager, InputManager input, CarSwitcher cars) = Create();
        input.AccelerationInput = new Vector2(0f, 1f);
        manager.ReadInputAndUpdateState(0.5);
        Vector3 acceleratedPosition = manager.Position;
        input.AccelerationInput = Vector2.Zero;
        manager.ReadInputAndUpdateState(0.25);
        AssertNear(manager.Position - acceleratedPosition, new Vector3(0f, 0f, -0.5f));
        Vector3 coastedPosition = manager.Position;
        input.AccelerationInput = new Vector2(0.01f, 0.01f);
        manager.ReadInputAndUpdateState(0.25);
        AssertNear(manager.Position - coastedPosition, new Vector3(0f, 0f, -0.5f));
        manager.Apply();
        AssertNear(cars.CurrentCar.Node.Position, manager.Position);
    }

    private static void VerifyHeading() {
        Vector2[] inputs = [new(0f, 1f), new(0f, -1f), new(1f, 0f), new(-1f, 0f), new(1f, 1f), new(-1f, 1f)];
        foreach (Vector2 direction in inputs) {
            (CarStateManager manager, InputManager input, CarSwitcher cars) = Create();
            Vector3 start = manager.Position;
            input.AccelerationInput = direction;
            manager.ReadInputAndUpdateState(0.5);
            manager.Apply();
            AssertNear(cars.CurrentCar.Node.AppliedRotation * Vector3.Forward, (manager.Position - start).Normalized());
        }
    }

    private static void VerifyRotatedSpawn() {
        (CarStateManager manager, InputManager input, _) = Create(-Mathf.Pi / 2f);
        Vector3 start = manager.Position;
        input.AccelerationInput = Vector2.Right;
        manager.ReadInputAndUpdateState(0.5);
        AssertNear(manager.Position - start, Vector3.Right);
    }

    private static void VerifyCameraYaw() {
        float[] cameraAngles = [-Mathf.Pi, -Mathf.Pi / 2f, -Mathf.Pi / 4f, Mathf.Pi / 4f, Mathf.Pi / 2f];
        foreach (float cameraAngle in cameraAngles) {
            (CarStateManager manager, InputManager input, _) = Create(cameraAngle, cameraAngle);
            Vector3 start = manager.Position;
            input.AccelerationInput = new Vector2(0f, 1f);
            manager.ReadInputAndUpdateState(0.5);
            // Independent expectation in native Godot units; never call the planar helpers here.
            AssertNear(manager.Position - start, new Quaternion(Vector3.Up, cameraAngle) * Vector3.Forward);
        }
    }

    private static void VerifyBraking() {
        (CarStateManager manager, InputManager input, CarSwitcher cars) = Create();
        input.AccelerationInput = Vector2.Right;
        manager.ReadInputAndUpdateState(1.0);
        input.Brake = 1f;
        Vector3 beforeBraking = manager.Position;
        manager.ReadInputAndUpdateState(0.125);
        AssertNear(manager.Position - beforeBraking, new Vector3(0.125f, 0f, 0f));
        manager.Apply();
        AssertNear(cars.CurrentCar.Node.AppliedRotation * Vector3.Forward, Vector3.Right);
        Vector3 beforeStopping = manager.Position;
        manager.ReadInputAndUpdateState(1.0);
        input.Brake = 0f;
        input.AccelerationInput = Vector2.Zero;
        manager.ReadInputAndUpdateState(1.0);
        manager.Apply();
        AssertNear(manager.Position, beforeStopping);
        AssertNear(cars.CurrentCar.Node.AppliedRotation * Vector3.Forward, Vector3.Right);
    }

    private static void VerifySpeedLimit() {
        (CarStateManager manager, InputManager input, CarSwitcher cars) = Create();
        input.AccelerationInput = new Vector2(0f, 1f);
        manager.ReadInputAndUpdateState(1.0);
        input.AccelerationInput = Vector2.Zero;
        cars.CurrentCar.Dynamic.VelocityLimiter = 1f;
        Vector3 start = manager.Position;
        manager.ReadInputAndUpdateState(0.5);
        AssertNear(manager.Position - start, Vector3.Forward * 0.5f);
    }

    private static void VerifyReset() {
        const float startingYaw = 0.7f;
        (CarStateManager manager, InputManager input, CarSwitcher cars) = Create(startingYaw);
        Vector3 start = manager.Position;
        Vector3 expectedHeading = new Quaternion(Vector3.Up, startingYaw) * Vector3.Forward;
        AssertNear(cars.CurrentCar.Node.AppliedRotation * Vector3.Forward, expectedHeading);
        input.AccelerationInput = Vector2.Right;
        manager.ReadInputAndUpdateState(1.0);
        manager.Reset_PositionRotationVelocity();
        input.AccelerationInput = Vector2.Zero;
        manager.ReadInputAndUpdateState(1.0);
        manager.Apply();
        AssertNear(cars.CurrentCar.Node.Position, start);
        AssertNear(cars.CurrentCar.Node.AppliedRotation * Vector3.Forward, expectedHeading);
    }

    private static void VerifyWorldSpawn() {
        TrackObjects track = new();
        track.PlaceholderCarNode.GlobalPosition = new Vector3(8f, 0f, -3f);
        track.PlaceholderCarNode.GlobalRotation = new Vector3(0f, 0.7f, 0f);
        CarSwitcher cars = new();
        CarStateManager manager = new(cars, new CameraYawManager(track), new InputManager(), track);
        AssertNear(manager.Position, track.PlaceholderCarNode.GlobalPosition);
        AssertNear(cars.CurrentCar.Node.AppliedRotation * Vector3.Forward, new Quaternion(Vector3.Up, 0.7f) * Vector3.Forward);
    }

    private static (CarStateManager Manager, InputManager Input, CarSwitcher Cars) Create(float startingYaw = 0f, float cameraYaw = 0f) {
        TrackObjects track = new();
        track.PlaceholderCarNode.Position = new Vector3(3f, 0f, 5f);
        track.PlaceholderCarNode.Rotation = new Vector3(0f, startingYaw, 0f);
        track.PlaceholderCarNode.GlobalPosition = track.PlaceholderCarNode.Position;
        track.PlaceholderCarNode.GlobalRotation = track.PlaceholderCarNode.Rotation;
        track.CameraPanAndYaw.GlobalRotation = new Vector3(0f, cameraYaw, 0f);
        // Child rotations must not affect the driving reference supplied by the pivot.
        track.Camera.GlobalRotation = new Vector3(-Mathf.Pi / 4f, cameraYaw + 0.3f, 0f);
        InputManager input = new();
        CarSwitcher cars = new();
        return (new CarStateManager(cars, new CameraYawManager(track), input, track), input, cars);
    }

    private static void AssertNear(Vector3 actual, Vector3 expected) {
        if ((actual - expected).Length() > 0.00001f) {
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
        }
    }
}
