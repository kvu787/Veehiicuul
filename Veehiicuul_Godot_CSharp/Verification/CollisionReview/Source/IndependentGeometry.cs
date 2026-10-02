using System;
using System.Numerics;
using Veehiicuul_Godot_CSharp;

namespace CollisionReview;

internal readonly record struct Point(float X, float Y);

/// <summary>Independent exact predicates over the final binary32 geometry.</summary>
internal sealed class IndependentGeometry {
    private readonly Segment[] _segments;

    internal IndependentGeometry(ColliderJson data) {
        int count = 0;
        foreach (Outline outline in data.Outlines) { count += outline.Vertices.Length; }
        this._segments = new Segment[count];
        int output = 0;
        foreach (Outline outline in data.Outlines) {
            for (int index = 0; index < outline.Vertices.Length; ++index) {
                CoordinateXY first = outline.Vertices[index];
                CoordinateXY second = outline.Vertices[(index + 1) % outline.Vertices.Length];
                this._segments[output++] = new Segment(new Point(first.X, first.Y), new Point(second.X, second.Y));
            }
        }
    }

    internal bool IsColliding(RectangleLocalBounds bounds, RectanglePose pose) {
        Span<Point> corners = stackalloc Point[4];
        double cosine = Math.Cos(pose.RotationRadians), sine = Math.Sin(pose.RotationRadians);
        corners[0] = World(bounds.MinX, bounds.MinY);
        corners[1] = World(bounds.MaxX, bounds.MinY);
        corners[2] = World(bounds.MaxX, bounds.MaxY);
        corners[3] = World(bounds.MinX, bounds.MaxY);
        float minimumX = float.PositiveInfinity, minimumY = float.PositiveInfinity;
        float maximumX = float.NegativeInfinity, maximumY = float.NegativeInfinity;
        foreach (Point corner in corners) {
            minimumX = Math.Min(minimumX, corner.X); maximumX = Math.Max(maximumX, corner.X);
            minimumY = Math.Min(minimumY, corner.Y); maximumY = Math.Max(maximumY, corner.Y);
        }
        foreach (Segment segment in this._segments) {
            if (segment.MaximumX < minimumX || segment.MinimumX > maximumX
                || segment.MaximumY < minimumY || segment.MinimumY > maximumY) { continue; }
            for (int side = 0; side < corners.Length; ++side) {
                if (Intersects(segment.First, segment.Second, corners[side], corners[(side + 1) % corners.Length])) { return true; }
            }
        }
        return false;

        Point World(float x, float y) {
            // Match the documented transform and its final rounding, not the production predicates or index.
            return new Point((float)((double)pose.PositionX + (double)x * cosine + (double)y * sine),
                (float)((double)pose.PositionY - (double)x * sine + (double)y * cosine));
        }
    }

    internal static bool Intersects(Point a, Point b, Point c, Point d) {
        if (Math.Max(a.X, b.X) < Math.Min(c.X, d.X) || Math.Max(c.X, d.X) < Math.Min(a.X, b.X)
            || Math.Max(a.Y, b.Y) < Math.Min(c.Y, d.Y) || Math.Max(c.Y, d.Y) < Math.Min(a.Y, b.Y)) { return false; }
        int first = Orientation(a, b, c), second = Orientation(a, b, d);
        int third = Orientation(c, d, a), fourth = Orientation(c, d, b);
        return first * second < 0 && third * fourth < 0
            || first == 0 && Contains(a, b, c) || second == 0 && Contains(a, b, d)
            || third == 0 && Contains(c, d, a) || fourth == 0 && Contains(c, d, b);
    }

    internal static int Orientation(Point a, Point b, Point c) {
        BigInteger x1 = Integer(b.X) - Integer(a.X), y1 = Integer(b.Y) - Integer(a.Y);
        BigInteger x2 = Integer(c.X) - Integer(a.X), y2 = Integer(c.Y) - Integer(a.Y);
        return (x1 * y2 - y1 * x2).Sign;
    }

    private static BigInteger Integer(float value) {
        uint bits = BitConverter.SingleToUInt32Bits(value);
        int exponent = (int)((bits >> 23) & 255);
        uint significand = bits & 0x7fffff;
        if (exponent != 0) { significand |= 0x800000; }
        BigInteger integer = new BigInteger(significand) << Math.Max(0, exponent - 1);
        return (bits & 0x80000000) == 0 ? integer : -integer;
    }

    private static bool Contains(Point a, Point b, Point point) {
        return point.X >= Math.Min(a.X, b.X) && point.X <= Math.Max(a.X, b.X)
            && point.Y >= Math.Min(a.Y, b.Y) && point.Y <= Math.Max(a.Y, b.Y);
    }

    private readonly struct Segment {
        internal Segment(Point first, Point second) {
            this.First = first; this.Second = second;
            this.MinimumX = Math.Min(first.X, second.X); this.MaximumX = Math.Max(first.X, second.X);
            this.MinimumY = Math.Min(first.Y, second.Y); this.MaximumY = Math.Max(first.Y, second.Y);
        }
        internal Point First { get; }
        internal Point Second { get; }
        internal float MinimumX { get; }
        internal float MaximumX { get; }
        internal float MinimumY { get; }
        internal float MaximumY { get; }
    }
}
