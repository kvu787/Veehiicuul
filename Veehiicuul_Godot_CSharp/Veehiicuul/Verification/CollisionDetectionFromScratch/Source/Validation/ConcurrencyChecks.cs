using System;
using System.Collections.Generic;
using System.Threading;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>
/// Queries one detector from many threads at once and compares every answer
/// with the answer obtained on a single thread. The detector documents no
/// thread-safety promise; this measures what the implementation does.
/// </summary>
internal static class ConcurrencyChecks {
    public static void Run(ValidationContext context, IReadOnlyList<TrackCase> tracks) {
        context.Run("Concurrency: simultaneous queries on one detector", result => {
            foreach (TrackCase track in tracks) {
                if (track.Name is TrackCatalog.Circuit or TrackCatalog.CircuitWide or TrackCatalog.Crowded
                    or TrackCatalog.ScatteredRemote) {
                    Simultaneous(result, context, track);
                }
            }
        });
        context.Run("Concurrency: simultaneous construction from one collider", result => {
            TrackCase track = TrackCatalog.Build(TrackCatalog.Circuit);
            RectangleLocalBounds footprint = Footprints.Reference;
            RectanglePose[] poses = PoseSampler.NearBarrier(track.Track.Oracle, footprint, 4000, 8502, 1e-6, 1.5);
            bool[] expected = new bool[poses.Length];
            for (int index = 0; index < poses.Length; ++index) {
                expected[index] = track.Detector.IsCollidingLinear(footprint, poses[index]);
            }

            Thread[] threads = new Thread[Math.Min(16, context.Threads)];
            for (int thread = 0; thread < threads.Length; ++thread) {
                threads[thread] = new Thread(() => {
                    TrackCollisionDetector detector = new(track.Track.Collider, footprint);
                    for (int index = 0; index < poses.Length; ++index) {
                        result.Check(
                            detector.IsColliding(footprint, poses[index]) == expected[index],
                            $"A detector built concurrently disagrees; {Exact.Text(poses[index])}.");
                    }
                });
                threads[thread].Start();
            }

            foreach (Thread thread in threads) {
                thread.Join();
            }
        });
    }

    private static void Simultaneous(SuiteResult result, ValidationContext context, TrackCase track) {
        RectangleLocalBounds footprint = Footprints.Reference;
        List<RectanglePose> poseList = [];
        poseList.AddRange(PoseSampler.Uniform(track.Track.Oracle, context.Scaled(20_000), 8500, 8.0));
        poseList.AddRange(PoseSampler.NearBarrier(track.Track.Oracle, footprint, context.Scaled(20_000), 8501, 1e-6, 1.5));
        RectanglePose[] poses = [.. poseList];
        bool[] expected = new bool[poses.Length];
        for (int index = 0; index < poses.Length; ++index) {
            expected[index] = track.Detector.IsColliding(footprint, poses[index]);
        }

        int threadCount = Math.Max(2, context.Threads);
        using Barrier barrier = new(threadCount);
        Thread[] threads = new Thread[threadCount];
        for (int thread = 0; thread < threadCount; ++thread) {
            int offset = thread * 7919;
            threads[thread] = new Thread(() => {
                barrier.SignalAndWait();
                for (int pass = 0; pass < 4; ++pass) {
                    for (int index = 0; index < poses.Length; ++index) {
                        int selected = (index + offset) % poses.Length;
                        if (track.Detector.IsColliding(footprint, poses[selected]) != expected[selected]) {
                            result.Fail($"{track.Name}: a concurrent query changed its answer; "
                                + $"{Exact.Text(poses[selected])}.");
                        }
                    }
                }
            });
            threads[thread].Start();
        }

        foreach (Thread thread in threads) {
            thread.Join();
        }

        result.AddCases((long)threadCount * 4 * poses.Length);
        result.Fact(track.Name, $"{threadCount} threads, {poses.Length} poses, four passes each");
    }
}
