using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Veehiicuul_Godot_CSharp;

namespace CollisionDetectionFromScratch;

internal delegate void CornerTransform(RectangleLocalBounds bounds, RectanglePose pose, float[] corners);

internal delegate bool SegmentPredicate(
    float ax, float ay, float bx, float by, float cx, float cy, float dx, float dy);

internal delegate int OrientationPredicate(float ax, float ay, float bx, float by, float cx, float cy);

/// <summary>
/// Compiled accessors for private members of the detector. They call the
/// detector's own code, so tests can examine intermediate results (rounded
/// corners, candidate edges, orientation signs) instead of only the final answer.
/// </summary>
internal static class DetectorInternals {
    internal const BindingFlags AnyMember =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static readonly Type DetectorType = typeof(TrackCollisionDetector);
    private static readonly Type PointType = Nested("PointF");
    private static readonly Type QuadType = Nested("RectangleQuad");
    private static readonly Type TransformerType = Nested("RectangleTransformer");
    private static readonly Type PredicatesType = Nested("RobustPredicates");

    public static readonly CornerTransform Transform = BuildTransform();
    public static readonly OrientationPredicate OrientationSign = BuildOrientation("OrientationSign");
    public static readonly OrientationPredicate ExactOrientationSign = BuildOrientation("ExactOrientationSign");
    public static readonly SegmentPredicate SegmentsIntersect = BuildSegments();

    internal static Type Nested(string name) {
        return DetectorType.GetNestedType(name, AnyMember)
            ?? throw new MissingMemberException(DetectorType.FullName, name);
    }

    internal static FieldInfo Field(Type type, string name) {
        return type.GetField(name, AnyMember) ?? throw new MissingFieldException(type.FullName, name);
    }

    internal static PropertyInfo Property(Type type, string name) {
        return type.GetProperty(name, AnyMember) ?? throw new MissingMemberException(type.FullName, name);
    }

    internal static MethodInfo Method(Type type, string name) {
        return type.GetMethod(name, AnyMember) ?? throw new MissingMethodException(type.FullName, name);
    }

    /// <summary>Reads the detector's private edge array as endpoint quadruples.</summary>
    public static float[,] ReadEdges(TrackCollisionDetector detector) {
        Array edges = (Array)Field(DetectorType, "_edges").GetValue(detector)!;
        Type edgeType = Nested("Edge");
        PropertyInfo a = Property(edgeType, "A"), b = Property(edgeType, "B");
        PropertyInfo x = Property(PointType, "X"), y = Property(PointType, "Y");
        float[,] result = new float[edges.Length, 4];
        for (int index = 0; index < edges.Length; ++index) {
            object edge = edges.GetValue(index)!;
            object start = a.GetValue(edge)!, end = b.GetValue(edge)!;
            result[index, 0] = (float)x.GetValue(start)!;
            result[index, 1] = (float)y.GetValue(start)!;
            result[index, 2] = (float)x.GetValue(end)!;
            result[index, 3] = (float)y.GetValue(end)!;
        }

        return result;
    }

    /// <summary>Length of the node array that the long-edge tree allocated.</summary>
    public static int ReadAllocatedTreeNodes(TrackCollisionDetector detector) {
        object outliers = Field(DetectorType, "_outliers").GetValue(detector)!;
        Array nodes = (Array)Field(outliers.GetType(), "_nodes").GetValue(outliers)!;
        return nodes.Length;
    }

    private static NewExpression NewPoint(Expression x, Expression y) {
        ConstructorInfo constructor = PointType.GetConstructor(AnyMember, null, [typeof(float), typeof(float)], null)
            ?? throw new MissingMethodException(PointType.FullName, ".ctor");
        return Expression.New(constructor, x, y);
    }

    private static CornerTransform BuildTransform() {
        ParameterExpression bounds = Expression.Parameter(typeof(RectangleLocalBounds), "bounds");
        ParameterExpression pose = Expression.Parameter(typeof(RectanglePose), "pose");
        ParameterExpression corners = Expression.Parameter(typeof(float[]), "corners");
        ParameterExpression quad = Expression.Variable(QuadType, "quad");
        List<Expression> body = [
            Expression.Assign(quad, Expression.Call(Method(TransformerType, "Transform"), bounds, pose)),
        ];
        string[] names = ["P0", "P1", "P2", "P3"];
        for (int index = 0; index < names.Length; ++index) {
            Expression point = Expression.Property(quad, Property(QuadType, names[index]));
            body.Add(Expression.Assign(
                Expression.ArrayAccess(corners, Expression.Constant(2 * index)),
                Expression.Property(point, Property(PointType, "X"))));
            body.Add(Expression.Assign(
                Expression.ArrayAccess(corners, Expression.Constant(2 * index + 1)),
                Expression.Property(point, Property(PointType, "Y"))));
        }

        body.Add(Expression.Empty());
        return Expression.Lambda<CornerTransform>(Expression.Block([quad], body), bounds, pose, corners).Compile();
    }

    private static OrientationPredicate BuildOrientation(string methodName) {
        ParameterExpression[] inputs = FloatParameters(6);
        ParameterExpression[] points = PointVariables(3);
        List<Expression> body = AssignPoints(points, inputs);
        body.Add(Expression.Call(Method(PredicatesType, methodName), points[0], points[1], points[2]));
        return Expression.Lambda<OrientationPredicate>(Expression.Block(points, body), inputs).Compile();
    }

    private static SegmentPredicate BuildSegments() {
        ParameterExpression[] inputs = FloatParameters(8);
        ParameterExpression[] points = PointVariables(4);
        List<Expression> body = AssignPoints(points, inputs);
        body.Add(Expression.Call(
            Method(PredicatesType, "SegmentsIntersect"), points[0], points[1], points[2], points[3]));
        return Expression.Lambda<SegmentPredicate>(Expression.Block(points, body), inputs).Compile();
    }

    private static ParameterExpression[] FloatParameters(int count) {
        ParameterExpression[] parameters = new ParameterExpression[count];
        for (int index = 0; index < count; ++index) {
            parameters[index] = Expression.Parameter(typeof(float), "value" + index);
        }

        return parameters;
    }

    private static ParameterExpression[] PointVariables(int count) {
        ParameterExpression[] variables = new ParameterExpression[count];
        for (int index = 0; index < count; ++index) {
            variables[index] = Expression.Variable(PointType, "point" + index);
        }

        return variables;
    }

    private static List<Expression> AssignPoints(ParameterExpression[] points, ParameterExpression[] inputs) {
        List<Expression> body = [];
        for (int index = 0; index < points.Length; ++index) {
            body.Add(Expression.Assign(points[index], NewPoint(inputs[2 * index], inputs[2 * index + 1])));
        }

        return body;
    }
}

/// <summary>Read-only view of one detector's expanded grid, or of its absence.</summary>
internal sealed class ExpandedGridView {
    private static readonly Type DetectorType = typeof(TrackCollisionDetector);
    private static readonly Type GridType = DetectorInternals.Nested("ExpandedGrid");
    private static readonly Type RangeType = DetectorInternals.Nested("CellRange");
    private static readonly Func<object, double, double, long> GetRangeAccessor = BuildGetRange();
    private static readonly Func<object, RectangleLocalBounds, RectanglePose, bool> SupportsAccessor =
        BuildSupports("Supports");
    private static readonly Func<object, RectangleLocalBounds, RectanglePose, bool> SupportsCenteredAccessor =
        BuildSupports("SupportsCentered");

    private readonly object _grid;

    private ExpandedGridView(object grid) {
        this._grid = grid;
        this.OriginX = (double)DetectorInternals.Field(GridType, "_originX").GetValue(grid)!;
        this.OriginY = (double)DetectorInternals.Field(GridType, "_originY").GetValue(grid)!;
        this.InverseCellSize = (double)DetectorInternals.Field(GridType, "_inverseCellSize").GetValue(grid)!;
        this.MaximumLocalX = (float)DetectorInternals.Field(GridType, "_maximumLocalX").GetValue(grid)!;
        this.MaximumLocalY = (float)DetectorInternals.Field(GridType, "_maximumLocalY").GetValue(grid)!;
        this.ColumnCount = (int)DetectorInternals.Property(GridType, "ColumnCount").GetValue(grid)!;
        this.RowCount = (int)DetectorInternals.Property(GridType, "RowCount").GetValue(grid)!;
        this.EdgeIds = (int[])DetectorInternals.Property(GridType, "EdgeIds").GetValue(grid)!;
        Array cells = (Array)DetectorInternals.Field(GridType, "_cells").GetValue(grid)!;
        PropertyInfo offset = DetectorInternals.Property(RangeType, "Offset");
        PropertyInfo count = DetectorInternals.Property(RangeType, "Count");
        this.CellOffsets = new int[cells.Length];
        this.CellCounts = new int[cells.Length];
        for (int index = 0; index < cells.Length; ++index) {
            object cell = cells.GetValue(index)!;
            this.CellOffsets[index] = (int)offset.GetValue(cell)!;
            this.CellCounts[index] = (int)count.GetValue(cell)!;
        }
    }

    public double OriginX { get; }
    public double OriginY { get; }
    public double InverseCellSize { get; }
    public float MaximumLocalX { get; }
    public float MaximumLocalY { get; }
    public int ColumnCount { get; }
    public int RowCount { get; }
    public int[] EdgeIds { get; }
    public int[] CellOffsets { get; }
    public int[] CellCounts { get; }

    public static ExpandedGridView? TryCreate(TrackCollisionDetector detector) {
        object? grid = DetectorInternals.Field(DetectorType, "_expandedGrid").GetValue(detector);
        return grid is null ? null : new ExpandedGridView(grid);
    }

    public bool Supports(RectangleLocalBounds bounds, RectanglePose pose) {
        return SupportsAccessor(this._grid, bounds, pose);
    }

    public bool SupportsCentered(RectangleLocalBounds bounds, RectanglePose pose) {
        return SupportsCenteredAccessor(this._grid, bounds, pose);
    }

    /// <summary>The detector's own cell lookup: first index into EdgeIds and number of candidates.</summary>
    public (int Offset, int Count) GetRange(double x, double y) {
        long packed = GetRangeAccessor(this._grid, x, y);
        return ((int)(packed >> 32), (int)(packed & 0xffffffff));
    }

    private static Func<object, double, double, long> BuildGetRange() {
        ParameterExpression grid = Expression.Parameter(typeof(object), "grid");
        ParameterExpression x = Expression.Parameter(typeof(double), "x");
        ParameterExpression y = Expression.Parameter(typeof(double), "y");
        ParameterExpression range = Expression.Variable(RangeType, "range");
        Expression call = Expression.Call(
            Expression.Convert(grid, GridType), DetectorInternals.Method(GridType, "GetRange"), x, y);
        Expression offset = Expression.Convert(
            Expression.Property(range, DetectorInternals.Property(RangeType, "Offset")), typeof(long));
        Expression count = Expression.Convert(
            Expression.Property(range, DetectorInternals.Property(RangeType, "Count")), typeof(long));
        Expression packed = Expression.Or(Expression.LeftShift(offset, Expression.Constant(32)), count);
        return Expression.Lambda<Func<object, double, double, long>>(
            Expression.Block([range], Expression.Assign(range, call), packed), grid, x, y).Compile();
    }

    private static Func<object, RectangleLocalBounds, RectanglePose, bool> BuildSupports(string name) {
        ParameterExpression grid = Expression.Parameter(typeof(object), "grid");
        ParameterExpression bounds = Expression.Parameter(typeof(RectangleLocalBounds), "bounds");
        ParameterExpression pose = Expression.Parameter(typeof(RectanglePose), "pose");
        Expression call = Expression.Call(
            Expression.Convert(grid, GridType), DetectorInternals.Method(GridType, name), bounds, pose);
        return Expression.Lambda<Func<object, RectangleLocalBounds, RectanglePose, bool>>(
            call, grid, bounds, pose).Compile();
    }
}
