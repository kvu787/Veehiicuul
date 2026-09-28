using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class TrackSwitcher {
    private readonly Node MainNode;
    private readonly InputManager InputManager;
    private readonly string[] TrackNames;

    private int CurrentTrackIndex;
    public TrackJson CurrentTrackJson { get; private set; }
    public Node CurrentTrackScene { get; private set; }

    public TrackSwitcher(Node mainNode, InputManager inputManager, string[] trackNames, int initialTrackIndex) {
        ArgumentNullException.ThrowIfNull(mainNode);
        ArgumentNullException.ThrowIfNull(inputManager);
        ArgumentNullException.ThrowIfNull(trackNames);
        if (initialTrackIndex < 0 || initialTrackIndex >= trackNames.Length) {
            throw new ArgumentOutOfRangeException(nameof(initialTrackIndex), "Must be a valid index for the trackNames list");
        }

        this.MainNode = mainNode;
        this.InputManager = inputManager;
        this.TrackNames = trackNames;

        this.CurrentTrackIndex = initialTrackIndex;
        this.CurrentTrackJson = this.ReadCurrentTrackJson();

        if (this.CurrentTrackJson.Cars.Count == 0) {
            throw new InvalidOperationException("The track must define at least 1 car");
        }
        if (this.CurrentTrackJson.StartCarIndex < 0
            || this.CurrentTrackJson.StartCarIndex >= this.CurrentTrackJson.Cars.Count) {
            throw new InvalidOperationException("The track must define a valid StartCarIndex");
        }

        this.CurrentTrackScene = this.LoadCurrentTrackScene();
    }

    public string CurrentTrackName => this.TrackNames[this.CurrentTrackIndex];

    public bool ReadInputAndSwitchTracks() {
        if (this.InputManager.PreviousTrack == this.InputManager.NextTrack) {
            return false;
        } else {
            this.CurrentTrackScene.Free();

            if (this.InputManager.PreviousTrack) {
                this.CurrentTrackIndex = this.CurrentTrackIndex.CyclePrev(this.TrackNames.Length);
            } else /* if (isNextTrack) */ {
                this.CurrentTrackIndex = this.CurrentTrackIndex.CycleNext(this.TrackNames.Length);
            }
            this.CurrentTrackJson = this.ReadCurrentTrackJson();
            this.CurrentTrackScene = this.LoadCurrentTrackScene();
            return true;
        }
    }

    private TrackJson ReadCurrentTrackJson() {
        return JsonUtility.Deserialize<TrackJson>($"res://Tracks/{this.CurrentTrackName}/{this.CurrentTrackName}_Settings.json");
    }

    private Node LoadCurrentTrackScene() {
        return SceneUtility.Load(this.MainNode, $"res://Tracks/{this.CurrentTrackName}/{this.CurrentTrackName}_Scene.tscn");
    }
}
