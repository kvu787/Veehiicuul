using System;
using System.Numerics;

namespace CollisionDetectionFromScratch;

/// <summary>Exact integer views of finite binary32 values.</summary>
internal static class ExactNumber {
    /// <summary>Every finite binary32 value is an integer multiple of two to the power of minus 149.</summary>
    public const int SingleScaleBits = 149;

    /// <summary>Returns the value multiplied by two to the power of 149, which is always an integer.</summary>
    public static BigInteger ToScaledInteger(float value) {
        Decompose(value, out int significand, out int exponent);
        return new BigInteger(significand) << (exponent + SingleScaleBits);
    }

    /// <summary>Writes the value as significand times two to the power of exponent.</summary>
    public static void Decompose(float value, out int significand, out int exponent) {
        if (!float.IsFinite(value)) {
            throw new ArgumentOutOfRangeException(nameof(value), "Only finite values have an exact integer view.");
        }

        int bits = BitConverter.SingleToInt32Bits(value);
        int exponentField = (bits >> 23) & 0xff;
        int fraction = bits & 0x7fffff;
        if (exponentField == 0) {
            significand = fraction;
            exponent = -SingleScaleBits;
        } else {
            significand = fraction | 0x800000;
            exponent = exponentField - 150;
        }

        if (bits < 0) {
            significand = -significand;
        }
    }

    /// <summary>Spacing between the value and the next binary32 value of larger magnitude.</summary>
    public static double UnitInLastPlace(float value) {
        float magnitude = MathF.Abs(value);
        return (double)MathF.BitIncrement(magnitude) - magnitude;
    }

    /// <summary>Moves a value by a signed number of representable binary32 steps.</summary>
    public static float Step(float value, int steps) {
        float result = value;
        for (int index = 0; index < Math.Abs(steps); ++index) {
            result = steps > 0 ? MathF.BitIncrement(result) : MathF.BitDecrement(result);
        }

        return result;
    }
}
