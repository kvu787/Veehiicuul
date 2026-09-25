using Godot;
using System;
using System.Collections.Generic;

namespace Veehiicuul_Godot_CSharp;

/// <summary>Duplicates scene car models and selects the current one synchronously.</summary>
public sealed class CarSwitcher {
    private InputManager InputManager { get; }
    private int CurrentCarIndex { get; set; }
    private List<Car> Cars { get; }
    private Car CurrentCar => this.Cars[this.CurrentCarIndex];

    public CarSwitcher(Node3D currentTrackScene, TrackJson currentTrackJson, InputManager inputManager) {
        ArgumentNullException.ThrowIfNull(currentTrackScene);
        ArgumentNullException.ThrowIfNull(currentTrackJson);
        ArgumentNullException.ThrowIfNull(inputManager);
        this.InputManager = inputManager;
        this.CurrentCarIndex = currentTrackJson.StartCarIndex;
        this.Cars = currentTrackJson.Cars;
        if (this.Cars.Count == 0 || this.CurrentCarIndex < 0 || this.CurrentCarIndex >= this.Cars.Count) {
            throw new InvalidOperationException("The track must declare cars and a valid StartCarIndex.");
        }

        foreach (Car car in this.Cars) {
            if (string.IsNullOrWhiteSpace(car.GameObjectName)) {
                throw new InvalidOperationException("Every car must name its decorative Node3D in GameObjectName.");
            }
            Node3D decorativeNode = currentTrackScene.FindChild(car.GameObjectName, true, false) as Node3D
                ?? throw new InvalidOperationException($"Car model Node3D '{car.GameObjectName}' was not found in '{currentTrackScene.Name}'.");

            // Zero flags exclude duplicated scripts and signals; Main owns all game execution.
            Node3D instance = decorativeNode.Duplicate(0) as Node3D
                ?? throw new InvalidOperationException($"Could not duplicate car model '{car.GameObjectName}'.");
            instance.Name = $"{car.GameObjectName}Player";
            instance.ProcessMode = Godot.Node.ProcessModeEnum.Disabled;
            currentTrackScene.AddChild(instance);
            instance.Scale = Vector3.One * (currentTrackJson.CarScale > 0f ? currentTrackJson.CarScale : 1f);
            instance.Visible = false;
            car.Node = instance;
        }
        this.CurrentCarTransform.Visible = true;
    }

    public Node3D CurrentCarTransform => this.CurrentCar.Node
        ?? throw new InvalidOperationException("The selected car has not been instantiated.");

    public CarDynamic CurrentCarDynamic => this.CurrentCar.Dynamic;

    public bool ReadInputAndSwitchCar() {
        if (this.InputManager.PreviousCar == this.InputManager.NextCar) {
            return false;
        }

        this.CurrentCarTransform.Visible = false;
        this.CurrentCarIndex = this.InputManager.NextCar
            ? this.CurrentCarIndex.CycleNext(this.Cars.Count)
            : this.CurrentCarIndex.CyclePrev(this.Cars.Count);
        this.CurrentCarTransform.Visible = true;
        return true;
    }
}
