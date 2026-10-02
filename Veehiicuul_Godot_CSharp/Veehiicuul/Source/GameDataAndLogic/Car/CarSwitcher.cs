using System;
using System.Collections.Generic;

namespace Veehiicuul_Godot_CSharp;

public sealed class CarSwitcher {
    private readonly InputManager InputManager;
    private readonly Car[] Cars;
    public int CurrentCarIndex { get; private set; }
    public IReadOnlyList<Car> AvailableCars => this.Cars;
    public Car CurrentCar => this.Cars[this.CurrentCarIndex];

    public CarSwitcher(InputManager inputManager, TrackSwitcher trackSwitcher) {
        ArgumentNullException.ThrowIfNull(inputManager);
        ArgumentNullException.ThrowIfNull(trackSwitcher);

        this.InputManager = inputManager;

        this.CurrentCarIndex = trackSwitcher.CurrentTrackJson.StartCarIndex;
        this.Cars = trackSwitcher.CurrentTrackJson.Cars;
        this.CurrentCar.Node!.Visible = true;
    }

    public bool ReadInputAndSwitchCar() {
        if (this.InputManager.PreviousCar == this.InputManager.NextCar) {
            return false;
        } else {
            this.CurrentCar.Node!.Visible = false;
            this.CurrentCarIndex = this.InputManager.NextCar ? this.CurrentCarIndex.CycleNext(this.Cars.Length) : this.CurrentCarIndex.CyclePrev(this.Cars.Length);
            this.CurrentCar.Node.Visible = true;
            return true;
        }
    }
}
