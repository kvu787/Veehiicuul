using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

/// <summary>Integrates the source game's planar acceleration, braking, and heading.</summary>
public sealed class CarState {
    // Hardware deadzones vary by controller; the source chose 0.05 to allow for
    // both controller noise and thumb precision. Apply this in car space, not
    // before rotating input: forward/reverse/left/right have different strengths.
    private const float AxialDeadzoneInner = 0.05f;
    private const float AxialDeadzoneOuter = 0.95f;

    private CarSwitcher CarSwitcher { get; }
    private CameraController CameraController { get; }
    private InputManager InputManager { get; }
    private TimeManager TimeManager { get; }
    private Vector3 StartingPosition { get; }
    private float StartingRotation { get; }
    private float? Rotation_ForMostRecentNonZeroVelocity { get; set; }
    private Vector3 Velocity { get; set; }
    private float Rotation => this.Rotation_ForMostRecentNonZeroVelocity ?? this.StartingRotation;

    public Vector3 Position { get; private set; }

    public CarState(Node3D placeholderCarTransform, CarSwitcher carSwitcher, CameraController cameraController, InputManager inputManager, TimeManager timeManager) {
        ArgumentNullException.ThrowIfNull(placeholderCarTransform);
        ArgumentNullException.ThrowIfNull(carSwitcher);
        ArgumentNullException.ThrowIfNull(cameraController);
        ArgumentNullException.ThrowIfNull(inputManager);
        ArgumentNullException.ThrowIfNull(timeManager);
        this.CarSwitcher = carSwitcher;
        this.CameraController = cameraController;
        this.InputManager = inputManager;
        this.TimeManager = timeManager;
        this.StartingPosition = placeholderCarTransform.GlobalPosition;
        this.StartingRotation = -Mathf.RadToDeg(placeholderCarTransform.GlobalRotation.Y);
        this.Reset_PositionRotationVelocity();
    }

    public void ReadInputAndUpdateState() {
        if (!this.InputManager.HasGamepad) {
            return;
        }

        float brakeInput = this.InputManager.Brake;
        Vector2 accelerationInput = this.InputManager.AccelerationInput;
        CarDynamic carDynamic = this.CarSwitcher.CurrentCarDynamic;
        float cameraYaw = this.CameraController.CameraYaw;

        if (brakeInput == 0f) {
            // The input snapshot reports stick-up as +Y; world-forward is -Z.
            Vector3 accelerationInputPlanar = new(accelerationInput.X, 0f, -accelerationInput.Y);
            Vector3 accelerationInputWorld = accelerationInputPlanar.Rotate2D(cameraYaw);
            Vector3 accelerationInputCar = accelerationInputWorld.Rotate2D(-this.Rotation);
            accelerationInputCar.X = InputUtility.AxialDeadzone(accelerationInputCar.X, AxialDeadzoneInner, AxialDeadzoneOuter);
            accelerationInputCar.Y = 0f;
            accelerationInputCar.Z = InputUtility.AxialDeadzone(accelerationInputCar.Z, AxialDeadzoneInner, AxialDeadzoneOuter);
            accelerationInputCar = accelerationInputCar.LimitLength(1f);

            if (accelerationInputCar != Vector3.Zero) {
                Vector3 accelerationOutputCar = new(
                    accelerationInputCar.X * (accelerationInputCar.X > 0f ? carDynamic.AccelerationMap.Right : carDynamic.AccelerationMap.Left),
                    0f,
                    accelerationInputCar.Z * (accelerationInputCar.Z < 0f ? carDynamic.AccelerationMap.Forward : carDynamic.AccelerationMap.Reverse));
                Vector3 accelerationOutputWorld = accelerationOutputCar.Rotate2D(this.Rotation);
                this.Velocity += this.TimeManager.DeltaTime * accelerationOutputWorld;
            }
        } else if (this.Velocity != Vector3.Zero) {
            float velocityLengthSquared = this.Velocity.LengthSquared();
            if (velocityLengthSquared < 0.0001f) {
                this.Velocity = Vector3.Zero;
            } else {
                Vector3 brakeDirection = -this.Velocity.Normalized();
                Vector3 brakeDeltaVelocity = carDynamic.AccelerationMap.Reverse * brakeInput * this.TimeManager.DeltaTime * brakeDirection;
                if (brakeDeltaVelocity.LengthSquared() >= velocityLengthSquared) {
                    this.Velocity = Vector3.Zero;
                } else {
                    this.Velocity += brakeDeltaVelocity;
                }
            }
        }

        if (carDynamic.VelocityLimiter > 0f) {
            this.Velocity = this.Velocity.LimitLength(carDynamic.VelocityLimiter);
        }
        if (this.Velocity != Vector3.Zero) {
            this.Rotation_ForMostRecentNonZeroVelocity = this.Velocity.Get2DRotation();
        }
        this.Position += this.Velocity * this.TimeManager.DeltaTime;
    }

    public void ApplyStateToGameObject() {
        this.CarSwitcher.CurrentCarTransform.SetPositionAndRotation(
            this.Position, new Quaternion(Vector3.Up, -Mathf.DegToRad(this.Rotation)));
    }

    public void Reset_PositionRotationVelocity() {
        this.Position = this.StartingPosition;
        this.Rotation_ForMostRecentNonZeroVelocity = null;
        this.Velocity = Vector3.Zero;
    }
}
