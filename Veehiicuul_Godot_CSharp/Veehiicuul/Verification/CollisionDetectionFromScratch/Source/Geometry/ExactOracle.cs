using System;
using System.Numerics;

namespace CollisionDetectionFromScratch;

/// <summary>
/// Exact closed-segment intersection for binary32 endpoints.
///
/// The test solves P + t R = Q + u S for t and u as exact rational numbers and
/// handles parallel, collinear, and zero-length inputs explicitly. It shares no
/// formulation with the orientation-sign predicate used by the detector.
/// </summary>
internal static class ExactOracle {
    // Operands below two to the power of 60 keep every intermediate below two to the power of 124.
    private const int MaximumFastShift = 36;

    public static bool SegmentsIntersect(
        float ax, float ay, float bx, float by,
        float cx, float cy, float dx, float dy) {
        if (TryScaleTogether(ax, ay, bx, by, cx, cy, dx, dy, out Int128Coordinates fast)) {
            return Intersect(fast.Ax, fast.Ay, fast.Bx, fast.By, fast.Cx, fast.Cy, fast.Dx, fast.Dy);
        }

        return SegmentsIntersectWithBigInteger(ax, ay, bx, by, cx, cy, dx, dy);
    }

    /// <summary>Same test restricted to 128-bit integers; false when the exponent span is too wide.</summary>
    public static bool TrySegmentsIntersectWithInt128(
        float ax, float ay, float bx, float by,
        float cx, float cy, float dx, float dy,
        out bool intersects) {
        if (TryScaleTogether(ax, ay, bx, by, cx, cy, dx, dy, out Int128Coordinates fast)) {
            intersects = Intersect(fast.Ax, fast.Ay, fast.Bx, fast.By, fast.Cx, fast.Cy, fast.Dx, fast.Dy);
            return true;
        }

        intersects = false;
        return false;
    }

    public static bool SegmentsIntersectWithBigInteger(
        float ax, float ay, float bx, float by,
        float cx, float cy, float dx, float dy) {
        return Intersect(
            ExactNumber.ToScaledInteger(ax), ExactNumber.ToScaledInteger(ay),
            ExactNumber.ToScaledInteger(bx), ExactNumber.ToScaledInteger(by),
            ExactNumber.ToScaledInteger(cx), ExactNumber.ToScaledInteger(cy),
            ExactNumber.ToScaledInteger(dx), ExactNumber.ToScaledInteger(dy));
    }

    /// <summary>Sign of the doubled signed area of triangle a, b, c. Positive is counterclockwise.</summary>
    public static int OrientationSign(float ax, float ay, float bx, float by, float cx, float cy) {
        BigInteger axi = ExactNumber.ToScaledInteger(ax), ayi = ExactNumber.ToScaledInteger(ay);
        BigInteger bxi = ExactNumber.ToScaledInteger(bx), byi = ExactNumber.ToScaledInteger(by);
        BigInteger cxi = ExactNumber.ToScaledInteger(cx), cyi = ExactNumber.ToScaledInteger(cy);
        // Shoelace form, deliberately different from a difference-of-products cross product.
        BigInteger doubledArea = axi * (byi - cyi) + bxi * (cyi - ayi) + cxi * (ayi - byi);
        return doubledArea.Sign;
    }

    private static bool Intersect<T>(T px, T py, T p2x, T p2y, T qx, T qy, T q2x, T q2y)
        where T : INumber<T> {
        T rx = p2x - px;
        T ry = p2y - py;
        T sx = q2x - qx;
        T sy = q2y - qy;
        T qpx = qx - px;
        T qpy = qy - py;
        T rCrossS = rx * sy - ry * sx;
        T qpCrossR = qpx * ry - qpy * rx;
        T qpCrossS = qpx * sy - qpy * sx;
        if (T.IsZero(rCrossS)) {
            bool rIsPoint = T.IsZero(rx) && T.IsZero(ry);
            bool sIsPoint = T.IsZero(sx) && T.IsZero(sy);
            if (rIsPoint && sIsPoint) {
                return T.IsZero(qpx) && T.IsZero(qpy);
            }

            if (rIsPoint) {
                // P must lie on the line of S and project inside it.
                if (!T.IsZero(qpCrossS)) {
                    return false;
                }

                T alongS = -(qpx * sx + qpy * sy);
                return alongS >= T.Zero && alongS <= sx * sx + sy * sy;
            }

            if (!T.IsZero(qpCrossR)) {
                return false;
            }

            T rSquared = rx * rx + ry * ry;
            T start = qpx * rx + qpy * ry;
            if (sIsPoint) {
                return start >= T.Zero && start <= rSquared;
            }

            T end = start + sx * rx + sy * ry;
            T low = T.Min(start, end);
            T high = T.Max(start, end);
            return high >= T.Zero && low <= rSquared;
        }

        if (rCrossS > T.Zero) {
            return qpCrossS >= T.Zero && qpCrossS <= rCrossS
                && qpCrossR >= T.Zero && qpCrossR <= rCrossS;
        }

        return qpCrossS <= T.Zero && qpCrossS >= rCrossS
            && qpCrossR <= T.Zero && qpCrossR >= rCrossS;
    }

    private static bool TryScaleTogether(
        float ax, float ay, float bx, float by,
        float cx, float cy, float dx, float dy,
        out Int128Coordinates coordinates) {
        Span<int> significands = stackalloc int[8];
        Span<int> exponents = stackalloc int[8];
        ExactNumber.Decompose(ax, out significands[0], out exponents[0]);
        ExactNumber.Decompose(ay, out significands[1], out exponents[1]);
        ExactNumber.Decompose(bx, out significands[2], out exponents[2]);
        ExactNumber.Decompose(by, out significands[3], out exponents[3]);
        ExactNumber.Decompose(cx, out significands[4], out exponents[4]);
        ExactNumber.Decompose(cy, out significands[5], out exponents[5]);
        ExactNumber.Decompose(dx, out significands[6], out exponents[6]);
        ExactNumber.Decompose(dy, out significands[7], out exponents[7]);
        int minimum = int.MaxValue;
        for (int index = 0; index < 8; ++index) {
            if (significands[index] != 0 && exponents[index] < minimum) {
                minimum = exponents[index];
            }
        }

        Span<Int128> scaled = stackalloc Int128[8];
        for (int index = 0; index < 8; ++index) {
            if (significands[index] == 0) {
                scaled[index] = Int128.Zero;
                continue;
            }

            int shift = exponents[index] - minimum;
            if (shift > MaximumFastShift) {
                coordinates = default;
                return false;
            }

            scaled[index] = (Int128)significands[index] << shift;
        }

        coordinates = new Int128Coordinates(
            scaled[0], scaled[1], scaled[2], scaled[3], scaled[4], scaled[5], scaled[6], scaled[7]);
        return true;
    }

    private readonly record struct Int128Coordinates(
        Int128 Ax, Int128 Ay, Int128 Bx, Int128 By, Int128 Cx, Int128 Cy, Int128 Dx, Int128 Dy);
}
