using System;
using System.Numerics;

namespace CollisionDetectionFromScratch;

/// <summary>
/// Checks the harness's own reference implementations before they judge the
/// detector: hand-derived answers, two exact formulations against each other,
/// and the high-precision trigonometry against known constants.
/// </summary>
internal static class OracleSelfChecks {
    // Segment a-b, segment c-d, and the answer derived by hand on integer coordinates.
    private static readonly (int[] Points, bool Expected, string Name)[] KnownCases = [
        ([0, 0, 4, 4, 0, 4, 4, 0], true, "proper crossing"),
        ([0, 0, 4, 0, 2, 0, 2, 3], true, "endpoint on interior"),
        ([0, 0, 4, 0, 2, -3, 2, 0], true, "endpoint on interior from below"),
        ([0, 0, 4, 0, 4, 0, 6, 2], true, "shared endpoint"),
        ([0, 0, 4, 0, 5, 0, 6, 0], false, "collinear with gap"),
        ([0, 0, 4, 0, 4, 0, 6, 0], true, "collinear touching"),
        ([0, 0, 4, 0, 2, 0, 6, 0], true, "collinear overlap"),
        ([0, 0, 4, 0, 1, 0, 3, 0], true, "collinear containment"),
        ([0, 0, 4, 0, 0, 1, 4, 1], false, "parallel"),
        ([0, 0, 4, 4, 5, 5, 6, 6], false, "diagonal collinear with gap"),
        ([0, 0, 4, 4, 2, 2, 6, 6], true, "diagonal collinear overlap"),
        ([0, 0, 4, 4, 1, 1, 3, 3], true, "diagonal collinear containment"),
        ([0, 0, 4, 4, 4, 4, 7, 5], true, "diagonal shared endpoint"),
        ([0, 0, 4, 4, 2, 2, 2, 2], true, "point on segment"),
        ([0, 0, 4, 4, 2, 3, 2, 3], false, "point off segment"),
        ([0, 0, 4, 4, 5, 5, 5, 5], false, "point on line beyond segment"),
        ([1, 1, 1, 1, 1, 1, 1, 1], true, "equal points"),
        ([1, 1, 1, 1, 2, 2, 2, 2], false, "distinct points"),
        ([0, 0, 4, 0, 2, 1, 2, 3], false, "would cross if extended"),
        ([0, 0, 4, 0, 5, -1, 5, 1], false, "crosses the line beyond the end"),
        ([3, 0, 3, 4, 3, 1, 3, 2], true, "vertical containment"),
        ([3, 0, 3, 4, 3, 5, 3, 6], false, "vertical collinear with gap"),
        ([3, 0, 3, 4, 3, 4, 3, 6], true, "vertical collinear touching"),
        ([3, 0, 3, 4, 3, -2, 3, 9], true, "vertical contained by other"),
        ([0, 0, 3, 6, 1, 2, 5, 0], true, "endpoint on sloped interior"),
        ([0, 0, 3, 6, 1, 3, -3, 6], false, "near sloped interior"),
        ([0, 0, 3, 6, 1, 3, 5, 0], true, "crosses sloped interior"),
        ([0, 0, 6, 3, 2, 0, 2, 1], true, "touches sloped segment from below"),
        ([0, 0, 6, 3, 2, 0, 2, 0], false, "point below sloped segment"),
    ];

    public static void Run(ValidationContext context) {
        context.Run("Oracle: hand-derived segment cases", KnownSegmentCases);
        context.Run("Oracle: two exact formulations agree", result => FormulationsAgree(result, context));
        context.Run("Oracle: high-precision trigonometry", Trigonometry);
    }

    /// <summary>
    /// Applies the symmetries of the problem to every known case. The detector's private
    /// predicate is judged by the same hand-derived answers.
    /// </summary>
    private static void KnownSegmentCases(SuiteResult result) {
        float[] scales = [1f, 0.25f, MathF.ScaleB(1f, -30), MathF.ScaleB(1f, 40), MathF.ScaleB(1f, -140), MathF.ScaleB(1f, 100)];
        int[] shifts = [0, 7, -1000, 65536];
        Span<float> values = stackalloc float[8];
        foreach ((int[] points, bool expected, string name) in KnownCases) {
            foreach (float scale in scales) {
                foreach (int shift in shifts) {
                    // Shifts must stay exact: integer coordinates below 2^24 times a power of two.
                    for (int symmetry = 0; symmetry < 32; ++symmetry) {
                        for (int index = 0; index < 8; ++index) {
                            values[index] = (points[index] + shift) * scale;
                        }

                        ApplySymmetry(values, symmetry);
                        string description = $"{name}, scale {scale:R}, shift {shift}, symmetry {symmetry}";
                        bool parametric = ExactOracle.SegmentsIntersect(
                            values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
                        result.Check(parametric == expected, "Parametric oracle: " + description);
                        bool largeIntegers = ExactOracle.SegmentsIntersectWithBigInteger(
                            values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
                        result.Check(largeIntegers == expected, "BigInteger oracle: " + description);
                        bool orientation = OrientationFormulation.SegmentsIntersect(
                            values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
                        result.Check(orientation == expected, "Orientation oracle: " + description);
                        // The detector requires a nonzero first segment; its rectangle sides may collapse.
                        if (values[0] != values[2] || values[1] != values[3]) {
                            bool detector = DetectorInternals.SegmentsIntersect(
                                values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
                            result.Check(detector == expected, "Detector predicate: " + description);
                        }
                    }
                }
            }
        }
    }

    private static void ApplySymmetry(Span<float> values, int symmetry) {
        if ((symmetry & 1) != 0) {
            // Swap the two segments.
            for (int index = 0; index < 4; ++index) {
                (values[index], values[index + 4]) = (values[index + 4], values[index]);
            }
        }

        if ((symmetry & 2) != 0) {
            (values[0], values[2]) = (values[2], values[0]);
            (values[1], values[3]) = (values[3], values[1]);
        }

        if ((symmetry & 4) != 0) {
            (values[4], values[6]) = (values[6], values[4]);
            (values[5], values[7]) = (values[7], values[5]);
        }

        if ((symmetry & 8) != 0) {
            // Transpose X and Y.
            for (int index = 0; index < 8; index += 2) {
                (values[index], values[index + 1]) = (values[index + 1], values[index]);
            }
        }

        if ((symmetry & 16) != 0) {
            // Mirror X.
            for (int index = 0; index < 8; index += 2) {
                values[index] = -values[index];
            }
        }
    }

    private static void FormulationsAgree(SuiteResult result, ValidationContext context) {
        Random random = new(7001);
        int count = context.Scaled(400_000);
        long fastPath = 0;
        Span<float> values = stackalloc float[8];
        for (int iteration = 0; iteration < count; ++iteration) {
            int style = iteration % 4;
            for (int index = 0; index < 8; ++index) {
                values[index] = style switch {
                    // Small lattices produce many touching and collinear configurations.
                    0 => random.Next(-4, 5),
                    1 => random.Next(-64, 65) * 0.25f,
                    2 => (float)((random.NextDouble() * 2.0 - 1.0) * 300.0),
                    _ => RandomFinite(random),
                };
            }

            bool parametric = ExactOracle.SegmentsIntersectWithBigInteger(
                values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
            bool orientation = OrientationFormulation.SegmentsIntersect(
                values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
            result.Check(parametric == orientation, "Formulations disagree: " + Describe(values));
            if (ExactOracle.TrySegmentsIntersectWithInt128(
                    values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7],
                    out bool fast)) {
                ++fastPath;
                result.Check(fast == parametric, "128-bit path disagrees: " + Describe(values));
            }
        }

        result.Fact("Cases that fit the 128-bit path", fastPath);
    }

    private static void Trigonometry(SuiteResult result) {
        BigInteger one = HighPrecision.OneValue;
        BigInteger tolerance = one >> 490;
        BigInteger knownPi = HighPrecision.FromDecimal("3.14159265358979323846264338327950288419716939937510");
        result.Check(
            BigInteger.Abs(HighPrecision.PiValue - knownPi) < (one >> 160),
            "Pi differs from its known first fifty decimals.");

        HighPrecision.SineCosine(one, out BigInteger sineOfOne, out BigInteger cosineOfOne);
        result.Check(
            BigInteger.Abs(sineOfOne - HighPrecision.FromDecimal("0.841470984807896506652502321630")) < (one >> 95),
            "Sine of one differs from its known first thirty decimals.");
        result.Check(
            BigInteger.Abs(cosineOfOne - HighPrecision.FromDecimal("0.540302305868139717400936607442")) < (one >> 95),
            "Cosine of one differs from its known first thirty decimals.");

        HighPrecision.SineCosine(HighPrecision.PiValue / 6, out BigInteger sineOfSixth, out BigInteger cosineOfSixth);
        result.Check(BigInteger.Abs(2 * sineOfSixth - one) < tolerance, "Sine of pi/6 is not one half.");
        result.Check(
            BigInteger.Abs(((cosineOfSixth * cosineOfSixth) >> HighPrecision.FractionBits) * 4 - 3 * one) < tolerance,
            "Cosine of pi/6 squared is not three quarters.");

        Random random = new(7002);
        double worstLibraryDifference = 0.0;
        for (int iteration = 0; iteration < 4000; ++iteration) {
            float angle = iteration switch {
                0 => 0f,
                1 => MathF.PI,
                2 => -MathF.PI,
                3 => MathF.PI / 2f,
                4 => float.Epsilon,
                5 => 1e30f,
                6 => -3.4e38f,
                _ => iteration % 3 == 0
                    ? RandomFinite(random)
                    : (float)((random.NextDouble() * 2.0 - 1.0) * 4.0 * Math.PI),
            };
            HighPrecision.SineCosine(HighPrecision.FromSingle(angle), out BigInteger sine, out BigInteger cosine);
            BigInteger identity = ((sine * sine + cosine * cosine) >> HighPrecision.FractionBits) - one;
            result.Check(
                BigInteger.Abs(identity) < tolerance,
                $"Sine squared plus cosine squared is not one for angle {Exact.Text(angle)}.");
            if (MathF.Abs(angle) <= 16f) {
                double difference = Math.Max(
                    Math.Abs(HighPrecision.ToDouble(sine) - Math.Sin(angle)),
                    Math.Abs(HighPrecision.ToDouble(cosine) - Math.Cos(angle)));
                worstLibraryDifference = Math.Max(worstLibraryDifference, difference);
                result.Check(
                    difference < 4e-16,
                    $"Platform sine or cosine differs by {difference:R} for angle {Exact.Text(angle)}.");
            }
        }

        result.Fact("Largest difference from the platform library within sixteen radians", worstLibraryDifference);
    }

    internal static float RandomFinite(Random random) {
        int bits = (int)random.NextInt64(0, 0x7f800000);
        if (random.Next(2) != 0) {
            bits |= int.MinValue;
        }

        return BitConverter.Int32BitsToSingle(bits);
    }

    private static string Describe(ReadOnlySpan<float> values) {
        string text = string.Empty;
        foreach (float value in values) {
            text += Exact.Text(value) + " ";
        }

        return text;
    }
}

/// <summary>
/// The textbook orientation formulation with exact shoelace areas. It exists only to
/// cross-check the parametric oracle; the two share no intermediate quantity.
/// </summary>
internal static class OrientationFormulation {
    public static bool SegmentsIntersect(
        float ax, float ay, float bx, float by, float cx, float cy, float dx, float dy) {
        int first = ExactOracle.OrientationSign(cx, cy, dx, dy, ax, ay);
        int second = ExactOracle.OrientationSign(cx, cy, dx, dy, bx, by);
        int third = ExactOracle.OrientationSign(ax, ay, bx, by, cx, cy);
        int fourth = ExactOracle.OrientationSign(ax, ay, bx, by, dx, dy);
        if (first * second < 0 && third * fourth < 0) {
            return true;
        }

        return (first == 0 && Within(cx, cy, dx, dy, ax, ay))
            || (second == 0 && Within(cx, cy, dx, dy, bx, by))
            || (third == 0 && Within(ax, ay, bx, by, cx, cy))
            || (fourth == 0 && Within(ax, ay, bx, by, dx, dy));
    }

    private static bool Within(float ax, float ay, float bx, float by, float px, float py) {
        return px >= MathF.Min(ax, bx) && px <= MathF.Max(ax, bx)
            && py >= MathF.Min(ay, by) && py <= MathF.Max(ay, by);
    }
}
