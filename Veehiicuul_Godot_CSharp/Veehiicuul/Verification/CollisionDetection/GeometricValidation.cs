using System.Reflection;
using Veehiicuul_Godot_CSharp;

namespace VeehiicuulCollisionVerification;

internal static partial class PerformanceAnalysis {
    private static void ValidateGridBoundaries(ColliderJson data, TrackCollisionDetector detector) {
        if (!detector.UsesExpandedGrid) { return; }
        object grid = typeof(TrackCollisionDetector).GetField("_expandedGrid", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(detector)!;
        double originX = (double)grid.GetType().GetField("_originX", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(grid)!;
        double originY = (double)grid.GetType().GetField("_originY", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(grid)!;
        int comparisons = 0;
        float[] angles = [0, MathF.PI / 4, MathF.PI / 2, MathF.PI, -0.7f, 1.3f, 1000000f];
        for (int row = 0; row <= detector.GridRowCount; ++row) {
            for (int column = 0; column <= detector.GridColumnCount; ++column) {
                float x = (float)(originX + column * detector.CellSize);
                float y = (float)(originY + row * detector.CellSize);
                foreach (float offsetX in new[] { float.BitDecrement(x), x, float.BitIncrement(x) }) {
                    foreach (float offsetY in new[] { float.BitDecrement(y), y, float.BitIncrement(y) }) {
                        foreach (float angle in angles) {
                            Query query = new(CarBounds, new(offsetX, offsetY, angle));
                            Require(detector.IsColliding(query.Bounds, query.Pose) == detector.IsCollidingLinear(query.Bounds, query.Pose), "Expanded grid boundary mismatch");
                            if (comparisons % 1024 == 0) { CompareOracle(data, detector, query); }
                            ++comparisons;
                        }
                    }
                }
            }
        }
        // An offset local origin must use the centered lookup without changing
        // the rounded world rectangle. Also exercise world-coordinate cancellation.
        foreach (float offset in new[] { -100000f, -1000f, 0f, 1000f, 100000f }) {
            foreach (CoordinateXY vertex in data.Outlines[0].Vertices.Take(32)) {
                Query query = new(new(offset, offset, offset + 3, offset + 6), new(vertex.X - offset, vertex.Y - offset, 0));
                CompareOracle(data, detector, query);
            }
        }
        Console.WriteLine($"PASS: {comparisons} expanded-grid boundary comparisons and 160 offset-origin oracle cases.");
    }

    private static void ValidateOrientationArithmetic() {
        // Invoke the actual private predicate so exponent-span tests reach the
        // Int128/BigInteger decision even when their enclosing AABBs miss.
        Type pointType = typeof(TrackCollisionDetector).GetNestedType("PointF", BindingFlags.NonPublic)!;
        ConstructorInfo constructor = pointType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(float), typeof(float)], null)!;
        Type predicateType = typeof(TrackCollisionDetector).GetNestedType("RobustPredicates", BindingFlags.NonPublic)!;
        MethodInfo orientation = predicateType.GetMethod("OrientationSign", BindingFlags.NonPublic | BindingFlags.Static)!;
        Random random = new(0x6a020926);
        int comparisons = 0;
        void Verify(Point a, Point b, Point c) {
            object[] arguments = [constructor.Invoke([a.X, a.Y]), constructor.Invoke([b.X, b.Y]), constructor.Invoke([c.X, c.Y])];
            int actual = (int)orientation.Invoke(null, arguments)!;
            Require(actual == Orientation(a, b, c), $"Exact orientation mismatch: {a}, {b}, {c}");
            ++comparisons;
        }
        float RandomFinite() {
            int bits = (int)random.NextInt64(0, 0x7f800000);
            if (random.Next(2) != 0) { bits |= int.MinValue; }
            return BitConverter.Int32BitsToSingle(bits);
        }
        for (int index = 0; index < 30000; ++index) {
            Point a = new(RandomFinite(), RandomFinite()), b = new(RandomFinite(), RandomFinite()), c = new(RandomFinite(), RandomFinite());
            Verify(a, b, c);
            if (index % 16 == 0) { Verify(a, b, a); }
        }
        // Both sides of the bounded-integer shift limit, with exact collinearity
        // and one representable value on either side of the line.
        foreach (int exponent in new[] { -100, -40, 0, 40, 80 }) {
            float low = MathF.ScaleB(1f, exponent);
            foreach (int span in new[] { 36, 37, 38, 39, 60 }) {
                float high = MathF.ScaleB(1f, exponent + span);
                if (!float.IsFinite(high)) { continue; }
                Point a = new(low, low), b = new(high, high);
                foreach (float y in new[] { high * 0.5f, float.BitIncrement(high * 0.5f), float.BitDecrement(high * 0.5f) }) {
                    Verify(a, b, new(high * 0.5f, y));
                    Verify(new(-a.X, -a.Y), new(-b.X, -b.Y), new(-high * 0.5f, -y));
                }
            }
        }
        Console.WriteLine($"PASS: {comparisons} independent exact orientation checks over arbitrary finite binary32 values.");
    }
}
