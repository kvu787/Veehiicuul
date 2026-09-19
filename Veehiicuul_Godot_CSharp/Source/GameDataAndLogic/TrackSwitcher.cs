using Godot;
using System;
using System.Collections.Generic;

namespace Veehiicuul_Godot_CSharp;

public sealed class TrackSwitcher {
    private readonly Node Host;
    private readonly InputManager InputManager;
    private readonly IReadOnlyList<string> TrackNames;
    private int CurrentTrackIndex;

    public string CurrentTrackName => this.TrackNames[this.CurrentTrackIndex];
    public Node3D CurrentTrackScene { get; private set; }
    public TrackJson CurrentTrackJson { get; private set; }

    public TrackSwitcher(Node host, InputManager inputManager, IReadOnlyList<string> trackNames, int currentTrackIndex) {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(inputManager);
        ArgumentNullException.ThrowIfNull(trackNames);
        if (currentTrackIndex < 0 || currentTrackIndex >= trackNames.Count) {
            throw new ArgumentOutOfRangeException(nameof(currentTrackIndex));
        }
        this.Host = host;
        this.InputManager = inputManager;
        this.TrackNames = trackNames;
        this.CurrentTrackIndex = currentTrackIndex;
        this.CurrentTrackJson = JsonUtility.Deserialize<TrackJson>($"{this.CurrentTrackName}.json");
        this.CurrentTrackScene = SceneLoadingUtility.LoadAndAttach<Node3D>(host, $"res://Tracks/{this.CurrentTrackName}.tscn");
    }

    public bool ReadInputAndSwitchTracks() {
        if (this.InputManager.PreviousTrack == this.InputManager.NextTrack) {
            return false;
        }
        int direction = this.InputManager.PreviousTrack ? -1 : 1;
        int nextIndex = (this.CurrentTrackIndex + direction + this.TrackNames.Count) % this.TrackNames.Count;
        string nextName = this.TrackNames[nextIndex];
        TrackJson nextTrackJson = JsonUtility.Deserialize<TrackJson>($"{nextName}.json");

        GD.Print($"Unload track '{this.CurrentTrackName}'...");
        // Immediate destruction is safe because these scenes have no executing scripts. Main
        // rebuilds its track-dependent managers before reading any old node reference again.
        this.Host.RemoveChild(this.CurrentTrackScene);
        this.CurrentTrackScene.Free();
        this.CurrentTrackScene = SceneLoadingUtility.LoadAndAttach<Node3D>(this.Host, $"res://Tracks/{nextName}.tscn");
        this.CurrentTrackIndex = nextIndex;
        this.CurrentTrackJson = nextTrackJson;
        return true;
    }
}
