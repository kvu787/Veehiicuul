using System;
using System.Collections.Generic;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>
/// Procedurally generated barrier outlines. No track in this harness comes
/// from a file; every vertex is computed from the parameters and the seed.
/// </summary>
internal sealed class SyntheticTrack {
    private readonly double[] _centerlineX;
    private readonly double[] _centerlineY;
    private readonly double[] _centerlineLength;

    private SyntheticTrack(
        string name,
        string description,
        List<List<CoordinateXY>> outlines,
        double[] centerlineX,
        double[] centerlineY,
        double halfWidth) {
        this.Name = name;
        this.Description = description;
        this.Collider = new ColliderJson();
        foreach (List<CoordinateXY> vertices in outlines) {
            this.Collider.Outlines.Add(new Outline { Vertices = vertices });
        }

        this.Oracle = new OracleTrack(this.Collider);
        this.HalfWidth = halfWidth;
        this._centerlineX = centerlineX;
        this._centerlineY = centerlineY;
        this._centerlineLength = new double[centerlineX.Length + 1];
        for (int index = 0; index < centerlineX.Length; ++index) {
            int next = (index + 1) % centerlineX.Length;
            double dx = centerlineX[next] - centerlineX[index];
            double dy = centerlineY[next] - centerlineY[index];
            this._centerlineLength[index + 1] = this._centerlineLength[index] + Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public string Name { get; }
    public string Description { get; }
    public ColliderJson Collider { get; }
    public OracleTrack Oracle { get; }

    /// <summary>Nominal half width of the drivable band; zero for tracks without a centerline.</summary>
    public double HalfWidth { get; }

    public bool HasCenterline => this._centerlineX.Length > 0;
    public double LapLength => this._centerlineLength[^1];
    public int EdgeCount => this.Oracle.EdgeCount;

    /// <summary>
    /// A closed circuit between two star-shaped outlines. The centerline radius
    /// is a mean radius modulated by four low harmonics chosen from the seed.
    /// </summary>
    public static SyntheticTrack Circuit(
        string name,
        int seed,
        double meanRadius,
        double halfWidth,
        double outerSpacing,
        double innerSpacing,
        double scale = 1.0,
        double offsetX = 0.0,
        double offsetY = 0.0,
        double modulation = 0.18) {
        Random random = new(seed);
        double[] amplitude = new double[4];
        double[] phase = new double[4];
        for (int index = 0; index < amplitude.Length; ++index) {
            amplitude[index] = modulation * (random.NextDouble() * 2.0 - 1.0) / (index + 1);
            phase[index] = random.NextDouble() * Math.Tau;
        }

        double Radius(double angle) {
            double factor = 1.0;
            for (int index = 0; index < amplitude.Length; ++index) {
                factor += amplitude[index] * Math.Cos((index + 2) * angle + phase[index]);
            }

            return meanRadius * factor;
        }

        List<CoordinateXY> Ring(double radialOffset, double spacing) {
            int count = Math.Max(8, (int)Math.Round(Math.Tau * (meanRadius + radialOffset) / spacing));
            List<CoordinateXY> vertices = new(count);
            for (int index = 0; index < count; ++index) {
                double angle = Math.Tau * index / count;
                double radius = Radius(angle) + radialOffset;
                if (!(radius > 0.0)) {
                    throw new ArgumentException("The half width must be smaller than every centerline radius.");
                }

                vertices.Add(new CoordinateXY(
                    (float)(offsetX + scale * radius * Math.Cos(angle)),
                    (float)(offsetY + scale * radius * Math.Sin(angle))));
            }

            RemoveRepeatedNeighbors(vertices);
            return vertices;
        }

        const int centerlineSamples = 8192;
        double[] centerlineX = new double[centerlineSamples];
        double[] centerlineY = new double[centerlineSamples];
        for (int index = 0; index < centerlineSamples; ++index) {
            double angle = Math.Tau * index / centerlineSamples;
            double radius = Radius(angle);
            centerlineX[index] = offsetX + scale * radius * Math.Cos(angle);
            centerlineY[index] = offsetY + scale * radius * Math.Sin(angle);
        }

        List<List<CoordinateXY>> outlines = [Ring(halfWidth, outerSpacing), Ring(-halfWidth, innerSpacing)];
        string description = $"circuit seed={seed} meanRadius={meanRadius} halfWidth={halfWidth} "
            + $"outerSpacing={outerSpacing} innerSpacing={innerSpacing} scale={scale} "
            + $"offset=({offsetX}, {offsetY})";
        return new SyntheticTrack(name, description, outlines, centerlineX, centerlineY, halfWidth * scale);
    }

    /// <summary>
    /// Two axis-aligned rectangular outlines. With a segment length of zero each
    /// side is a single long edge. All coordinates are exact multiples of 1/4.
    /// </summary>
    public static SyntheticTrack Rectangular(
        string name, double width, double height, double halfWidth, double segmentLength) {
        List<CoordinateXY> Ring(double halfX, double halfY) {
            List<CoordinateXY> vertices = [];
            (double X, double Y)[] corners = [(-halfX, -halfY), (halfX, -halfY), (halfX, halfY), (-halfX, halfY)];
            for (int side = 0; side < 4; ++side) {
                (double startX, double startY) = corners[side];
                (double endX, double endY) = corners[(side + 1) % 4];
                double length = Math.Abs(endX - startX) + Math.Abs(endY - startY);
                int pieces = segmentLength > 0.0 ? Math.Max(1, (int)Math.Round(length / segmentLength)) : 1;
                for (int piece = 0; piece < pieces; ++piece) {
                    double fraction = (double)piece / pieces;
                    double x = Math.Round((startX + (endX - startX) * fraction) * 4.0) / 4.0;
                    double y = Math.Round((startY + (endY - startY) * fraction) * 4.0) / 4.0;
                    vertices.Add(new CoordinateXY((float)x, (float)y));
                }
            }

            RemoveRepeatedNeighbors(vertices);
            return vertices;
        }

        List<List<CoordinateXY>> outlines = [
            Ring(width * 0.5 + halfWidth, height * 0.5 + halfWidth),
            Ring(width * 0.5 - halfWidth, height * 0.5 - halfWidth),
        ];
        string description = $"rectangular width={width} height={height} halfWidth={halfWidth} "
            + $"segmentLength={segmentLength}";
        return new SyntheticTrack(name, description, outlines, [], [], halfWidth);
    }

    /// <summary>A lattice of small regular polygons with seeded position, size, and turn.</summary>
    public static SyntheticTrack Scattered(
        string name, int seed, int columns, int rows, double spacing,
        double minimumRadius, double maximumRadius, int sides,
        double offsetX = 0.0, double offsetY = 0.0) {
        Random random = new(seed);
        List<List<CoordinateXY>> outlines = [];
        for (int row = 0; row < rows; ++row) {
            for (int column = 0; column < columns; ++column) {
                double radius = minimumRadius + random.NextDouble() * (maximumRadius - minimumRadius);
                double slack = Math.Max(0.0, spacing * 0.5 - radius);
                double centerX = offsetX + (column - (columns - 1) * 0.5) * spacing
                    + (random.NextDouble() * 2.0 - 1.0) * slack * 0.5;
                double centerY = offsetY + (row - (rows - 1) * 0.5) * spacing
                    + (random.NextDouble() * 2.0 - 1.0) * slack * 0.5;
                double turn = random.NextDouble() * Math.Tau;
                List<CoordinateXY> vertices = new(sides);
                for (int side = 0; side < sides; ++side) {
                    double angle = turn + Math.Tau * side / sides;
                    vertices.Add(new CoordinateXY(
                        (float)(centerX + radius * Math.Cos(angle)),
                        (float)(centerY + radius * Math.Sin(angle))));
                }

                RemoveRepeatedNeighbors(vertices);
                outlines.Add(vertices);
            }
        }

        string description = $"scattered seed={seed} grid={columns}x{rows} spacing={spacing} "
            + $"radius={minimumRadius}..{maximumRadius} sides={sides} offset=({offsetX}, {offsetY})";
        return new SyntheticTrack(name, description, outlines, [], [], 0.0);
    }

    /// <summary>Outlines given directly, for small hand-built fixtures.</summary>
    public static SyntheticTrack FromOutlines(string name, params (float X, float Y)[][] loops) {
        List<List<CoordinateXY>> outlines = [];
        foreach ((float X, float Y)[] loop in loops) {
            List<CoordinateXY> vertices = new(loop.Length);
            foreach ((float x, float y) in loop) {
                vertices.Add(new CoordinateXY(x, y));
            }

            outlines.Add(vertices);
        }

        return new SyntheticTrack(name, "explicit outlines", outlines, [], [], 0.0);
    }

    /// <summary>
    /// Poses that follow the centerline at equal distances. The lateral fraction moves
    /// the car toward the outer barrier (positive) or the inner barrier (negative) as a
    /// fraction of the half width. Yaw follows the direction of travel.
    /// </summary>
    public RectanglePose[] LapPoses(int count, double stepDistance, double lateralFraction, double startDistance = 0.0) {
        if (!this.HasCenterline) {
            throw new InvalidOperationException("This track has no centerline.");
        }

        RectanglePose[] poses = new RectanglePose[count];
        int segment = 0;
        int samples = this._centerlineX.Length;
        for (int index = 0; index < count; ++index) {
            double distance = (startDistance + index * stepDistance) % this.LapLength;
            if (distance < this._centerlineLength[segment]) {
                segment = 0;
            }

            while (this._centerlineLength[segment + 1] <= distance) {
                ++segment;
            }

            int next = (segment + 1) % samples;
            double span = this._centerlineLength[segment + 1] - this._centerlineLength[segment];
            double fraction = span > 0.0 ? (distance - this._centerlineLength[segment]) / span : 0.0;
            double x = this._centerlineX[segment] + (this._centerlineX[next] - this._centerlineX[segment]) * fraction;
            double y = this._centerlineY[segment] + (this._centerlineY[next] - this._centerlineY[segment]) * fraction;
            double tangentX = this._centerlineX[next] - this._centerlineX[segment];
            double tangentY = this._centerlineY[next] - this._centerlineY[segment];
            double length = Math.Sqrt(tangentX * tangentX + tangentY * tangentY);
            tangentX /= length;
            tangentY /= length;
            // The centerline runs counterclockwise, so the right-hand normal points outward.
            double lateral = lateralFraction * this.HalfWidth;
            x += tangentY * lateral;
            y -= tangentX * lateral;
            poses[index] = new RectanglePose((float)x, (float)y, (float)YawForDirection(tangentX, tangentY));
        }

        return poses;
    }

    /// <summary>
    /// Clockwise yaw that points the vehicle front along a direction. The front is the
    /// minimum local Y side, so local (0, -1) must map onto the direction.
    /// </summary>
    public static double YawForDirection(double directionX, double directionY) {
        return Math.Atan2(-directionX, -directionY);
    }

    private static void RemoveRepeatedNeighbors(List<CoordinateXY> vertices) {
        // Rounding to binary32 can merge neighbors; the detector rejects zero-length edges.
        for (int index = vertices.Count - 1; index >= 0 && vertices.Count > 3; --index) {
            CoordinateXY current = vertices[index];
            CoordinateXY next = vertices[(index + 1) % vertices.Count];
            if (current.X == next.X && current.Y == next.Y) {
                vertices.RemoveAt(index);
            }
        }
    }
}
