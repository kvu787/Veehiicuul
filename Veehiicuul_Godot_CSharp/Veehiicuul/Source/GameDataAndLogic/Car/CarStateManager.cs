using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

/// <summary>Integrates the source game's planar acceleration, braking, and heading.</summary>
public sealed class CarStateManager {
    // Hardware deadzones vary by controller; the source chose 0.05 to allow for
    // both controller noise and thumb precision. Apply this in car space, not
    // before rotating input: forward/reverse/left/right have different strengths.
    private const float AxialDeadzoneInner = 0.05f;
    private const float AxialDeadzoneOuter = 0.95f;

    private readonly CarSwitcher CarSwitcher;
    private readonly CameraYawManager CameraYawManager;
    private readonly InputManager InputManager;

    private readonly Vector3 StartingPosition;
    // Car yaw uses native Godot radians from model front (+Z), relative to the track.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0032:Use auto property", Justification = "Readability")]
    private readonly float StartingRotation;

    private float? Rotation_ForMostRecentNonZeroVelocity;
    private Vector3 Velocity;

    private float Rotation => this.Rotation_ForMostRecentNonZeroVelocity ?? this.StartingRotation;

    /// <summary>Car position relative to the track root; Model has an identity local transform.</summary>
    public Vector3 Position { get; private set; }

    public CarStateManager(CarSwitcher carSwitcher, CameraYawManager cameraYawManager, InputManager inputManager, TrackObjects trackObjects) {
        ArgumentNullException.ThrowIfNull(trackObjects);
        ArgumentNullException.ThrowIfNull(cameraYawManager);
        ArgumentNullException.ThrowIfNull(carSwitcher);
        ArgumentNullException.ThrowIfNull(inputManager);

        this.CarSwitcher = carSwitcher;
        this.CameraYawManager = cameraYawManager;
        this.InputManager = inputManager;

        this.StartingPosition = trackObjects.PlaceholderCarNode.Position;
        this.StartingRotation = trackObjects.PlaceholderCarNode.Rotation.Y;

        this.Reset_PositionRotationVelocity();
        this.Apply();
    }

    public void ReadInputAndUpdateState(double delta) {
        // Neutral input must still integrate velocity so the car continues coasting.
        Vector2 accelerationInput = this.InputManager.AccelerationInput;
        CarDynamic carDynamic = this.CarSwitcher.CurrentCar.Dynamic;

        if (this.InputManager.Brake == 0f) {
            // Stick-up is +Y; the camera pivot's forward direction is local -Z.
            Vector3 accelerationInput_xzPlane = new(accelerationInput.X, 0f, -accelerationInput.Y);
            Vector3 accelerationInput_trackSpace = accelerationInput_xzPlane.Rotated(Vector3.Up, this.CameraYawManager.Yaw);

            Vector3 accelerationInput_carSpace = accelerationInput_trackSpace.Rotated(Vector3.Up, -1f * this.Rotation);
            accelerationInput_carSpace.X = InputUtility.AxialDeadzone(accelerationInput_carSpace.X, AxialDeadzoneInner, AxialDeadzoneOuter);
            accelerationInput_carSpace.Y = 0f;
            accelerationInput_carSpace.Z = InputUtility.AxialDeadzone(accelerationInput_carSpace.Z, AxialDeadzoneInner, AxialDeadzoneOuter);
            accelerationInput_carSpace = accelerationInput_carSpace.LimitLength(1f);

            if (accelerationInput_carSpace != Vector3.Zero) {
                // Imported cars use model front (+Z) and model right (-X).
                Vector3 accelerationOutput_carSpace = new(
                    accelerationInput_carSpace.X * (accelerationInput_carSpace.X < 0f ? carDynamic.AccelerationMap.Right : carDynamic.AccelerationMap.Left),
                    0f,
                    accelerationInput_carSpace.Z * (accelerationInput_carSpace.Z < 0f ? carDynamic.AccelerationMap.Reverse : carDynamic.AccelerationMap.Forward)
                );

                Vector3 accelerationOutput_trackSpace = accelerationOutput_carSpace.Rotated(Vector3.Up, this.Rotation);
                Vector3 deltaVelocity_trackSpace = (float)delta * accelerationOutput_trackSpace;
                this.Velocity += deltaVelocity_trackSpace;
            } else {
                // Brake and acceleration are zero, so do nothing
            }
        } else {
            if (this.Velocity == Vector3.Zero) {
                // Brake is non-zero, but velocity is already zero, so do nothing
            } else {
                float velocityLengthSquared = this.Velocity.LengthSquared();
                if (velocityLengthSquared < 0.0001f) {
                    this.Velocity = Vector3.Zero;
                } else {
                    Vector3 brakeDirection = -this.Velocity.Normalized();
                    Vector3 brakeDeltaVelocity = carDynamic.AccelerationMap.Reverse * this.InputManager.Brake * (float)delta * brakeDirection;
                    if (brakeDeltaVelocity.LengthSquared() >= velocityLengthSquared) {
                        this.Velocity = Vector3.Zero;
                    } else {
                        this.Velocity += brakeDeltaVelocity;
                    }
                }
            }
        }

        if (carDynamic.VelocityLimiter > 0f) {
            this.Velocity = this.Velocity.LimitLength(carDynamic.VelocityLimiter);
        }

        if (this.Velocity != Vector3.Zero) {
            this.Rotation_ForMostRecentNonZeroVelocity = this.Velocity.Get2DRotation();
        }

        this.Position += this.Velocity * (float)delta;
    }

    public void Reset_PositionRotationVelocity() {
        this.Position = this.StartingPosition;
        this.Rotation_ForMostRecentNonZeroVelocity = null;
        this.Velocity = Vector3.Zero;
    }

    public void Apply() {
        this.CarSwitcher.CurrentCar.Node!.Position = this.Position;
        this.CarSwitcher.CurrentCar.Node!.Rotation = new Vector3(0f, this.Rotation, 0f);
    }
}
