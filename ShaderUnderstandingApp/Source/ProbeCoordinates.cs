namespace ShaderUnderstanding;

internal enum ProbeMovement { Free, TiltOnly, SliceOnly }

// Coordinates of the facing graph, before the shader's linear warp. A slice
// keeps rotated Y fixed; tilt sweeps the circle in the rotated XZ plane.
internal readonly record struct ProbeCoordinates(double Slice, double Tilt) {
    public static ProbeCoordinates FromNormal(Direction normal, double rotationDegrees, double fallbackTilt = 0) {
        double rotation = rotationDegrees * Math.PI / 180;
        double x = normal.X * Math.Cos(rotation) + normal.Y * Math.Sin(rotation);
        double y = -normal.X * Math.Sin(rotation) + normal.Y * Math.Cos(rotation);
        double tilt = Math.Abs(x) + Math.Abs(normal.Z) < 1e-12 ? fallbackTilt : Math.Atan2(x, normal.Z);
        return new(Math.Clamp(y, -1, 1), tilt);
    }

    public Direction ToNormal(double rotationDegrees) {
        double radius = Math.Sqrt(Math.Max(0, (1 - this.Slice) * (1 + this.Slice)));
        double x = radius * Math.Sin(this.Tilt);
        double rotation = rotationDegrees * Math.PI / 180;
        return new(x * Math.Cos(rotation) - this.Slice * Math.Sin(rotation),
            x * Math.Sin(rotation) + this.Slice * Math.Cos(rotation), radius * Math.Cos(this.Tilt));
    }

    public static Direction Constrain(Direction current, Direction candidate, double rotationDegrees, ProbeMovement movement, double fallbackTilt = 0) {
        if (movement == ProbeMovement.Free) return candidate;
        // A back probe selected on the graph returns to the front when the
        // user interacts with a sphere. The sphere previews always show front.
        ProbeCoordinates anchor = FromNormal(new(current.X, current.Y, Math.Abs(current.Z)), rotationDegrees, fallbackTilt);
        ProbeCoordinates target = FromNormal(candidate, rotationDegrees, anchor.Tilt);
        return (movement == ProbeMovement.TiltOnly ? anchor with { Tilt = target.Tilt }
            : anchor with { Slice = target.Slice }).ToNormal(rotationDegrees);
    }
}
