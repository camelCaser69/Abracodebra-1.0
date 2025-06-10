// Assets/Scripts/Ticks/GridPosition.cs

using System;
using UnityEngine;

namespace WegoSystem
{
    /// <summary>
    /// Represents a 2D integer coordinate on the game grid.
    /// This is a lightweight struct for grid calculations.
    /// </summary>
    [System.Serializable]
    public struct GridPosition : IEquatable<GridPosition>
    {
        public int x;
        public int y;

        public GridPosition(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public GridPosition(Vector3Int vector3Int)
        {
            this.x = vector3Int.x;
            this.y = vector3Int.y;
        }

        public GridPosition(Vector2Int vector2Int)
        {
            this.x = vector2Int.x;
            this.y = vector2Int.y;
        }

        #region Operators and Conversions
        public static GridPosition operator +(GridPosition a, GridPosition b)
        {
            return new GridPosition(a.x + b.x, a.y + b.y);
        }

        public static GridPosition operator -(GridPosition a, GridPosition b)
        {
            return new GridPosition(a.x - b.x, a.y - b.y);
        }

        public static bool operator ==(GridPosition a, GridPosition b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(GridPosition a, GridPosition b)
        {
            return !(a == b);
        }

        public Vector3Int ToVector3Int() => new Vector3Int(x, y, 0);
        public Vector2Int ToVector2Int() => new Vector2Int(x, y);
        public Vector3 ToWorldPosition(float cellSize = 1f) => new Vector3(x * cellSize, y * cellSize, 0);
        #endregion

        #region Static Directions
        public static readonly GridPosition Up = new GridPosition(0, 1);
        public static readonly GridPosition Down = new GridPosition(0, -1);
        public static readonly GridPosition Left = new GridPosition(-1, 0);
        public static readonly GridPosition Right = new GridPosition(1, 0);
        public static readonly GridPosition UpLeft = new GridPosition(-1, 1);
        public static readonly GridPosition UpRight = new GridPosition(1, 1);
        public static readonly GridPosition DownLeft = new GridPosition(-1, -1);
        public static readonly GridPosition DownRight = new GridPosition(1, -1);
        public static readonly GridPosition Zero = new GridPosition(0, 0);
        #endregion

        #region Utility Methods
        public int ManhattanDistance(GridPosition other)
        {
            return Mathf.Abs(x - other.x) + Mathf.Abs(y - other.y);
        }

        public int ChebyshevDistance(GridPosition other)
        {
            return Mathf.Max(Mathf.Abs(x - other.x), Mathf.Abs(y - other.y));
        }

        public float EuclideanDistance(GridPosition other)
        {
            int dx = x - other.x;
            int dy = y - other.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public GridPosition[] GetNeighbors(bool includeDiagonals = false)
        {
            if (includeDiagonals)
            {
                return new GridPosition[] {
                    this + Up, this + Down, this + Left, this + Right,
                    this + UpLeft, this + UpRight, this + DownLeft, this + DownRight
                };
            }
            else
            {
                return new GridPosition[] {
                    this + Up, this + Down, this + Left, this + Right
                };
            }
        }
        #endregion

        #region Equality & Overrides
        public bool Equals(GridPosition other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }
        #endregion
    }
}