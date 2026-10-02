using System;
using System.Collections.Generic;
using Veehiicuul_Godot_CSharp;

namespace CollisionReview;

/// <summary>Checks topology and float spacing independently of the runtime index.</summary>
internal static class AssetAnalysis {
    internal static object Run(ColliderJson data) {
        List<object> outlines = [];
        int intersections = 0, vertices = 0;
        double minimumLength = double.PositiveInfinity, maximumLength = 0;
        float minimumX = float.PositiveInfinity, minimumY = float.PositiveInfinity;
        float maximumX = float.NegativeInfinity, maximumY = float.NegativeInfinity;
        for (int outlineIndex = 0; outlineIndex < data.Outlines.Length; ++outlineIndex) {
            CoordinateXY[] points = data.Outlines[outlineIndex].Vertices;
            double area = 0;
            for (int index = 0; index < points.Length; ++index) {
                CoordinateXY first = points[index], second = points[(index + 1) % points.Length];
                ++vertices;
                double x = (double)second.X - first.X, y = (double)second.Y - first.Y;
                double length = Math.Sqrt(x * x + y * y);
                minimumLength = Math.Min(minimumLength, length); maximumLength = Math.Max(maximumLength, length);
                minimumX = Math.Min(minimumX, first.X); maximumX = Math.Max(maximumX, first.X);
                minimumY = Math.Min(minimumY, first.Y); maximumY = Math.Max(maximumY, first.Y);
                area += (double)first.X * second.Y - (double)second.X * first.Y;
                for (int otherOutline = outlineIndex; otherOutline < data.Outlines.Length; ++otherOutline) {
                    CoordinateXY[] others = data.Outlines[otherOutline].Vertices;
                    for (int other = otherOutline == outlineIndex ? index + 1 : 0; other < others.Length; ++other) {
                        if (otherOutline == outlineIndex && (other == index + 1 || index == 0 && other == points.Length - 1)) { continue; }
                        CoordinateXY third = others[other], fourth = others[(other + 1) % others.Length];
                        if (IndependentGeometry.Intersects(new Point(first.X, first.Y), new Point(second.X, second.Y), new Point(third.X, third.Y), new Point(fourth.X, fourth.Y))) { ++intersections; }
                    }
                }
            }
            outlines.Add(new { Index = outlineIndex, Vertices = points.Length, SignedArea = area * 0.5 });
        }
        return new {
            Outlines = outlines, VertexCount = vertices, NonadjacentIntersections = intersections,
            MinimumEdgeLength = minimumLength, MaximumEdgeLength = maximumLength,
            MinimumX = minimumX, MaximumX = maximumX, MinimumY = minimumY, MaximumY = maximumY,
            LargestCoordinateUlp = MathF.BitIncrement(Math.Max(Math.Max(Math.Abs(minimumX), Math.Abs(maximumX)), Math.Max(Math.Abs(minimumY), Math.Abs(maximumY))))
                - Math.Max(Math.Max(Math.Abs(minimumX), Math.Abs(maximumX)), Math.Max(Math.Abs(minimumY), Math.Abs(maximumY)))
        };
    }
}
