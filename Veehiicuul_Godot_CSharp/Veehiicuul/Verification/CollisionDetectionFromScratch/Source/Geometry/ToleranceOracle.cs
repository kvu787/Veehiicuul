using System;

namespace CollisionDetectionFromScratch;

internal enum ToleranceVerdict {
    /// <summary>Every barrier edge is farther than the tolerance from the rectangle perimeter.</summary>
    Clear,

    /// <summary>A barrier edge crosses a rectangle side with more than the tolerance to spare.</summary>
    Colliding,

    /// <summary>The geometry is within the tolerance of changing its answer.</summary>
    Undecided,
}

/// <summary>
/// Classifies a pose from distances in binary64 arithmetic. The rectangle is
/// built from the documented pose convention, independent of the detector's
/// rounding. Poses that are nearly touching are left undecided.
/// </summary>
internal static class ToleranceOracle {
    public static ToleranceVerdict Classify(
        OracleTrack track,
        double minX, double minY, double maxX, double maxY,
        double positionX, double positionY, double clockwiseYaw,
        double tolerance) {
        Span<double> corners = stackalloc double[8];
        BuildCorners(minX, minY, maxX, maxY, positionX, positionY, clockwiseYaw, corners);
        double boundsMinX = Math.Min(Math.Min(corners[0], corners[2]), Math.Min(corners[4], corners[6])) - tolerance;
        double boundsMaxX = Math.Max(Math.Max(corners[0], corners[2]), Math.Max(corners[4], corners[6])) + tolerance;
        double boundsMinY = Math.Min(Math.Min(corners[1], corners[3]), Math.Min(corners[5], corners[7])) - tolerance;
        double boundsMaxY = Math.Max(Math.Max(corners[1], corners[3]), Math.Max(corners[5], corners[7])) + tolerance;

        bool undecided = false;
        for (int edge = 0; edge < track.EdgeCount; ++edge) {
            if (track.MaxX[edge] < boundsMinX || track.MinX[edge] > boundsMaxX
                || track.MaxY[edge] < boundsMinY || track.MinY[edge] > boundsMaxY) {
                continue;
            }

            double ax = track.Ax[edge], ay = track.Ay[edge], bx = track.Bx[edge], by = track.By[edge];
            for (int side = 0; side < 4; ++side) {
                int next = (side + 1) & 3;
                double cx = corners[2 * side], cy = corners[2 * side + 1];
                double dx = corners[2 * next], dy = corners[2 * next + 1];
                if (CrossesWithMargin(ax, ay, bx, by, cx, cy, dx, dy, tolerance)) {
                    return ToleranceVerdict.Colliding;
                }

                if (SegmentDistance(ax, ay, bx, by, cx, cy, dx, dy) <= tolerance) {
                    undecided = true;
                }
            }
        }

        return undecided ? ToleranceVerdict.Undecided : ToleranceVerdict.Clear;
    }

    public static void BuildCorners(
        double minX, double minY, double maxX, double maxY,
        double positionX, double positionY, double clockwiseYaw,
        Span<double> corners) {
        // A clockwise turn by yaw is a counterclockwise turn by its negative.
        double angle = -clockwiseYaw;
        double cosine = Math.Cos(angle);
        double sine = Math.Sin(angle);
        Span<double> localX = [minX, maxX, maxX, minX];
        Span<double> localY = [minY, minY, maxY, maxY];
        for (int index = 0; index < 4; ++index) {
            corners[2 * index] = positionX + localX[index] * cosine - localY[index] * sine;
            corners[2 * index + 1] = positionY + localX[index] * sine + localY[index] * cosine;
        }
    }

    public static double SegmentDistance(
        double ax, double ay, double bx, double by,
        double cx, double cy, double dx, double dy) {
        if (ProperlyCross(ax, ay, bx, by, cx, cy, dx, dy)) {
            return 0.0;
        }

        double distance = PointSegmentDistance(ax, ay, cx, cy, dx, dy);
        distance = Math.Min(distance, PointSegmentDistance(bx, by, cx, cy, dx, dy));
        distance = Math.Min(distance, PointSegmentDistance(cx, cy, ax, ay, bx, by));
        return Math.Min(distance, PointSegmentDistance(dx, dy, ax, ay, bx, by));
    }

    public static double PointSegmentDistance(
        double px, double py, double ax, double ay, double bx, double by) {
        double abx = bx - ax, aby = by - ay;
        double lengthSquared = abx * abx + aby * aby;
        double t = lengthSquared == 0.0 ? 0.0 : ((px - ax) * abx + (py - ay) * aby) / lengthSquared;
        t = Math.Clamp(t, 0.0, 1.0);
        double nearestX = ax + t * abx, nearestY = ay + t * aby;
        double offsetX = px - nearestX, offsetY = py - nearestY;
        return Math.Sqrt(offsetX * offsetX + offsetY * offsetY);
    }

    private static double SignedLineDistance(
        double px, double py, double ax, double ay, double bx, double by) {
        double abx = bx - ax, aby = by - ay;
        double length = Math.Sqrt(abx * abx + aby * aby);
        return length == 0.0 ? 0.0 : (abx * (py - ay) - aby * (px - ax)) / length;
    }

    private static bool ProperlyCross(
        double ax, double ay, double bx, double by,
        double cx, double cy, double dx, double dy) {
        double first = SignedLineDistance(cx, cy, ax, ay, bx, by);
        double second = SignedLineDistance(dx, dy, ax, ay, bx, by);
        double third = SignedLineDistance(ax, ay, cx, cy, dx, dy);
        double fourth = SignedLineDistance(bx, by, cx, cy, dx, dy);
        return first * second < 0.0 && third * fourth < 0.0;
    }

    private static bool CrossesWithMargin(
        double ax, double ay, double bx, double by,
        double cx, double cy, double dx, double dy,
        double tolerance) {
        double first = SignedLineDistance(cx, cy, ax, ay, bx, by);
        double second = SignedLineDistance(dx, dy, ax, ay, bx, by);
        double third = SignedLineDistance(ax, ay, cx, cy, dx, dy);
        double fourth = SignedLineDistance(bx, by, cx, cy, dx, dy);
        return Math.Abs(first) > tolerance && Math.Abs(second) > tolerance
            && Math.Abs(third) > tolerance && Math.Abs(fourth) > tolerance
            && first * second < 0.0 && third * fourth < 0.0;
    }
}
