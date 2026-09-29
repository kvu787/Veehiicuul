using System;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>Seeded pose generators. Every generator returns finite binary32 poses.</summary>
internal static class PoseSampler {
    /// <summary>Uniform positions inside the track bounds grown by a margin; uniform yaw in one full turn.</summary>
    public static RectanglePose[] Uniform(OracleTrack track, int count, int seed, double margin) {
        Random random = new(seed);
        RectanglePose[] poses = new RectanglePose[count];
        double minX = track.BoundsMinX - margin, maxX = track.BoundsMaxX + margin;
        double minY = track.BoundsMinY - margin, maxY = track.BoundsMaxY + margin;
        for (int index = 0; index < count; ++index) {
            poses[index] = new RectanglePose(
                (float)(minX + random.NextDouble() * (maxX - minX)),
                (float)(minY + random.NextDouble() * (maxY - minY)),
                (float)((random.NextDouble() * 2.0 - 1.0) * Math.PI));
        }

        return poses;
    }

    /// <summary>
    /// Poses that bring one rectangle corner or one side midpoint close to a random point
    /// on a random edge. The gap is log-uniform between the two limits and may be on
    /// either side of the edge.
    /// </summary>
    public static RectanglePose[] NearBarrier(
        OracleTrack track, RectangleLocalBounds footprint, int count, int seed,
        double minimumGap, double maximumGap) {
        Random random = new(seed);
        RectanglePose[] poses = new RectanglePose[count];
        double centerX = ((double)footprint.MinX + footprint.MaxX) * 0.5;
        double centerY = ((double)footprint.MinY + footprint.MaxY) * 0.5;
        for (int index = 0; index < count; ++index) {
            int edge = random.Next(track.EdgeCount);
            double along = random.NextDouble();
            double targetX = track.Ax[edge] + ((double)track.Bx[edge] - track.Ax[edge]) * along;
            double targetY = track.Ay[edge] + ((double)track.By[edge] - track.Ay[edge]) * along;
            double gap = minimumGap * Math.Pow(maximumGap / minimumGap, random.NextDouble());
            double direction = random.NextDouble() * Math.Tau;
            targetX += gap * Math.Cos(direction);
            targetY += gap * Math.Sin(direction);

            double localX, localY;
            int feature = random.Next(8);
            if (feature < 4) {
                localX = (feature & 1) == 0 ? footprint.MinX : footprint.MaxX;
                localY = (feature & 2) == 0 ? footprint.MinY : footprint.MaxY;
            } else if (feature < 6) {
                localX = centerX;
                localY = feature == 4 ? footprint.MinY : footprint.MaxY;
            } else {
                localX = feature == 6 ? footprint.MinX : footprint.MaxX;
                localY = centerY;
            }

            double yaw = (random.NextDouble() * 2.0 - 1.0) * Math.PI;
            poses[index] = PlaceLocalPoint(localX, localY, targetX, targetY, yaw);
        }

        return poses;
    }

    /// <summary>Pose whose local point lands on a world point, before any rounding.</summary>
    public static RectanglePose PlaceLocalPoint(
        double localX, double localY, double worldX, double worldY, double yaw) {
        float roundedYaw = (float)yaw;
        double cosine = Math.Cos(roundedYaw), sine = Math.Sin(roundedYaw);
        double offsetX = localX * cosine + localY * sine;
        double offsetY = localY * cosine - localX * sine;
        return new RectanglePose((float)(worldX - offsetX), (float)(worldY - offsetY), roundedYaw);
    }

    /// <summary>
    /// Searches the binary32 neighborhood of a pose for one whose rounded corner equals a
    /// target point exactly. Returns false when no neighbor within the radius does.
    /// </summary>
    public static bool TryLandCornerExactly(
        RectangleLocalBounds footprint, int corner, float targetX, float targetY, float yaw,
        int searchRadius, float[] scratch, out RectanglePose pose) {
        double localX = corner is 0 or 3 ? footprint.MinX : footprint.MaxX;
        double localY = corner is 0 or 1 ? footprint.MinY : footprint.MaxY;
        RectanglePose start = PlaceLocalPoint(localX, localY, targetX, targetY, yaw);
        for (int radius = 0; radius <= searchRadius; ++radius) {
            for (int stepX = -radius; stepX <= radius; ++stepX) {
                for (int stepY = -radius; stepY <= radius; ++stepY) {
                    if (Math.Max(Math.Abs(stepX), Math.Abs(stepY)) != radius) {
                        continue;
                    }

                    float x = ExactNumber.Step(start.PositionX, stepX);
                    float y = ExactNumber.Step(start.PositionY, stepY);
                    if (!float.IsFinite(x) || !float.IsFinite(y)) {
                        continue;
                    }

                    RectanglePose candidate = new(x, y, yaw);
                    DetectorInternals.Transform(footprint, candidate, scratch);
                    if (scratch[2 * corner] == targetX && scratch[2 * corner + 1] == targetY) {
                        pose = candidate;
                        return true;
                    }
                }
            }
        }

        pose = start;
        return false;
    }
}
