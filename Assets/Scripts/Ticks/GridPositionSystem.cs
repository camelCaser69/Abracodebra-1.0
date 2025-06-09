using System;
using System.Linq;
using UnityEngine;

namespace WegoSystem
{
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

        public static readonly GridPosition Up = new GridPosition(0, 1);
        public static readonly GridPosition Down = new GridPosition(0, -1);
        public static readonly GridPosition Left = new GridPosition(-1, 0);
        public static readonly GridPosition Right = new GridPosition(1, 0);
        public static readonly GridPosition UpLeft = new GridPosition(-1, 1);
        public static readonly GridPosition UpRight = new GridPosition(1, 1);
        public static readonly GridPosition DownLeft = new GridPosition(-1, -1);
        public static readonly GridPosition DownRight = new GridPosition(1, -1);
        public static readonly GridPosition Zero = new GridPosition(0, 0);

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
    }

    public class GridEntity : MonoBehaviour
    {
        [Header("Grid Logic")]
        [SerializeField] private GridPosition gridPosition;
        [SerializeField] private bool snapToGridOnStart = true;

        [Header("Visuals")]
        [SerializeField] private Vector3 visualOffset = Vector3.zero;
        [SerializeField] private float visualInterpolationSpeed = 5f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private GridPosition previousGridPosition;
        private Vector3 visualStartPosition;
        private Vector3 visualTargetPosition;
        private float movementProgress = 1f;
        private bool isMoving = false;

        public GridPosition Position
        {
            get => gridPosition;
            private set // Keep setter private to be controlled by SetPosition method
            {
                if (gridPosition != value)
                {
                    previousGridPosition = gridPosition;
                    gridPosition = value;
                    OnGridPositionChanged();
                }
            }
        }

        public GridPosition PreviousPosition => previousGridPosition;
        public bool IsMoving => isMoving;
        public float MovementProgress => movementProgress;

        public event Action<GridPosition, GridPosition> OnPositionChanged;
        public event Action<GridPosition> OnMovementComplete;
        public event Action OnMovementStart;

        protected virtual void Start()
        {
            if (snapToGridOnStart)
            {
                SnapToGrid();
            }

            visualStartPosition = transform.position;
            visualTargetPosition = transform.position;
            previousGridPosition = gridPosition;

            GridPositionManager.Instance?.RegisterEntity(this);
        }

        protected virtual void OnDestroy()
        {
            GridPositionManager.Instance?.UnregisterEntity(this);
        }

        protected virtual void Update()
        {
            if (movementProgress < 1f)
            {
                movementProgress += Time.deltaTime * visualInterpolationSpeed;
                movementProgress = Mathf.Clamp01(movementProgress);

                float curvedProgress = movementCurve.Evaluate(movementProgress);
                transform.position = Vector3.Lerp(visualStartPosition, visualTargetPosition, curvedProgress);

                if (movementProgress >= 1f)
                {
                    transform.position = visualTargetPosition;
                    isMoving = false;
                    OnMovementComplete?.Invoke(gridPosition);
                }
            }
        }

        protected virtual void OnGridPositionChanged()
        {
            visualStartPosition = transform.position;
            // The target position is the grid cell center PLUS the visual offset.
            visualTargetPosition = GridPositionManager.Instance.GridToWorld(gridPosition) + visualOffset;
            movementProgress = 0f;

            if (!isMoving)
            {
                isMoving = true;
                OnMovementStart?.Invoke();
            }

            OnPositionChanged?.Invoke(previousGridPosition, gridPosition);
        }

        public void SetPosition(GridPosition newPosition, bool instant = false)
        {
            Position = newPosition;

            if (instant)
            {
                // Instantly apply the position including the visual offset
                transform.position = GridPositionManager.Instance.GridToWorld(Position) + visualOffset;
                visualStartPosition = transform.position;
                visualTargetPosition = transform.position;
                movementProgress = 1f;
                isMoving = false;
            }
        }

        public void SnapToGrid()
        {
            if (GridPositionManager.Instance == null) return;
            GridPosition currentGridPos = GridPositionManager.Instance.WorldToGrid(transform.position);
            SetPosition(currentGridPos, true);
        }

        public void MoveInDirection(GridPosition direction)
        {
            SetPosition(gridPosition + direction);
        }

        public bool CanMoveTo(GridPosition targetPosition)
        {
            return GridPositionManager.Instance?.IsPositionValid(targetPosition) ?? false;
        }

        public void CompleteMovement()
        {
            if (isMoving)
            {
                transform.position = visualTargetPosition;
                movementProgress = 1f;
                isMoving = false;
                OnMovementComplete?.Invoke(gridPosition);
            }
        }
    }
}