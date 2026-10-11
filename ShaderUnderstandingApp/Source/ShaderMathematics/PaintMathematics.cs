namespace ShaderUnderstanding;

public enum PaintModel { Original, LinearWarp }
public enum PreviewStage { Paint, Facing, Tone, Normal }

public readonly record struct Direction(double X, double Y, double Z) {
    public double Length => Math.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z);
    public Direction Unit => this / this.Length;
    public static Direction operator *(Direction value, double scale) => new(value.X * scale, value.Y * scale, value.Z * scale);
    public static Direction operator /(Direction value, double scale) => value * (1.0 / scale);
    public static Direction operator +(Direction left, Direction right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
}

public readonly record struct PaintColor(double Red, double Green, double Blue) {
    public static PaintColor operator *(PaintColor color, double scale) => new(color.Red * scale, color.Green * scale, color.Blue * scale);
    public static PaintColor operator +(PaintColor left, PaintColor right) => new(left.Red + right.Red, left.Green + right.Green, left.Blue + right.Blue);
    public static PaintColor operator /(PaintColor left, PaintColor right) => new(left.Red / right.Red, left.Green / right.Green, left.Blue / right.Blue);
    public PaintColor Decode() => new(PaintMath.DecodeSrgb(this.Red), PaintMath.DecodeSrgb(this.Green), PaintMath.DecodeSrgb(this.Blue));
    public PaintColor Encode() => new(PaintMath.EncodeSrgb(this.Red), PaintMath.EncodeSrgb(this.Green), PaintMath.EncodeSrgb(this.Blue));
    public double MaximumDifference(PaintColor other) => Math.Max(Math.Abs(this.Red - other.Red), Math.Max(Math.Abs(this.Green - other.Green), Math.Abs(this.Blue - other.Blue)));
}

public sealed record PaintParameters {
    public const double Margin = 1.0 / 1024.0;
    public const double MaximumInterior = 1.0 - Margin;
    public PaintColor BaseColor { get; init; } = new(0.107, 0.223, 0.578);
    public double Brightness { get; init; } = 0.5;
    public double Shift { get; init; } = 0.6;
    public double RotationDegrees { get; init; } = 45;
    public double DarkPoint { get; init; }
    public double LightPoint { get; init; } = 1;

    public void Validate() {
        Check(this.BaseColor.Red, Margin, MaximumInterior, "Red");
        Check(this.BaseColor.Green, Margin, MaximumInterior, "Green");
        Check(this.BaseColor.Blue, Margin, MaximumInterior, "Blue");
        Check(this.Brightness, Margin, MaximumInterior, "Brightness");
        Check(this.Shift, 0, MaximumInterior, "Shift");
        Check(this.DarkPoint, 0, MaximumInterior, "Dark Point");
        Check(this.LightPoint, Margin, 1, "Light Point");
        Check(this.RotationDegrees, 0, Math.BitDecrement(360), "Rotation");
    }

    private static void Check(double value, double minimum, double maximum, string name) {
        if (!double.IsFinite(value) || value < minimum || value > maximum)
            throw new ArgumentOutOfRangeException(name, $"{name} must be finite and in [{minimum:G12}, {maximum:G12}].");
    }
}

public readonly record struct FacingWeights(double Light, double Dark) {
    public double Facing => this.Light / (this.Light + this.Dark);
}

public readonly record struct PaintSample(Direction ViewNormal, Direction RotatedNormal, Direction WarpedNormal,
    FacingWeights Weights, double Facing, double Tone, PaintColor LinearColor) {
    public PaintColor DisplayColor => this.LinearColor.Encode();
}

// Double-precision educational evaluation of the canonical module's real-number
// equations. The stable original P/Q construction is translated from
// 3dTestScene_CppDx12/Source/SimplePaint/SimplePaintCore.hlsli. Material
// composition follows Material.cpp. This is not a GPU timing or binary32 emulator.
// Original facing-lobe and anchored color-curve construction: Kevin Vu.
public sealed class CompiledPaint {
    public PaintParameters Parameters { get; }
    public double Height { get; }
    private readonly double cosine;
    private readonly double sine;
    private readonly double shiftRatio;
    private readonly PaintColor numeratorDark;
    private readonly PaintColor numeratorLight;
    private readonly PaintColor complementDark;
    private readonly PaintColor complementLight;
    private readonly PaintColor curveNumerator;
    private readonly PaintColor curveComplement;

    public CompiledPaint(PaintParameters parameters) {
        parameters.Validate();
        this.Parameters = parameters;
        double radians = -parameters.RotationDegrees * Math.PI / 180;
        this.cosine = Math.Cos(radians);
        this.sine = Math.Sin(radians);
        this.Height = Math.Sqrt((1 - parameters.Shift) * (1 + parameters.Shift));
        this.shiftRatio = Math.Sqrt((1 - parameters.Shift) / (1 + parameters.Shift));
        PaintColor color = parameters.BaseColor.Decode();
        this.curveNumerator = color * parameters.Brightness;
        this.curveComplement = new PaintColor(1 - color.Red, 1 - color.Green, 1 - color.Blue) * (1 - parameters.Brightness);
        PaintColor first = this.curveNumerator * parameters.DarkPoint;
        PaintColor second = this.curveNumerator * parameters.LightPoint;
        PaintColor third = this.curveComplement * (1 - parameters.DarkPoint);
        PaintColor fourth = this.curveComplement * (1 - parameters.LightPoint);
        PaintColor scale = new(Maximum(first.Red, second.Red, third.Red, fourth.Red),
            Maximum(first.Green, second.Green, third.Green, fourth.Green),
            Maximum(first.Blue, second.Blue, third.Blue, fourth.Blue));
        this.numeratorDark = first / scale;
        this.numeratorLight = second / scale;
        this.complementDark = third / scale;
        this.complementLight = fourth / scale;
    }

    public Direction Rotate(Direction normal) => this.Parameters.Shift == 0 ? normal : new(
        normal.X * this.cosine - normal.Y * this.sine,
        normal.X * this.sine + normal.Y * this.cosine, normal.Z);

    public Direction Warp(Direction rotatedNormal) => new(
        this.Height * rotatedNormal.X - this.Parameters.Shift * rotatedNormal.Z,
        this.Height * rotatedNormal.Y, rotatedNormal.Z);

    public Direction Peak {
        get {
            double angle = this.Parameters.RotationDegrees * Math.PI / 180;
            return new(this.Parameters.Shift * Math.Cos(angle), this.Parameters.Shift * Math.Sin(angle), this.Height);
        }
    }

    public PaintSample Evaluate(Direction normal, PaintModel model) {
        if (!double.IsFinite(normal.X) || !double.IsFinite(normal.Y) || !double.IsFinite(normal.Z) || normal.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(normal), "The normal must be finite and nonzero.");
        Direction rotated = this.Rotate(normal);
        Direction warped = model == PaintModel.LinearWarp ? this.Warp(rotated) : rotated;
        FacingWeights weights = model == PaintModel.Original
            ? this.OriginalWeights(rotated)
            : new(warped.Z * warped.Z, warped.X * warped.X + warped.Y * warped.Y);
        double tone = (this.Parameters.DarkPoint * weights.Dark + this.Parameters.LightPoint * weights.Light) / (weights.Light + weights.Dark);
        return new(normal, rotated, warped, weights, weights.Facing, tone, this.ColorForWeights(weights));
    }

    public double ToneAtFacing(double facing) => this.Parameters.DarkPoint * (1 - facing) + this.Parameters.LightPoint * facing;

    // Plot samples start from facing. Normal evaluation keeps separate P/Q
    // weights so a rounded facing cannot erase a tiny highlight complement.
    public PaintColor ColorAtFacing(double facing) => this.ColorForWeights(new(facing, 1 - facing));

    public PaintColor Curve(double tone) {
        PaintColor numerator = this.curveNumerator * tone;
        return numerator / (numerator + this.curveComplement * (1 - tone));
    }

    private PaintColor ColorForWeights(FacingWeights weights) {
        PaintColor numerator = this.numeratorDark * weights.Dark + this.numeratorLight * weights.Light;
        PaintColor complement = this.complementDark * weights.Dark + this.complementLight * weights.Light;
        return numerator / (numerator + complement);
    }

    private FacingWeights OriginalWeights(Direction normal) {
        if (normal.Z <= 0) return new(0, 1);
        double length = normal.Length;
        if (this.Parameters.Shift == 0)
            return new(normal.Z, (normal.X * normal.X + normal.Y * normal.Y) / (length + normal.Z));
        double scale = Math.Max(Math.Abs(normal.X), normal.Z);
        double x = normal.X / scale;
        double z = normal.Z / scale;
        double sliceLength = Math.Sqrt(x * x + z * z);
        double radius = scale * sliceLength;
        double first = this.shiftRatio * (normal.X >= 0 ? sliceLength + x : z);
        double second = normal.X >= 0 ? z : sliceLength - x;
        double squareSum = first * first + second * second;
        double difference = first - second;
        return new(radius * 2 * first * second,
            normal.Y * normal.Y / (length + radius) * squareSum + radius * difference * difference);
    }

    private static double Maximum(double first, double second, double third, double fourth) => Math.Max(Math.Max(first, second), Math.Max(third, fourth));
}

public static class PaintMath {
    public static double DecodeSrgb(double channel) => channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    public static double EncodeSrgb(double channel) => channel <= 0.0031308 ? 12.92 * channel : 1.055 * Math.Pow(channel, 1.0 / 2.4) - 0.055;

    public static Direction Sweep(double tiltDegrees, double azimuthDegrees) {
        double tilt = tiltDegrees * Math.PI / 180;
        double azimuth = azimuthDegrees * Math.PI / 180;
        return new(Math.Sin(tilt) * Math.Cos(azimuth), Math.Sin(tilt) * Math.Sin(azimuth), Math.Cos(tilt));
    }

    public static Direction? SphereNormal(double horizontal, double vertical) {
        double squared = horizontal * horizontal + vertical * vertical;
        if (squared > 1) return null;
        return new(horizontal, vertical, Math.Sqrt(Math.Max(0, 1 - squared)));
    }
}
