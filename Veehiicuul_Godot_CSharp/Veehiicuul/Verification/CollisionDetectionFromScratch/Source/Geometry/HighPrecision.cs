using System;
using System.Numerics;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>
/// Fixed-point arithmetic with 512 fractional bits, including sine and cosine
/// computed from their Taylor series. It gives the unrounded rectangle corners
/// that the documented pose convention describes, without the platform
/// mathematics library and without binary32 or binary64 rounding.
/// </summary>
internal static class HighPrecision {
    public const int FractionBits = 512;
    private const int GuardBits = 64;

    private static readonly BigInteger One = BigInteger.One << FractionBits;
    private static readonly BigInteger Pi = ComputePi();
    private static readonly BigInteger HalfPi = Pi >> 1;

    public static BigInteger OneValue => One;
    public static BigInteger PiValue => Pi;

    /// <summary>Parses a decimal fraction such as "0.8414709848".</summary>
    public static BigInteger FromDecimal(string text) {
        bool negative = text.StartsWith('-');
        string digits = negative ? text[1..] : text;
        int point = digits.IndexOf('.');
        int fractionDigits = point < 0 ? 0 : digits.Length - point - 1;
        BigInteger numerator = BigInteger.Parse(
            digits.Replace(".", string.Empty, StringComparison.Ordinal),
            System.Globalization.CultureInfo.InvariantCulture);
        BigInteger value = (numerator << FractionBits) / BigInteger.Pow(10, fractionDigits);
        return negative ? -value : value;
    }

    public static BigInteger FromSingle(float value) {
        return ExactNumber.ToScaledInteger(value) << (FractionBits - ExactNumber.SingleScaleBits);
    }

    public static double ToDouble(BigInteger value) {
        // Keep 128 significant fractional bits before converting.
        return (double)(value >> (FractionBits - 128)) / Math.Pow(2.0, 128);
    }

    public static void SineCosine(BigInteger angle, out BigInteger sine, out BigInteger cosine) {
        BigInteger quadrant = FloorDivide(2 * angle + HalfPi, 2 * HalfPi);
        BigInteger reduced = angle - quadrant * HalfPi;
        BigInteger squared = (reduced * reduced) >> FractionBits;

        BigInteger reducedSine = reduced;
        BigInteger term = reduced;
        for (int index = 1; !term.IsZero; ++index) {
            term = -((term * squared) >> FractionBits) / ((2 * index) * (2 * index + 1));
            reducedSine += term;
        }

        BigInteger reducedCosine = One;
        term = One;
        for (int index = 1; !term.IsZero; ++index) {
            term = -((term * squared) >> FractionBits) / ((2 * index - 1) * (2 * index));
            reducedCosine += term;
        }

        int remainder = (int)(((quadrant % 4) + 4) % 4);
        switch (remainder) {
        case 0:
            sine = reducedSine;
            cosine = reducedCosine;
            break;
        case 1:
            sine = reducedCosine;
            cosine = -reducedSine;
            break;
        case 2:
            sine = -reducedSine;
            cosine = -reducedCosine;
            break;
        default:
            sine = -reducedCosine;
            cosine = reducedSine;
            break;
        }
    }

    /// <summary>
    /// Unrounded corners in the detector's order: (MinX, MinY), (MaxX, MinY),
    /// (MaxX, MaxY), (MinX, MaxY). Positive yaw turns the rectangle clockwise
    /// when X points right and Y points up.
    /// </summary>
    public static BigInteger[] IdealCorners(RectangleLocalBounds bounds, RectanglePose pose) {
        SineCosine(FromSingle(pose.RotationRadians), out BigInteger sine, out BigInteger cosine);
        BigInteger positionX = FromSingle(pose.PositionX);
        BigInteger positionY = FromSingle(pose.PositionY);
        float[] localX = [bounds.MinX, bounds.MaxX, bounds.MaxX, bounds.MinX];
        float[] localY = [bounds.MinY, bounds.MinY, bounds.MaxY, bounds.MaxY];
        BigInteger[] corners = new BigInteger[8];
        for (int index = 0; index < 4; ++index) {
            BigInteger x = FromSingle(localX[index]);
            BigInteger y = FromSingle(localY[index]);
            // Clockwise rotation by yaw: (x, y) maps to (x cos + y sin, y cos - x sin).
            corners[2 * index] = positionX + ((x * cosine + y * sine) >> FractionBits);
            corners[2 * index + 1] = positionY + ((y * cosine - x * sine) >> FractionBits);
        }

        return corners;
    }

    /// <summary>Distance from an unrounded value to a binary32 value, in units of that value's spacing.</summary>
    public static double ErrorInUnitsInLastPlace(BigInteger ideal, float rounded) {
        ExactNumber.Decompose(rounded, out int significand, out int exponent);
        if (significand != 0 && Math.Abs(significand) == 0x800000 && exponent > -ExactNumber.SingleScaleBits) {
            // At a power of two the spacing below is half the spacing above; use the finer one
            // when the ideal value lies on the inner side.
            BigInteger difference = FromSingle(rounded) - ideal;
            bool idealIsInside = significand > 0 ? difference.Sign > 0 : difference.Sign < 0;
            if (idealIsInside) {
                --exponent;
            }
        }

        BigInteger spacing = BigInteger.One << (FractionBits + exponent);
        BigInteger error = BigInteger.Abs(FromSingle(rounded) - ideal);
        return (double)((error << 64) / spacing) / Math.Pow(2.0, 64);
    }

    private static BigInteger ComputePi() {
        int bits = FractionBits + GuardBits;
        BigInteger value = 16 * ArcTangentOfReciprocal(5, bits) - 4 * ArcTangentOfReciprocal(239, bits);
        return value >> GuardBits;
    }

    private static BigInteger ArcTangentOfReciprocal(int denominator, int bits) {
        BigInteger power = (BigInteger.One << bits) / denominator;
        BigInteger sum = power;
        BigInteger square = (BigInteger)denominator * denominator;
        for (int index = 1; !power.IsZero; ++index) {
            power /= square;
            BigInteger term = power / (2 * index + 1);
            sum += (index & 1) == 0 ? term : -term;
        }

        return sum;
    }

    private static BigInteger FloorDivide(BigInteger dividend, BigInteger divisor) {
        BigInteger quotient = BigInteger.DivRem(dividend, divisor, out BigInteger remainder);
        if (!remainder.IsZero && (remainder.Sign < 0) != (divisor.Sign < 0)) {
            quotient -= BigInteger.One;
        }

        return quotient;
    }
}
