using System;
using System.Diagnostics.CodeAnalysis;

namespace Veehiicuul_Godot_CSharp;

public sealed partial class TrackCollisionDetector {
    /// <summary>
    /// A cell contains every edge a vehicle centered anywhere in it can reach,
    /// at any yaw. Repeating edge references trades a little immutable storage
    /// for one cell lookup, including rejection before any trigonometry.
    /// </summary>
    private sealed class ExpandedGrid {
        private const int MaximumCellCount = 65536;
        private const int MaximumReferenceCount = 1048576;
        private const double CoordinateLimit = 1e12;

        private readonly double _originX;
        private readonly double _originY;
        private readonly double _inverseCellSize;
        private readonly float _maximumLocalX;
        private readonly float _maximumLocalY;
        private readonly CellRange[] _cells;

        private ExpandedGrid(double originX, double originY, double cellSize,
            float maximumLocalX, float maximumLocalY, int columns, int rows,
            CellRange[] cells, int[] edgeIds, int occupiedCellCount) {
            this._originX = originX;
            this._originY = originY;
            this._inverseCellSize = 1.0 / cellSize;
            this._maximumLocalX = maximumLocalX;
            this._maximumLocalY = maximumLocalY;
            this.ColumnCount = columns;
            this.RowCount = rows;
            this._cells = cells;
            this.EdgeIds = edgeIds;
            this.OccupiedCellCount = occupiedCellCount;
        }

        internal int ColumnCount { get; }
        internal int RowCount { get; }
        internal int[] EdgeIds { get; }
        internal int OccupiedCellCount { get; }

        internal bool Supports(RectangleLocalBounds bounds, RectanglePose pose) {
            // Comparisons also reject NaNs. Keep validation and overflow behavior
            // on the general path for invalid bounds or extreme coordinates.
            return bounds.MinX < bounds.MaxX && bounds.MinY < bounds.MaxY
                && bounds.MinX >= -this._maximumLocalX && bounds.MaxX <= this._maximumLocalX
                && bounds.MinY >= -this._maximumLocalY && bounds.MaxY <= this._maximumLocalY
                && Math.Abs(pose.PositionX) < CoordinateLimit && Math.Abs(pose.PositionY) < CoordinateLimit
                && float.IsFinite(pose.RotationRadians);
        }

        internal bool SupportsCentered(RectangleLocalBounds bounds, RectanglePose pose) {
            return bounds.MinX < bounds.MaxX && bounds.MinY < bounds.MaxY
                && (double)bounds.MaxX - bounds.MinX <= 2.0 * this._maximumLocalX
                && (double)bounds.MaxY - bounds.MinY <= 2.0 * this._maximumLocalY
                && Math.Abs(pose.PositionX) < CoordinateLimit && Math.Abs(pose.PositionY) < CoordinateLimit
                && float.IsFinite(pose.RotationRadians);
        }

        internal CellRange GetRange(double x, double y) {
            double column = (x - this._originX) * this._inverseCellSize;
            double row = (y - this._originY) * this._inverseCellSize;
            if (column < 0 || row < 0 || column >= this.ColumnCount || row >= this.RowCount) {
                return default;
            }

            return this._cells[(int)row * this.ColumnCount + (int)column];
        }

        internal static bool TryCreate(Edge[] edges, AabbF allBounds, RectangleLocalBounds vehicleBounds,
            double cellSize, [NotNullWhen(true)] out ExpandedGrid? grid) {
            grid = null;
            float maximumLocalX = Math.Max(Math.Abs(vehicleBounds.MinX), Math.Abs(vehicleBounds.MaxX));
            float maximumLocalY = Math.Max(Math.Abs(vehicleBounds.MinY), Math.Abs(vehicleBounds.MaxY));
            double radius = Math.Sqrt((double)maximumLocalX * maximumLocalX + (double)maximumLocalY * maximumLocalY);
            double coordinateMagnitude = Math.Max(Math.Max(Math.Abs(allBounds.MinX), Math.Abs(allBounds.MaxX)),
                Math.Max(Math.Abs(allBounds.MinY), Math.Abs(allBounds.MaxY))) + radius;
            if (coordinateMagnitude >= CoordinateLimit || radius >= CoordinateLimit) {
                return false;
            }

            // Cover rounding in sin/cos, the double additions and final float
            // corner conversion. This expands ONLY the broad phase; contacts
            // still use the double-precision segment predicate.
            float magnitude = (float)coordinateMagnitude;
            double roundingMargin = 2.0 * ((double)MathF.BitIncrement(magnitude) - magnitude);
            radius = Math.BitIncrement(radius + roundingMargin);
            double originX = Math.BitDecrement((double)allBounds.MinX - radius);
            double originY = Math.BitDecrement((double)allBounds.MinY - radius);
            double inverseCellSize = 1.0 / cellSize;
            double columns = Math.Floor((Math.BitIncrement((double)allBounds.MaxX + radius) - originX) * inverseCellSize) + 1;
            double rows = Math.Floor((Math.BitIncrement((double)allBounds.MaxY + radius) - originY) * inverseCellSize) + 1;
            if (!(columns > 0) || !(rows > 0) || columns * rows > MaximumCellCount) {
                return false;
            }

            int columnCount = (int)columns, rowCount = (int)rows;
            int[] counts = new int[columnCount * rowCount];
            (int MinX, int MinY, int MaxX, int MaxY)[] coverage = new (int, int, int, int)[edges.Length];
            long referenceCount = 0;
            for (int index = 0; index < edges.Length; ++index) {
                AabbF bounds = edges[index].Bounds;
                int minX = Map(Math.BitDecrement((double)bounds.MinX - radius), originX, inverseCellSize, columnCount);
                int minY = Map(Math.BitDecrement((double)bounds.MinY - radius), originY, inverseCellSize, rowCount);
                int maxX = Map(Math.BitIncrement((double)bounds.MaxX + radius), originX, inverseCellSize, columnCount);
                int maxY = Map(Math.BitIncrement((double)bounds.MaxY + radius), originY, inverseCellSize, rowCount);
                referenceCount += (long)(maxX - minX + 1) * (maxY - minY + 1);
                if (referenceCount > MaximumReferenceCount) {
                    return false;
                }

                coverage[index] = (minX, minY, maxX, maxY);
                for (int row = minY; row <= maxY; ++row) {
                    for (int column = minX; column <= maxX; ++column) {
                        ++counts[row * columnCount + column];
                    }
                }
            }

            CellRange[] cells = new CellRange[counts.Length];
            int offset = 0, occupied = 0;
            for (int index = 0; index < counts.Length; ++index) {
                int count = counts[index];
                cells[index] = new CellRange(offset, count);
                counts[index] = offset;
                offset += count;
                if (count != 0) { ++occupied; }
            }

            int[] edgeIds = new int[offset];
            for (int index = 0; index < edges.Length; ++index) {
                (int minX, int minY, int maxX, int maxY) = coverage[index];
                for (int row = minY; row <= maxY; ++row) {
                    for (int column = minX; column <= maxX; ++column) {
                        edgeIds[counts[row * columnCount + column]++] = index;
                    }
                }
            }

            grid = new ExpandedGrid(originX, originY, cellSize, maximumLocalX, maximumLocalY,
                columnCount, rowCount, cells, edgeIds, occupied);
            return true;
        }

        private static int Map(double value, double origin, double inverseCellSize, int count) {
            return Math.Clamp((int)Math.Floor((value - origin) * inverseCellSize), 0, count - 1);
        }
    }
}
