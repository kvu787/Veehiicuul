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
    private float StartingRotation { get; }

    private float? Rotation_ForMostRecentNonZeroVelocity;
    private Vector3 Velocity_WorldSpace;

    /// <summary>
    /// "WorldSpace" means 0 deg starts at +Z and positive Rotation travels clockwise from +Z to +X.
    /// </summary>
    public float Rotation_WorldSpace => this.Rotation_ForMostRecentNonZeroVelocity ?? this.StartingRotation;

    /// <summary>
    /// "WorldSpace" means the Godot axes of +Y up, -Z forward, +X right.
    /// </summary>
    public Vector3 Position_WorldSpace { get; private set; }

    public CarStateManager(CarSwitcher carSwitcher, CameraYawManager cameraYawManager, InputManager inputManager, TrackNodes trackObjects) {
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
        this.ApplyInternalStateToCarTransform();
    }

    public void ReadAccelerationAndBrakeInput_AndUpdateVelocity(double delta) {
        Vector2 accelerationInput = this.InputManager.AccelerationInput;
        CarDynamic carDynamic = this.CarSwitcher.CurrentCar.Dynamic;

        if (this.InputManager.Brake == 0f) {
            // Translate stick input from controller space to car space
            Vector3 accelerationInput_cameraSpace = new(accelerationInput.X, 0f, -accelerationInput.Y);
            Vector3 accelerationInput_worldSpace = accelerationInput_cameraSpace.Rotated(Vector3.Up, this.CameraYawManager.Yaw);
            Vector3 accelerationInput_carSpace = accelerationInput_worldSpace.Rotated(Vector3.Up, -1f * this.Rotation_WorldSpace);

            // Apply axial deadzones and clamp length
            accelerationInput_carSpace.X = InputUtility.AxialDeadzone(accelerationInput_carSpace.X, AxialDeadzone_Inner_LeftRight, AxialDeadzone_Outer_LeftRight);
            accelerationInput_carSpace.Y = 0f;
            accelerationInput_carSpace.Z = InputUtility.AxialDeadzone(accelerationInput_carSpace.Z, AxialDeadzone_Inner_ForwardBackward, AxialDeadzone_Outer_ForwardBackward);
            accelerationInput_carSpace = accelerationInput_carSpace.LimitLength(1f);

            if (accelerationInput_carSpace != Vector3.Zero) {
                // Apply acceleration map
                Vector3 accelerationOutput_carSpace = new(
                    accelerationInput_carSpace.X * (accelerationInput_carSpace.X < 0f ? carDynamic.AccelerationMap.Right : carDynamic.AccelerationMap.Left),
                    0f,
                    accelerationInput_carSpace.Z * (accelerationInput_carSpace.Z < 0f ? carDynamic.AccelerationMap.Reverse : carDynamic.AccelerationMap.Forward)
                );

                // Translate acceleration to world space
                Vector3 accelerationOutput_worldSpace = accelerationOutput_carSpace.Rotated(Vector3.Up, this.Rotation_WorldSpace);

                // Apply acceleration to velocity
                this.Velocity_WorldSpace += (float)delta * accelerationOutput_worldSpace;
            } else {
                // Brake and acceleration are zero, so do nothing
            }
        } else {
            if (this.Velocity_WorldSpace == Vector3.Zero) {
                // Brake is non-zero, but velocity is already zero, so do nothing
            } else {
                float velocityLengthSquared = this.Velocity_WorldSpace.LengthSquared();
                if (velocityLengthSquared < 0.0001f) {
                    this.Velocity_WorldSpace = Vector3.Zero;
                } else {
                    Vector3 brakeDirection = -this.Velocity_WorldSpace.Normalized();
                    Vector3 brakeDeltaVelocity = carDynamic.AccelerationMap.Reverse * this.InputManager.Brake * (float)delta * brakeDirection;
                    if (brakeDeltaVelocity.LengthSquared() >= velocityLengthSquared) {
                        this.Velocity_WorldSpace = Vector3.Zero;
                    } else {
                        this.Velocity_WorldSpace += brakeDeltaVelocity;
                    }
                }
            }
        }

        if (carDynamic.VelocityLimiter > 0f) {
            this.Velocity_WorldSpace = this.Velocity_WorldSpace.LimitLength(carDynamic.VelocityLimiter);
        }

        if (this.Velocity_WorldSpace != Vector3.Zero) {
            this.Rotation_ForMostRecentNonZeroVelocity = this.Velocity_WorldSpace.Get2DRotation();
        }
    }

    public void UpdatePosition(double delta) {
        this.Position_WorldSpace += this.Velocity_WorldSpace * (float)delta;
    }

    public void Reset_PositionRotationVelocity() {
        this.Position_WorldSpace = this.StartingPosition;
        this.Rotation_ForMostRecentNonZeroVelocity = null;
        this.Velocity_WorldSpace = Vector3.Zero;
    }

    public void ApplyInternalStateToCarTransform() {
        this.CarSwitcher.CurrentCar.Node!.Position = this.Position_WorldSpace;
        this.CarSwitcher.CurrentCar.Node!.Rotation = new Vector3(0f, this.Rotation_WorldSpace, 0f);
    }
}
