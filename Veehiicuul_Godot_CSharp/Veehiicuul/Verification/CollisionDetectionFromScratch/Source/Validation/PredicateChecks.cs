using System;

namespace CollisionDetectionFromScratch;

/// <summary>
/// Checks the detector's private orientation and segment predicates against
/// exact arithmetic. The inputs are chosen so that each of the three
/// arithmetic routes is decisive: the binary64 filter, the 128-bit integers,
/// and the arbitrary-precision integers.
/// </summary>
internal static class PredicateChecks {
    public static void Run(ValidationContext context) {
        context.Run("Predicate: orientation of arbitrary finite values", result => Arbitrary(result, context));
        context.Run("Predicate: orientation near collinearity", result => NearCollinear(result, context));
        context.Run("Predicate: orientation across wide exponent spans", result => WideSpans(result, context));
        context.Run("Predicate: segment intersection", result => Segments(result, context));
    }

    private static void CheckOrientation(
        SuiteResult result, float ax, float ay, float bx, float by, float cx, float cy, ref long zeros) {
        int expected = ExactOracle.OrientationSign(ax, ay, bx, by, cx, cy);
        if (expected == 0) {
            ++zeros;
        }

        int filtered = DetectorInternals.OrientationSign(ax, ay, bx, by, cx, cy);
        result.Check(
            filtered == expected,
            $"Orientation is {filtered}, expected {expected}: a=({Exact.Text(ax)}, {Exact.Text(ay)}) "
            + $"b=({Exact.Text(bx)}, {Exact.Text(by)}) c=({Exact.Text(cx)}, {Exact.Text(cy)}).");
        int exact = DetectorInternals.ExactOrientationSign(ax, ay, bx, by, cx, cy);
        result.Check(
            exact == expected,
            $"Exact orientation is {exact}, expected {expected}: a=({Exact.Text(ax)}, {Exact.Text(ay)}) "
            + $"b=({Exact.Text(bx)}, {Exact.Text(by)}) c=({Exact.Text(cx)}, {Exact.Text(cy)}).");
    }

    private static void Arbitrary(SuiteResult result, ValidationContext context) {
        Random random = new(8101);
        long zeros = 0;
        int count = context.Scaled(150_000);
        for (int iteration = 0; iteration < count; ++iteration) {
            float ax = OracleSelfChecks.RandomFinite(random), ay = OracleSelfChecks.RandomFinite(random);
            float bx = OracleSelfChecks.RandomFinite(random), by = OracleSelfChecks.RandomFinite(random);
            float cx = OracleSelfChecks.RandomFinite(random), cy = OracleSelfChecks.RandomFinite(random);
            CheckOrientation(result, ax, ay, bx, by, cx, cy, ref zeros);
            if ((iteration & 7) == 0) {
                // Repeated points and shared coordinates.
                CheckOrientation(result, ax, ay, bx, by, ax, ay, ref zeros);
                CheckOrientation(result, ax, ay, bx, by, bx, by, ref zeros);
                CheckOrientation(result, ax, ay, ax, by, ax, cy, ref zeros);
                CheckOrientation(result, ax, ay, bx, ay, cx, ay, ref zeros);
                CheckOrientation(result, 0f, -0f, bx, by, cx, cy, ref zeros);
            }
        }

        result.Fact("Exactly collinear triples", zeros);
    }

    /// <summary>
    /// The third point is the binary32 rounding of a point on the line through the first
    /// two, then moved by a few representable steps. Coordinates span several binades, as
    /// track coordinates do, so binary64 products are inexact.
    /// </summary>
    private static void NearCollinear(SuiteResult result, ValidationContext context) {
        Random random = new(8102);
        long zeros = 0;
        int count = context.Scaled(200_000);
        for (int iteration = 0; iteration < count; ++iteration) {
            double magnitude = Math.Pow(2.0, random.Next(-20, 21));
            float ax = (float)((random.NextDouble() * 2.0 - 1.0) * 300.0 * magnitude * Math.Pow(2.0, -random.Next(0, 24)));
            float ay = (float)((random.NextDouble() * 2.0 - 1.0) * 300.0 * magnitude * Math.Pow(2.0, -random.Next(0, 24)));
            float bx = (float)((random.NextDouble() * 2.0 - 1.0) * 300.0 * magnitude);
            float by = (float)((random.NextDouble() * 2.0 - 1.0) * 300.0 * magnitude);
            double along = random.NextDouble() * 2.0 - 0.5;
            float cx = (float)(ax + ((double)bx - ax) * along);
            float cy = (float)(ay + ((double)by - ay) * along);
            if (!float.IsFinite(cx) || !float.IsFinite(cy)) {
                continue;
            }

            CheckOrientation(result, ax, ay, bx, by, cx, cy, ref zeros);
            CheckOrientation(
                result, ax, ay, bx, by,
                ExactNumber.Step(cx, random.Next(-3, 4)), ExactNumber.Step(cy, random.Next(-3, 4)), ref zeros);
            // Lattice coordinates make exact collinearity common.
            float lx = random.Next(-400, 401) * 0.25f, ly = random.Next(-400, 401) * 0.25f;
            float dx = random.Next(-40, 41) * 0.25f, dy = random.Next(-40, 41) * 0.25f;
            int first = random.Next(-8, 9), second = random.Next(-8, 9);
            CheckOrientation(
                result, lx, ly, lx + dx * first, ly + dy * first, lx + dx * second, ly + dy * second, ref zeros);
        }

        result.Fact("Exactly collinear triples", zeros);
    }

    /// <summary>
    /// Three points on one ray through the origin, with exponents that differ by a chosen
    /// span, then one coordinate of the smallest point moved by a few steps. The true
    /// determinant is far below the rounding error of the binary64 evaluation, so only
    /// exact arithmetic can determine its sign. Spans above 37 exceed the 128-bit route.
    /// </summary>
    private static void WideSpans(SuiteResult result, ValidationContext context) {
        Random random = new(8103);
        long zeros = 0, cases = 0;
        int repetitions = context.Scaled(40);
        for (int span = 24; span <= 80; ++span) {
            for (int repetition = 0; repetition < repetitions; ++repetition) {
                int baseExponent = random.Next(-110, 100 - span);
                int u = RandomSignificand(random), v = RandomSignificand(random);
                float smallX = MathF.ScaleB(u, baseExponent), smallY = MathF.ScaleB(v, baseExponent);
                float largeX = MathF.ScaleB(u, baseExponent + span), largeY = MathF.ScaleB(v, baseExponent + span);
                float middleX = MathF.ScaleB(u, baseExponent + span - 1);
                float middleY = MathF.ScaleB(v, baseExponent + span - 1);
                if (!float.IsFinite(largeX) || !float.IsFinite(largeY)) {
                    continue;
                }

                for (int step = -2; step <= 2; ++step) {
                    float movedY = ExactNumber.Step(smallY, step);
                    float movedX = ExactNumber.Step(smallX, step);
                    // Every ordering of the three points reaches different operand pairs.
                    CheckOrientation(result, smallX, movedY, largeX, largeY, middleX, middleY, ref zeros);
                    CheckOrientation(result, largeX, largeY, smallX, movedY, middleX, middleY, ref zeros);
                    CheckOrientation(result, middleX, middleY, largeX, largeY, smallX, movedY, ref zeros);
                    CheckOrientation(result, movedX, smallY, largeX, largeY, middleX, middleY, ref zeros);
                    CheckOrientation(result, largeX, largeY, middleX, middleY, movedX, smallY, ref zeros);
                    cases += 5;
                }
            }
        }

        result.Fact("Triples", cases);
        result.Fact("Exactly collinear triples", zeros);
    }

    private static void Segments(SuiteResult result, ValidationContext context) {
        Random random = new(8104);
        int count = context.Scaled(300_000);
        long intersections = 0;
        Span<float> values = stackalloc float[8];
        for (int iteration = 0; iteration < count; ++iteration) {
            switch (iteration % 5) {
            case 0:
                for (int index = 0; index < 8; ++index) {
                    values[index] = random.Next(-6, 7);
                }

                break;
            case 1:
                for (int index = 0; index < 8; ++index) {
                    values[index] = random.Next(-200, 201) * 0.125f + 64f;
                }

                break;
            case 2:
                for (int index = 0; index < 8; ++index) {
                    values[index] = (float)((random.NextDouble() * 2.0 - 1.0) * 300.0);
                }

                break;
            case 3:
                // The second segment starts on the first one after rounding, within a few steps.
                for (int index = 0; index < 4; ++index) {
                    values[index] = (float)((random.NextDouble() * 2.0 - 1.0) * 300.0);
                }

                double along = random.NextDouble();
                values[4] = ExactNumber.Step((float)(values[0] + ((double)values[2] - values[0]) * along), random.Next(-2, 3));
                values[5] = ExactNumber.Step((float)(values[1] + ((double)values[3] - values[1]) * along), random.Next(-2, 3));
                values[6] = (float)(values[4] + (random.NextDouble() * 2.0 - 1.0) * 6.0);
                values[7] = (float)(values[5] + (random.NextDouble() * 2.0 - 1.0) * 6.0);
                break;
            default:
                for (int index = 0; index < 8; ++index) {
                    values[index] = OracleSelfChecks.RandomFinite(random);
                }

                break;
            }

            if (values[0] == values[2] && values[1] == values[3]) {
                // The detector never stores a zero-length barrier edge.
                continue;
            }

            if (iteration % 11 == 0) {
                // A rectangle side can collapse to a point at very large coordinates.
                values[6] = values[4];
                values[7] = values[5];
            }

            bool expected = ExactOracle.SegmentsIntersectWithBigInteger(
                values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
            bool actual = DetectorInternals.SegmentsIntersect(
                values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
            if (expected) {
                ++intersections;
            }

            result.Check(
                actual == expected,
                $"Segments: detector={actual}, exact={expected}: "
                + $"({Exact.Text(values[0])}, {Exact.Text(values[1])})-({Exact.Text(values[2])}, {Exact.Text(values[3])}) "
                + $"({Exact.Text(values[4])}, {Exact.Text(values[5])})-({Exact.Text(values[6])}, {Exact.Text(values[7])}).");
        }

        result.Fact("Intersecting pairs", intersections);
    }

    private static int RandomSignificand(Random random) {
        int magnitude = random.Next(1 << 22, 1 << 24) | 1;
        return random.Next(2) == 0 ? magnitude : -magnitude;
    }
}
