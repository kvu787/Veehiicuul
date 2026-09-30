using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class CarStateManager {
    /// <summary>
    /// A "hardware deadzone" refers to deadzone processing done by the controller hardware prior to sending input data via USB or wireless connection to the PC.
    /// A "game deadzone" refers to deadzone processing by the game software via Unity built-in code or this custom code.
    ///
    /// 1. For an 8BitDo Ultimate 2 Wireless controller:
    ///    * This controller has hardware deadzones that are enabled by default.
    ///    * The at-rest analog stick value is always reported as 0.0000152587890625 for both X and Y.
    ///      * (I don't know why it is 0.0000152587890625 instead of just 0.0.)
    ///    * Therefore, if the hardware deadzone is enabled and set high enough, then a minimum safe game inner deadzone is 0.0001, which is effectively 0.0.
    ///    * However, if the hardware deadzone is disabled or enabled but set low enough, then you will need a high enough game inner deadzone.
    ///
    /// 2. For a Razer Wolverine Pro 8K PC controller:
    ///    * Turn off "Prevent Double Deadzones".
    ///    * Same as 1.
    ///
    /// 3. For a Gamesir G7 Pro 8K PC controller:
    ///    * Same as 1.
    ///
    /// 4. For a standard Xbox Series controller:
    ///    * This controller does not have hardware deadzones.
    ///    * This means the at-rest analog stick value will bounce around from 0.00 to +/-0.02.
    ///    * A minimum safe game inner deadzone is 0.03.
    ///
    /// 5. For a standard PlayStation 5 DualSense controller:
    ///    * Same as 3.
    ///
    /// However, just because a controller's minimum safe game inner deadzone is N doesn't mean it should be set to N.
    /// I have set the inner deadzone value to 0.05, which is well above all the minimums for the controllers I use,
    /// because my thumb's precision is too janky below 0.05.
    ///
    /// In general, a player should start by setting the inner deadzone to the minimum for their controller.
    /// Then, they should test it out and increase the deadzone in small increments (~0.01) until they have
    /// good control of the thumbstick even at its smallest actuations.
    /// </summary>
    private const float AxialDeadzone_Inner_LeftRight = 0.10f;
    private const float AxialDeadzone_Outer_LeftRight = 0.95f;
    private const float AxialDeadzone_Inner_ForwardBackward = 0.05f;
    private const float AxialDeadzone_Outer_ForwardBackward = 0.95f;

    private readonly CarSwitcher CarSwitcher;
    private readonly CameraYawManager CameraYawManager;
    private readonly InputManager InputManager;

    private readonly Vector3 StartingPosition;
    // Car yaw uses native Godot radians from model front (+Z), relative to the track.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0032:Use auto property", Justification = "Readability")]
    private readonly float StartingRotation;

    private float? Rotation_ForMostRecentNonZeroVelocity;
    private Vector3 Velocity;

    /// <summary>The managed yaw applied to the car; collision queries share this authoritative pose.</summary>
    public float Rotation => this.Rotation_ForMostRecentNonZeroVelocity ?? this.StartingRotation;

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
            accelerationInput_carSpace.X = InputUtility.AxialDeadzone(accelerationInput_carSpace.X, AxialDeadzone_Inner_LeftRight, AxialDeadzone_Outer_LeftRight);
            accelerationInput_carSpace.Y = 0f;
            accelerationInput_carSpace.Z = InputUtility.AxialDeadzone(accelerationInput_carSpace.Z, AxialDeadzone_Inner_ForwardBackward, AxialDeadzone_Outer_ForwardBackward);
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
