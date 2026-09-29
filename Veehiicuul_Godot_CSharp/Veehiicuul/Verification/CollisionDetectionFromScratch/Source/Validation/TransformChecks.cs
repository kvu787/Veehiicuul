using System;
using System.Numerics;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>
/// Compares the detector's rounded rectangle corners with the unrounded
/// corners that the pose convention defines. This is the only check of the
/// rotation direction, the corner order, and the accuracy of the rounding
/// that does not reuse the detector's own arithmetic.
/// </summary>
internal static class TransformChecks {
    public static void Run(ValidationContext context) {
        context.Run("Transform: corners follow the pose convention", Convention);
        context.Run("Transform: rounding error within one turn of yaw", result =>
            Accuracy(result, context.Scaled(60_000), Math.PI, 9201, strict: true));
        context.Run("Transform: rounding error for yaw up to one thousand radians", result =>
            Accuracy(result, context.Scaled(20_000), 1000.0, 9202, strict: true));
        context.Run("Transform: rounding error for arbitrary finite yaw", result =>
            Accuracy(result, context.Scaled(20_000), double.PositiveInfinity, 9203, strict: false));
    }

    /// <summary>Hand-derived corners for quarter turns of an asymmetric footprint.</summary>
    private static void Convention(SuiteResult result) {
        RectangleLocalBounds footprint = new(-1f, -2f, 3f, 5f);
        float[] corners = new float[8];

        // No rotation: corners in the order (MinX, MinY), (MaxX, MinY), (MaxX, MaxY), (MinX, MaxY).
        DetectorInternals.Transform(footprint, new RectanglePose(10f, 20f, 0f), corners);
        ExpectCorners(result, corners, [9f, 18f, 13f, 18f, 13f, 25f, 9f, 25f], 0.0, "zero yaw");

        // A clockwise quarter turn maps local +Y onto world +X and local +X onto world -Y.
        DetectorInternals.Transform(footprint, new RectanglePose(10f, 20f, MathF.PI / 2f), corners);
        ExpectCorners(result, corners, [8f, 21f, 8f, 17f, 15f, 17f, 15f, 21f], 1e-5, "clockwise quarter turn");

        // A counterclockwise quarter turn maps local +Y onto world -X and local +X onto world +Y.
        DetectorInternals.Transform(footprint, new RectanglePose(10f, 20f, -MathF.PI / 2f), corners);
        ExpectCorners(result, corners, [12f, 19f, 12f, 23f, 5f, 23f, 5f, 19f], 1e-5, "counterclockwise quarter turn");

        DetectorInternals.Transform(footprint, new RectanglePose(10f, 20f, MathF.PI), corners);
        ExpectCorners(result, corners, [11f, 22f, 7f, 22f, 7f, 15f, 11f, 15f], 1e-5, "half turn");
    }

    private static void ExpectCorners(
        SuiteResult result, float[] actual, float[] expected, double tolerance, string name) {
        for (int index = 0; index < 8; ++index) {
            result.Check(
                Math.Abs((double)actual[index] - expected[index]) <= tolerance,
                $"{name}: coordinate {index} is {Exact.Text(actual[index])}, expected {expected[index]:R}.");
        }
    }

    private static void Accuracy(SuiteResult result, int count, double yawLimit, int seed, bool strict) {
        Random random = new(seed);
        float[] corners = new float[8];
        double worst = 0.0, worstAbsolute = 0.0;
        long coordinates = 0, notNearest = 0, beyondBound = 0;
        for (int iteration = 0; iteration < count; ++iteration) {
            double scale = Math.Pow(2.0, random.Next(-12, 13));
            RectangleLocalBounds footprint = (iteration % 3) switch {
                0 => TrackCatalog.ScaleFootprint(Footprints.Reference, scale),
                1 => TrackCatalog.ScaleFootprint(Footprints.ShiftedOrigin, scale),
                _ => TrackCatalog.ScaleFootprint(Footprints.Oversized, scale),
            };
            double positionScale = scale * Math.Pow(10.0, random.Next(-2, 5));
            float yaw = double.IsPositiveInfinity(yawLimit)
                ? OracleSelfChecks.RandomFinite(random)
                : (float)((random.NextDouble() * 2.0 - 1.0) * yawLimit);
            RectanglePose pose = new(
                (float)((random.NextDouble() * 2.0 - 1.0) * positionScale),
                (float)((random.NextDouble() * 2.0 - 1.0) * positionScale),
                yaw);
            DetectorInternals.Transform(footprint, pose, corners);
            BigInteger[] ideal = HighPrecision.IdealCorners(footprint, pose);
            // Binary64 evaluation error is relative to the operands, not to a result
            // that may be small through cancellation.
            double operands = Math.Abs((double)pose.PositionX) + Math.Abs((double)pose.PositionY)
                + 2.0 * (Math.Abs((double)footprint.MinX) + Math.Abs((double)footprint.MaxX)
                    + Math.Abs((double)footprint.MinY) + Math.Abs((double)footprint.MaxY));
            for (int index = 0; index < 8; ++index) {
                ++coordinates;
                double error = HighPrecision.ErrorInUnitsInLastPlace(ideal[index], corners[index]);
                double absolute = Math.Abs(
                    HighPrecision.ToDouble(HighPrecision.FromSingle(corners[index]) - ideal[index]));
                worst = Math.Max(worst, error);
                worstAbsolute = Math.Max(worstAbsolute, absolute / Math.Max(operands, double.Epsilon));
                if (error > 0.5) {
                    ++notNearest;
                }

                double bound = 0.5 * ExactNumber.UnitInLastPlace(corners[index]) * (1.0 + 1e-9)
                    + 8.0 * 1.1102230246251565e-16 * operands;
                bool within = absolute <= bound;
                if (!within) {
                    ++beyondBound;
                }

                if (strict) {
                    result.Check(
                        within,
                        $"Corner coordinate {index} is off by {error:R} spacings; "
                        + $"{Exact.Text(footprint)} {Exact.Text(pose)}.");
                } else {
                    result.AddCases(1);
                }
            }
        }

        result.Fact("Coordinates compared", coordinates);
        result.Fact("Largest error in spacings of the rounded value", worst);
        result.Fact("Largest error relative to operand magnitude", worstAbsolute);
        result.Fact("Coordinates that are not the nearest binary32 value", notNearest);
        result.Fact("Coordinates beyond the rounding bound", beyondBound);
    }
}
