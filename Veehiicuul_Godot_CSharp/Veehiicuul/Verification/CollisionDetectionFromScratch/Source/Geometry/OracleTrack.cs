using System;
using System.Collections.Generic;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

/// <summary>
/// The harness's own copy of a track's edges, built straight from the outline
/// vertices. Edge numbering follows outline order, then vertex order; each
/// outline closes from its last vertex to its first.
/// </summary>
internal sealed class OracleTrack {
    public OracleTrack(ColliderJson collider) {
        int count = 0;
        foreach (Outline outline in collider.Outlines) {
            count += outline.Vertices.Count;
        }

        this.EdgeCount = count;
        this.Ax = new float[count];
        this.Ay = new float[count];
        this.Bx = new float[count];
        this.By = new float[count];
        this.MinX = new float[count];
        this.MinY = new float[count];
        this.MaxX = new float[count];
        this.MaxY = new float[count];
        int edge = 0;
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        foreach (Outline outline in collider.Outlines) {
            List<CoordinateXY> vertices = outline.Vertices;
            for (int index = 0; index < vertices.Count; ++index) {
                CoordinateXY a = vertices[index];
                CoordinateXY b = vertices[(index + 1) % vertices.Count];
                this.Ax[edge] = a.X;
                this.Ay[edge] = a.Y;
                this.Bx[edge] = b.X;
                this.By[edge] = b.Y;
                this.MinX[edge] = MathF.Min(a.X, b.X);
                this.MinY[edge] = MathF.Min(a.Y, b.Y);
                this.MaxX[edge] = MathF.Max(a.X, b.X);
                this.MaxY[edge] = MathF.Max(a.Y, b.Y);
                minX = MathF.Min(minX, this.MinX[edge]);
                minY = MathF.Min(minY, this.MinY[edge]);
                maxX = MathF.Max(maxX, this.MaxX[edge]);
                maxY = MathF.Max(maxY, this.MaxY[edge]);
                ++edge;
            }
        }

        this.BoundsMinX = minX;
        this.BoundsMinY = minY;
        this.BoundsMaxX = maxX;
        this.BoundsMaxY = maxY;
    }

    public int EdgeCount { get; }
    public float[] Ax { get; }
    public float[] Ay { get; }
    public float[] Bx { get; }
    public float[] By { get; }
    public float[] MinX { get; }
    public float[] MinY { get; }
    public float[] MaxX { get; }
    public float[] MaxY { get; }
    public float BoundsMinX { get; }
    public float BoundsMinY { get; }
    public float BoundsMaxX { get; }
    public float BoundsMaxY { get; }

    /// <summary>Exact perimeter test for a rectangle given as four binary32 corners.</summary>
    public bool Intersects(ReadOnlySpan<float> corners) {
        return this.Intersects(corners, null, useBoundsFilter: true);
    }

    /// <summary>
    /// Exact perimeter test that optionally records every intersecting edge. Without the
    /// bounds filter every edge receives the full exact test.
    /// </summary>
    public bool Intersects(ReadOnlySpan<float> corners, List<int>? intersectingEdges, bool useBoundsFilter) {
        float rectangleMinX = MathF.Min(MathF.Min(corners[0], corners[2]), MathF.Min(corners[4], corners[6]));
        float rectangleMaxX = MathF.Max(MathF.Max(corners[0], corners[2]), MathF.Max(corners[4], corners[6]));
        float rectangleMinY = MathF.Min(MathF.Min(corners[1], corners[3]), MathF.Min(corners[5], corners[7]));
        float rectangleMaxY = MathF.Max(MathF.Max(corners[1], corners[3]), MathF.Max(corners[5], corners[7]));
        bool any = false;
        for (int edge = 0; edge < this.EdgeCount; ++edge) {
            if (useBoundsFilter
                && (this.MaxX[edge] < rectangleMinX || this.MinX[edge] > rectangleMaxX
                    || this.MaxY[edge] < rectangleMinY || this.MinY[edge] > rectangleMaxY)) {
                continue;
            }

            if (this.EdgeIntersects(edge, corners)) {
                if (intersectingEdges is null) {
                    return true;
                }

                intersectingEdges.Add(edge);
                any = true;
            }
        }

        return any;
    }

    public bool EdgeIntersects(int edge, ReadOnlySpan<float> corners) {
        for (int side = 0; side < 4; ++side) {
            int next = (side + 1) & 3;
            if (ExactOracle.SegmentsIntersect(
                    this.Ax[edge], this.Ay[edge], this.Bx[edge], this.By[edge],
                    corners[2 * side], corners[2 * side + 1], corners[2 * next], corners[2 * next + 1])) {
                return true;
            }
        }

        return false;
    }
}
