using Godot;
using System;
using System.Collections.Generic;

namespace Veehiicuul_Godot_CSharp;

public sealed class CarSwitcher {
    private InputManager InputManager { get; }
    private int CurrentCarIndex { get; set; }
    private List<Car> Cars { get; }
    private Car CurrentCar => this.Cars[this.CurrentCarIndex];

    public CarSwitcher(InputManager inputManager, TrackSwitcher trackSwitcher) {
        ArgumentNullException.ThrowIfNull(inputManager);
        ArgumentNullException.ThrowIfNull(trackSwitcher);

        this.InputManager = inputManager;

        this.CurrentCarIndex = trackSwitcher.CurrentTrackJson.StartCarIndex;
        this.Cars = trackSwitcher.CurrentTrackJson.Cars;
        this.CurrentCarNode.Visible = true;
    }

    public Node3D CurrentCarNode => this.CurrentCar.Node!;

    public CarDynamic CurrentCarDynamic => this.CurrentCar.Dynamic;

    public bool ReadInputAndSwitchCar() {
        if (this.InputManager.PreviousCar == this.InputManager.NextCar) {
            return false;
        } else {
            this.CurrentCarNode.Visible = false;
            this.CurrentCarIndex = this.InputManager.NextCar ? this.CurrentCarIndex.CycleNext(this.Cars.Count) : this.CurrentCarIndex.CyclePrev(this.Cars.Count);
            this.CurrentCarNode.Visible = true;
            return true;
        }
    }
}
