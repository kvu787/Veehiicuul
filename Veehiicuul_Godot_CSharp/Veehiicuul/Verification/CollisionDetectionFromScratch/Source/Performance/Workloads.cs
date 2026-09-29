using System;
using System.Collections.Generic;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>Pose sets for timing. Each is deterministic for a given track.</summary>
internal static class Workloads {
    public const int PoseCount = 4096;

    /// <summary>A lap along the centerline with a fixed step between frames.</summary>
    public static RectanglePose[] Lap(TrackCase track, double lateralFraction, double step) {
        return track.Track.LapPoses(PoseCount, step * track.FootprintScale, lateralFraction);
    }

    /// <summary>
    /// A lap with the vehicle's side a small gap away from the outer barrier's nominal
    /// line, reduced to the poses that are clear.
    /// </summary>
    public static RectanglePose[] LapBesideBarrier(TrackCase track, RectangleLocalBounds footprint, double gap, double step) {
        double halfVehicleWidth = Math.Max(Math.Abs((double)footprint.MinX), Math.Abs((double)footprint.MaxX));
        double lateral = (track.Track.HalfWidth - halfVehicleWidth - gap * track.FootprintScale) / track.Track.HalfWidth;
        RectanglePose[] poses = track.Track.LapPoses(PoseCount * 2, step * track.FootprintScale, lateral);
        return Filter(track.Detector, footprint, poses, wantContact: false, PoseCount);
    }

    public static RectanglePose[] Uniform(TrackCase track, int seed) {
        return PoseSampler.Uniform(track.Track.Oracle, PoseCount, seed, 0.0);
    }

    public static RectanglePose[] UniformClear(TrackCase track, RectangleLocalBounds footprint, int seed) {
        RectanglePose[] poses = PoseSampler.Uniform(track.Track.Oracle, PoseCount * 16, seed, 0.0);
        return Filter(track.Detector, footprint, poses, wantContact: false, PoseCount);
    }

    public static RectanglePose[] Contacts(TrackCase track, RectangleLocalBounds footprint, int seed) {
        RectanglePose[] poses = PoseSampler.NearBarrier(
            track.Track.Oracle, footprint, PoseCount * 8, seed, 1e-3 * track.FootprintScale, 1.0 * track.FootprintScale);
        return Filter(track.Detector, footprint, poses, wantContact: true, PoseCount);
    }

    public static RectanglePose[] NearMisses(TrackCase track, RectangleLocalBounds footprint, int seed) {
        RectanglePose[] poses = PoseSampler.NearBarrier(
            track.Track.Oracle, footprint, PoseCount * 16, seed, 1e-3 * track.FootprintScale, 0.5 * track.FootprintScale);
        return Filter(track.Detector, footprint, poses, wantContact: false, PoseCount);
    }

    public static RectanglePose[] Outside(TrackCase track) {
        OracleTrack oracle = track.Track.Oracle;
        double span = Math.Max(
            (double)oracle.BoundsMaxX - oracle.BoundsMinX, (double)oracle.BoundsMaxY - oracle.BoundsMinY);
        Random random = new(8600);
        RectanglePose[] poses = new RectanglePose[PoseCount];
        for (int index = 0; index < poses.Length; ++index) {
            double angle = random.NextDouble() * Math.Tau;
            double distance = span * (1.5 + random.NextDouble());
            poses[index] = new RectanglePose(
                (float)(((double)oracle.BoundsMinX + oracle.BoundsMaxX) * 0.5 + distance * Math.Cos(angle)),
                (float)(((double)oracle.BoundsMinY + oracle.BoundsMaxY) * 0.5 + distance * Math.Sin(angle)),
                (float)((random.NextDouble() * 2.0 - 1.0) * Math.PI));
        }

        return poses;
    }

    /// <summary>Poses inside the grid whose cell lists no edge at all.</summary>
    public static RectanglePose[] EmptyCells(TrackCase track, int seed) {
        ExpandedGridView grid = track.Grid ?? throw new InvalidOperationException("No expanded grid.");
        RectanglePose[] candidates = PoseSampler.Uniform(track.Track.Oracle, PoseCount * 16, seed, 0.0);
        List<RectanglePose> selected = [];
        foreach (RectanglePose pose in candidates) {
            if (grid.GetRange(pose.PositionX, pose.PositionY).Count == 0) {
                selected.Add(pose);
                if (selected.Count == PoseCount) {
                    break;
                }
            }
        }

        return [.. selected];
    }

    /// <summary>The clear pose whose cell lists the most edges: the most expensive clear query.</summary>
    public static RectanglePose[] BusiestClearCell(TrackCase track, RectangleLocalBounds footprint, int seed, out int candidates) {
        ExpandedGridView grid = track.Grid ?? throw new InvalidOperationException("No expanded grid.");
        RectanglePose[] poses = PoseSampler.NearBarrier(
            track.Track.Oracle, footprint, PoseCount * 16, seed, 0.05 * track.FootprintScale, 3.0 * track.FootprintScale);
        RectanglePose best = default;
        candidates = -1;
        foreach (RectanglePose pose in poses) {
            int count = grid.GetRange(pose.PositionX, pose.PositionY).Count;
            if (count > candidates && !track.Detector.IsColliding(footprint, pose)) {
                candidates = count;
                best = pose;
            }
        }

        RectanglePose[] repeated = new RectanglePose[64];
        Array.Fill(repeated, best);
        return repeated;
    }

    public static RectanglePose[] Filter(
        TrackCollisionDetector detector, RectangleLocalBounds footprint, RectanglePose[] poses,
        bool wantContact, int limit) {
        List<RectanglePose> selected = new(limit);
        foreach (RectanglePose pose in poses) {
            if (detector.IsColliding(footprint, pose) == wantContact) {
                selected.Add(pose);
                if (selected.Count == limit) {
                    break;
                }
            }
        }

        return [.. selected];
    }
}
