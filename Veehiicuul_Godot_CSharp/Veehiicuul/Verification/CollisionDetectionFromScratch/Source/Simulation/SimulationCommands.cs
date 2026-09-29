using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

internal sealed class ApproachResult {
    public required string Barrier { get; init; }
    public required double StepPerFrame { get; init; }
    public required double StepInVehicleLengths { get; init; }
    public required double IncidenceDegrees { get; init; }
    public required int Trials { get; init; }
    public required int Missed { get; init; }
    public required double PredictedMissFraction { get; init; }
    public required double MeanOverlapAtDetection { get; init; }
    public required double LargestOverlapAtDetection { get; init; }
    public required long FramesAfterMiss { get; init; }
    public required long ClearFramesAfterMiss { get; init; }
    public double MissFraction => (double)this.Missed / this.Trials;
}

/// <summary>
/// The detector answers for one pose. A moving vehicle is tested once per
/// frame, so what happens between two frames is never examined. These
/// experiments drive a rectangle along straight lines at a fixed distance per
/// frame and record when the detector first reports contact, if ever.
///
/// The motion model is deliberately minimal and belongs to this harness: a
/// constant velocity, a yaw aligned with the velocity, and a uniformly random
/// position within the first frame.
/// </summary>
internal static class SimulationCommands {
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static int Simulate(Options options) {
        int trials = (int)options.Number("trials", 20000);
        RectangleLocalBounds footprint = Footprints.Reference;
        double length = (double)footprint.MaxY - footprint.MinY;
        double width = (double)footprint.MaxX - footprint.MinX;
        List<ApproachResult> results = [];

        // A straight wall at X = 110: the near face of one large rectangular barrier, with
        // nothing else within reach of the approaching vehicle.
        TrackCase circuit = new(
            SyntheticTrack.FromOutlines(
                "Wall", [(WallX, -WallHalfHeight), (WallFarX, -WallHalfHeight), (WallFarX, WallHalfHeight), (WallX, WallHalfHeight)]),
            footprint, 1.0, "one straight wall");
        if (!circuit.Detector.UsesExpandedGrid) {
            throw new InvalidOperationException("The wall fixture should use the expanded grid.");
        }

        foreach (double incidence in new[] { 0.0, 30.0, 60.0, 80.0 }) {
            foreach (double lengths in new[] { 0.05, 0.25, 0.5, 0.9, 1.0, 1.1, 1.25, 1.5, 2.0, 3.0, 5.0 }) {
                results.Add(Wall(circuit, footprint, lengths * length, incidence, trials, 9000 + results.Count));
            }
        }

        // Isolated obstacles narrower than the vehicle, met head on.
        foreach (double radius in new[] { 0.25, 0.5, 1.0 }) {
            foreach (double step in new[] { 0.1, 0.25, 0.5, 1.0, 1.5, 2.0, 3.0, 6.0 }) {
                results.Add(Obstacle(footprint, radius, step, trials, 9500 + results.Count));
            }
        }

        foreach (ApproachResult result in results) {
            Console.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{result.Barrier}: step={result.StepPerFrame:F3} ({result.StepInVehicleLengths:F2} lengths); "
                + $"incidence={result.IncidenceDegrees:F0}; missed={result.MissFraction:P2}; "
                + $"predicted={result.PredictedMissFraction:P2}; meanOverlap={result.MeanOverlapAtDetection:F4}; "
                + $"largestOverlap={result.LargestOverlapAtDetection:F4}; "
                + $"framesInsideBarrier={result.FramesAfterMiss}; clearFramesInsideBarrier={result.ClearFramesAfterMiss}"));
        }

        string output = options.Text("output", string.Empty);
        if (output.Length > 0) {
            File.WriteAllText(output, JsonSerializer.Serialize(new {
                VehicleLength = length,
                VehicleWidth = width,
                Trials = trials,
                Results = results,
            }, JsonOptions));
        }

        return 0;
    }

    private const float WallX = 110f;
    private const float WallFarX = 250f;
    private const float WallHalfHeight = 250f;

    private static ApproachResult Wall(
        TrackCase track, RectangleLocalBounds footprint, double step, double incidenceDegrees, int trials, int seed) {
        const double wall = WallX;
        double length = (double)footprint.MaxY - footprint.MinY;
        double width = (double)footprint.MaxX - footprint.MinX;
        double incidence = incidenceDegrees * Math.PI / 180.0;
        double directionX = Math.Cos(incidence), directionY = Math.Sin(incidence);
        float yaw = (float)SyntheticTrack.YawForDirection(directionX, directionY);
        // Extent of the rectangle along the wall normal, and the distance from the pose
        // position to its leading and trailing points along that normal.
        Span<double> corners = stackalloc double[8];
        ToleranceOracle.BuildCorners(
            footprint.MinX, footprint.MinY, footprint.MaxX, footprint.MaxY, 0.0, 0.0, yaw, corners);
        double leading = double.NegativeInfinity, trailing = double.PositiveInfinity;
        for (int corner = 0; corner < 4; ++corner) {
            leading = Math.Max(leading, corners[2 * corner]);
            trailing = Math.Min(trailing, corners[2 * corner]);
        }

        double advance = step * directionX;
        double predicted = Math.Max(0.0, 1.0 - (leading - trailing) / advance);
        Random random = new(seed);
        int missed = 0, detected = 0;
        double overlapSum = 0.0, overlapLargest = 0.0;
        long clearAfterMiss = 0, framesAfterMiss = 0;
        for (int trial = 0; trial < trials; ++trial) {
            // Start with the leading point between one and two frames before the wall.
            double startX = wall - leading - advance * (1.0 + random.NextDouble());
            double startY = (random.NextDouble() * 2.0 - 1.0) * 20.0 - directionY * 10.0;
            bool found = false;
            for (int frame = 0; frame < 100000; ++frame) {
                double x = startX + frame * step * directionX;
                double y = startY + frame * step * directionY;
                if (x + trailing > wall) {
                    // Entirely beyond the wall, inside the barrier. Keep driving while the
                    // vehicle stays inside: no later pose reports the barrier.
                    for (int later = 0; later < 200; ++later) {
                        double laterX = x + later * step * directionX;
                        double laterY = y + later * step * directionY;
                        if (laterX + leading >= WallFarX - 1.0 || Math.Abs(laterY) >= WallHalfHeight - 8.0) {
                            break;
                        }

                        ++framesAfterMiss;
                        if (!track.Detector.IsColliding(footprint, new RectanglePose((float)laterX, (float)laterY, yaw))) {
                            ++clearAfterMiss;
                        }
                    }

                    break;
                }

                RectanglePose pose = new((float)x, (float)y, yaw);
                if (track.Detector.IsColliding(footprint, pose)) {
                    double overlap = x + leading - wall;
                    overlapSum += overlap;
                    overlapLargest = Math.Max(overlapLargest, overlap);
                    found = true;
                    break;
                }
            }

            if (found) {
                ++detected;
            } else {
                ++missed;
            }
        }

        return new ApproachResult {
            Barrier = "straight wall",
            StepPerFrame = step,
            StepInVehicleLengths = step / length,
            IncidenceDegrees = incidenceDegrees,
            Trials = trials,
            Missed = missed,
            PredictedMissFraction = predicted,
            MeanOverlapAtDetection = detected == 0 ? 0.0 : overlapSum / detected,
            LargestOverlapAtDetection = overlapLargest,
            FramesAfterMiss = framesAfterMiss,
            ClearFramesAfterMiss = clearAfterMiss,
        };
    }

    private static ApproachResult Obstacle(
        RectangleLocalBounds footprint, double radius, double step, int trials, int seed) {
        double length = (double)footprint.MaxY - footprint.MinY;
        float r = (float)radius;
        // A square obstacle with its sides across and along the direction of travel,
        // plus two distant anchors that give the index a realistic extent.
        SyntheticTrack source = SyntheticTrack.FromOutlines(
            "Obstacle",
            [(-r, -r), (r, -r), (r, r), (-r, r)],
            [(-150f, -150f), (-149f, -150f), (-150f, -149f)],
            [(150f, 150f), (149f, 150f), (150f, 149f)]);
        TrackCase track = new(source, footprint, 1.0, "isolated obstacle");
        // Travel along +Y. The front is the minimum local Y side, so yaw is a half turn.
        float yaw = (float)SyntheticTrack.YawForDirection(0.0, 1.0);
        Span<double> corners = stackalloc double[8];
        ToleranceOracle.BuildCorners(
            footprint.MinX, footprint.MinY, footprint.MaxX, footprint.MaxY, 0.0, 0.0, yaw, corners);
        double leading = double.NegativeInfinity, trailing = double.PositiveInfinity, halfSpan = 0.0;
        for (int corner = 0; corner < 4; ++corner) {
            leading = Math.Max(leading, corners[2 * corner + 1]);
            trailing = Math.Min(trailing, corners[2 * corner + 1]);
            halfSpan = Math.Max(halfSpan, Math.Abs(corners[2 * corner]));
        }

        // The front side and the rear side each pass over the obstacle's depth of two radii.
        // Their two windows of detection are one vehicle length apart along the path.
        double depth = 2.0 * radius;
        double predicted = PredictedObstacleMiss(step, depth, leading - trailing);
        Random random = new(seed);
        int missed = 0, detected = 0;
        double overlapSum = 0.0, overlapLargest = 0.0;
        for (int trial = 0; trial < trials; ++trial) {
            double lateral = (random.NextDouble() * 2.0 - 1.0) * Math.Max(0.0, halfSpan - radius - 0.05);
            double startY = -radius - leading - step * (1.0 + random.NextDouble());
            bool found = false;
            for (int frame = 0; frame < 100000; ++frame) {
                double y = startY + frame * step;
                if (y + trailing > radius + 0.5) {
                    break;
                }

                if (track.Detector.IsColliding(footprint, new RectanglePose((float)lateral, (float)y, yaw))) {
                    double overlap = y + leading + radius;
                    overlapSum += overlap;
                    overlapLargest = Math.Max(overlapLargest, overlap);
                    found = true;
                    break;
                }
            }

            if (found) {
                ++detected;
            } else {
                ++missed;
            }
        }

        return new ApproachResult {
            Barrier = string.Create(CultureInfo.InvariantCulture, $"square obstacle of side {depth:F2}"),
            StepPerFrame = step,
            StepInVehicleLengths = step / length,
            IncidenceDegrees = 0.0,
            Trials = trials,
            Missed = missed,
            PredictedMissFraction = predicted,
            MeanOverlapAtDetection = detected == 0 ? 0.0 : overlapSum / detected,
            LargestOverlapAtDetection = overlapLargest,
            FramesAfterMiss = 0,
            ClearFramesAfterMiss = 0,
        };
    }

    /// <summary>
    /// Fraction of starting phases for which no sampled pose has the front side or the
    /// rear side over the obstacle. Phases are positions modulo the step. The front
    /// side detects over a window as long as the obstacle's depth, and the rear side
    /// over an equal window one vehicle length later.
    /// </summary>
    private static double PredictedObstacleMiss(double step, double depth, double vehicleLength) {
        if (depth >= step) {
            return 0.0;
        }

        const int resolution = 200000;
        int covered = 0;
        double rearOffset = vehicleLength % step;
        for (int index = 0; index < resolution; ++index) {
            double phase = (index + 0.5) * step / resolution;
            bool front = phase <= depth;
            double rear = (phase - rearOffset + step) % step;
            if (front || rear <= depth) {
                ++covered;
            }
        }

        return 1.0 - (double)covered / resolution;
    }
}
