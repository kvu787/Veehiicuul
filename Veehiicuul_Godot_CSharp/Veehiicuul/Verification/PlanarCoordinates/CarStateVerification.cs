using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

internal static class CarStateVerification {
    public static void Run() {
        (string Name, Action Verify)[] checks = [
            ("Neutral input preserves motion", VerifyCoasting),
            ("Applied heading follows velocity", VerifyHeading),
            ("Rotated spawn uses forward acceleration", VerifyRotatedSpawn),
            ("Model axes select distinct forward, reverse, left, and right strengths", VerifyModelAcceleration),
            ("Forward acceleration stays aligned over successive updates", VerifySuccessiveForwardAcceleration),
            ("Camera pivot yaw rotates input independently of camera children", VerifyCameraYaw),
            ("Braking slows without reversing and preserves heading", VerifyBraking),
            ("Neutral input still enforces the speed limit", VerifySpeedLimit),
            ("Reset restores the authored pose and clears velocity", VerifyReset),
            ("Spawn, input, and reset use track space independently of world transforms", VerifyTrackSpace)
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
            AssertNear(cars.CurrentCar.Node.Quaternion * Vector3.ModelFront, (manager.Position - start).Normalized());
        }
    }

    private static void VerifyRotatedSpawn() {
        (CarStateManager manager, InputManager input, _) = Create(Mathf.Pi / 2f);
        Vector3 start = manager.Position;
        input.AccelerationInput = Vector2.Right;
        manager.ReadInputAndUpdateState(0.5);
        AssertNear(manager.Position - start, Vector3.Right);
    }

    private static void VerifyModelAcceleration() {
        (Vector3 Direction, float Strength)[] directions = [
            (Vector3.ModelFront, 4f),
            (Vector3.ModelRear, 8f),
            (Vector3.ModelLeft, 2f),
            (Vector3.ModelRight, 6f)
        ];
        float[] startingAngles = [0f, Mathf.Pi / 2f, Mathf.Pi, -Mathf.Pi / 2f, 0.7f];
        const float cameraYaw = -0.35f;
        foreach (float startingYaw in startingAngles) {
            foreach ((Vector3 direction, float strength) in directions) {
                (CarStateManager manager, InputManager input, CarSwitcher cars) = Create(startingYaw, cameraYaw);
                cars.CurrentCar.Dynamic.AccelerationMap.Right = 6f;
                Vector3 trackDirection = new Quaternion(Vector3.Up, startingYaw) * direction;
                Vector3 cameraDirection = new Quaternion(Vector3.Up, -cameraYaw) * trackDirection;
                input.AccelerationInput = new Vector2(cameraDirection.X, -cameraDirection.Z);
                Vector3 start = manager.Position;
                manager.ReadInputAndUpdateState(0.5);
                AssertNear(manager.Position - start, trackDirection * (strength * 0.25f));
                manager.Apply();
                AssertNear(cars.CurrentCar.Node.Quaternion * Vector3.ModelFront, trackDirection);
            }
        }
    }

    private static void VerifySuccessiveForwardAcceleration() {
        const float startingYaw = 0.7f;
        (CarStateManager manager, InputManager input, CarSwitcher cars) = Create(startingYaw);
        Vector3 trackFront = new Quaternion(Vector3.Up, startingYaw) * Vector3.ModelFront;
        input.AccelerationInput = new Vector2(trackFront.X, -trackFront.Z);
        Vector3 start = manager.Position;
        manager.ReadInputAndUpdateState(0.5);
        manager.ReadInputAndUpdateState(0.5);
        manager.Apply();
        AssertNear(manager.Position - start, trackFront * 3f);
        AssertNear(cars.CurrentCar.Node.Quaternion * Vector3.ModelFront, trackFront);
    }

    private static void VerifyCameraYaw() {
        float[] cameraAngles = [-Mathf.Pi, -Mathf.Pi / 2f, -Mathf.Pi / 4f, Mathf.Pi / 4f, Mathf.Pi / 2f];
        foreach (float cameraAngle in cameraAngles) {
            // Face the model's +Z in the same direction as the camera pivot's -Z.
            (CarStateManager manager, InputManager input, _) = Create(cameraAngle + Mathf.Pi, cameraAngle);
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
        AssertNear(cars.CurrentCar.Node.Quaternion * Vector3.ModelFront, Vector3.Right);
        Vector3 beforeStopping = manager.Position;
        manager.ReadInputAndUpdateState(1.0);
        input.Brake = 0f;
        input.AccelerationInput = Vector2.Zero;
        manager.ReadInputAndUpdateState(1.0);
        manager.Apply();
        AssertNear(manager.Position, beforeStopping);
        AssertNear(cars.CurrentCar.Node.Quaternion * Vector3.ModelFront, Vector3.Right);
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
        Vector3 expectedHeading = new Quaternion(Vector3.Up, startingYaw) * Vector3.ModelFront;
        AssertNear(cars.CurrentCar.Node.Quaternion * Vector3.ModelFront, expectedHeading);
        input.AccelerationInput = Vector2.Right;
        manager.ReadInputAndUpdateState(1.0);
        manager.Reset_PositionRotationVelocity();
        input.AccelerationInput = Vector2.Zero;
        manager.ReadInputAndUpdateState(1.0);
        manager.Apply();
        AssertNear(cars.CurrentCar.Node.Position, start);
        AssertNear(cars.CurrentCar.Node.Quaternion * Vector3.ModelFront, expectedHeading);
    }

    private static void VerifyTrackSpace() {
        TrackObjects track = new();
        const float parentYaw = 0.4f;
        const float startingYaw = 0.7f;
        const float cameraYaw = -0.35f;
        Transform3D trackToWorld = new(new Basis(Vector3.Up, parentYaw), new Vector3(8f, 0f, -3f));
        track.PlaceholderCarNode.Position = new Vector3(3f, 0f, 5f);
        track.PlaceholderCarNode.Rotation = new Vector3(0f, startingYaw, 0f);
        track.PlaceholderCarNode.GlobalPosition = trackToWorld * track.PlaceholderCarNode.Position;
        track.PlaceholderCarNode.GlobalRotation = new Vector3(0f, parentYaw + startingYaw, 0f);
        track.CameraPanAndYaw.Rotation = new Vector3(0f, cameraYaw, 0f);
        track.CameraPanAndYaw.GlobalRotation = new Vector3(0f, parentYaw + cameraYaw, 0f);
        CarSwitcher cars = new();
        InputManager input = new();
        CarStateManager manager = new(cars, new CameraYawManager(track), input, track);
        Vector3 start = track.PlaceholderCarNode.Position;
        Vector3 trackFront = new Quaternion(Vector3.Up, startingYaw) * Vector3.ModelFront;
        AssertNear(manager.Position, start);
        AssertNear(cars.CurrentCar.Node.Position, start);
        AssertNear(cars.CurrentCar.Node.Quaternion * Vector3.ModelFront, trackFront);

        Vector3 cameraDirection = new Quaternion(Vector3.Up, -cameraYaw) * trackFront;
        input.AccelerationInput = new Vector2(cameraDirection.X, -cameraDirection.Z);
        manager.ReadInputAndUpdateState(0.5);
        manager.Apply();
        AssertNear(cars.CurrentCar.Node.Position - start, trackFront);

        manager.Reset_PositionRotationVelocity();
        input.AccelerationInput = Vector2.Zero;
        manager.ReadInputAndUpdateState(1.0);
        manager.Apply();
        AssertNear(cars.CurrentCar.Node.Position, start);
        AssertNear(cars.CurrentCar.Node.Quaternion * Vector3.ModelFront, trackFront);
    }

    // By default, face model front (+Z) along camera forward (-Z).
    private static (CarStateManager Manager, InputManager Input, CarSwitcher Cars) Create(float startingYaw = Mathf.Pi, float cameraYaw = 0f) {
        TrackObjects track = new();
        track.PlaceholderCarNode.Position = new Vector3(3f, 0f, 5f);
        track.PlaceholderCarNode.Rotation = new Vector3(0f, startingYaw, 0f);
        track.PlaceholderCarNode.GlobalPosition = track.PlaceholderCarNode.Position;
        track.PlaceholderCarNode.GlobalRotation = track.PlaceholderCarNode.Rotation;
        track.CameraPanAndYaw.GlobalRotation = new Vector3(0f, cameraYaw, 0f);
        track.CameraPanAndYaw.Rotation = track.CameraPanAndYaw.GlobalRotation;
        // Child rotations must not affect the driving reference supplied by the pivot.
        track.Camera.GlobalRotation = new Vector3(-Mathf.Pi / 4f, cameraYaw + 0.3f, 0f);
        InputManager input = new();
        CarSwitcher cars = new();
        return (new CarStateManager(cars, new CameraYawManager(track), input, track), input, cars);
    }

    private static void AssertNear(Vector3 actual, Vector3 expected) {
        if (!((actual - expected).Length() <= 0.00001f)) {
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
        }
    }
}
