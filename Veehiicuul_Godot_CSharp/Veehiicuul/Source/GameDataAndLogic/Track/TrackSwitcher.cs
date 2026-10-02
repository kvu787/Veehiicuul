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
        this.CurrentTrackJson = ReadTrackJson(this.CurrentTrackName);
        ValidateTrackJson(this.CurrentTrackJson);

        this.CurrentTrackScene = this.LoadCurrentTrackScene();
    }

    public string CurrentTrackName => this.TrackNames[this.CurrentTrackIndex];

    public bool ReadInputAndSwitchTracks() {
        if (this.InputManager.PreviousTrack == this.InputManager.NextTrack) {
            return false;
        } else {
            int nextTrackIndex = this.InputManager.PreviousTrack
                ? this.CurrentTrackIndex.CyclePrev(this.TrackNames.Length)
                : this.CurrentTrackIndex.CycleNext(this.TrackNames.Length);
            TrackJson nextTrackJson = ReadTrackJson(this.TrackNames[nextTrackIndex]);
            ValidateTrackJson(nextTrackJson);

            this.CurrentTrackScene.Free();
            this.CurrentTrackIndex = nextTrackIndex;
            this.CurrentTrackJson = nextTrackJson;
            this.CurrentTrackScene = this.LoadCurrentTrackScene();
            return true;
        }
    }

    private static void ValidateTrackJson(TrackJson trackJson) {
        if (trackJson.Cars is null || trackJson.Cars.Count == 0) {
            throw new InvalidOperationException("The track must define at least 1 car");
        }
        if (trackJson.StartCarIndex < 0 || trackJson.StartCarIndex >= trackJson.Cars.Count) {
            throw new InvalidOperationException("The track must define a valid StartCarIndex");
        }
    }

    private static TrackJson ReadTrackJson(string trackName) {
        return JsonUtility.Deserialize<TrackJson>($"res://Tracks/{trackName}/{trackName}_Settings.json");
    }

    private Node LoadCurrentTrackScene() {
        Node trackSceneRootNode = SceneUtility.Load(this.MainNode, $"res://Tracks/{this.CurrentTrackName}/{this.CurrentTrackName}_Scene.tscn");
        ValidateTrackScene(trackSceneRootNode);
        return trackSceneRootNode;
    }

    private static void ValidateTrackScene(Node rootNode) {
        if (rootNode.Name != "Track") {
            throw new InvalidOperationException("Track scene root node must be named 'Track'");
        }
        if (rootNode is not Node3D) {
            throw new InvalidOperationException("Track scene root node must have type='Node3D'");
        }

        RequireTrackNode<WorldEnvironment>(rootNode, "WorldEnvironment");
        RequireTrackNode<Node3D>(rootNode, "CameraPanAndYaw");
        RequireTrackNode<Node3D>(rootNode, "CameraPanAndYaw/CameraYawOffset");
        RequireTrackNode<Node3D>(rootNode, "CameraPanAndYaw/CameraYawOffset/CameraPanOffsetAndPitch");
        RequireTrackNode<Camera3D>(rootNode, "CameraPanAndYaw/CameraYawOffset/CameraPanOffsetAndPitch/Camera");
        RequireTrackNode<DirectionalLight3D>(rootNode, "Sunlight");
        RequireTrackNode<Node3D>(rootNode, "Model");
    }

    private static void RequireTrackNode<T>(Node node, string path) where T : Node {
        _ = node.GetNodeOrNull<T>(path) ?? throw new InvalidOperationException($"Track scene format error: Couldn't find '{path}'");
    }
}
