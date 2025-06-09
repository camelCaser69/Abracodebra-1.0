// Assets\Scripts\Ticks\GridPositionSystem.cs

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WegoSystem {
    [Serializable]
    public struct GridPosition : IEquatable<GridPosition> {
        public int x;
        public int y;

        public GridPosition(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public GridPosition(Vector3Int vector3Int) {
            this.x = vector3Int.x;
            this.y = vector3Int.y;
        }

        public GridPosition(Vector2Int vector2Int) {
            this.x = vector2Int.x;
            this.y = vector2Int.y;
        }

        public static GridPosition operator +(GridPosition a, GridPosition b) {
            return new GridPosition(a.x + b.x, a.y + b.y);
        }

        public static GridPosition operator -(GridPosition a, GridPosition b) {
            return new GridPosition(a.x - b.x, a.y - b.y);
        }

        public static bool operator ==(GridPosition a, GridPosition b) {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(GridPosition a, GridPosition b) {
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

        public int ManhattanDistance(GridPosition other) {
            return Mathf.Abs(x - other.x) + Mathf.Abs(y - other.y);
        }

        public int ChebyshevDistance(GridPosition other) {
            return Mathf.Max(Mathf.Abs(x - other.x), Mathf.Abs(y - other.y));
        }

        public float EuclideanDistance(GridPosition other) {
            int dx = x - other.x;
            int dy = y - other.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public GridPosition[] GetNeighbors(bool includeDiagonals = false) {
            if (includeDiagonals) {
                return new GridPosition[] {
                    this + Up, this + Down, this + Left, this + Right,
                    this + UpLeft, this + UpRight, this + DownLeft, this + DownRight
                };
            }
            else {
                return new GridPosition[] {
                    this + Up, this + Down, this + Left, this + Right
                };
            }
        }

        public bool Equals(GridPosition other) {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj) {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(x, y);
        }

        public override string ToString() {
            return $"({x}, {y})";
        }
    }

    public class GridEntity : MonoBehaviour {
        [SerializeField] GridPosition gridPosition;
        [SerializeField] float visualInterpolationSpeed = 5f;
        [SerializeField] bool snapToGridOnStart = true;
        [SerializeField] AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        GridPosition previousGridPosition;
        Vector3 visualStartPosition;
        Vector3 visualTargetPosition;
        float movementProgress = 1f;
        bool isMoving = false;

        public GridPosition Position {
            get => gridPosition;
            set {
                if (gridPosition != value) {
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

        protected virtual void Start() {
            if (snapToGridOnStart && GridPositionManager.Instance != null) {
                gridPosition = GridPositionManager.Instance.WorldToGrid(transform.position);
                transform.position = GridPositionManager.Instance.GridToWorld(gridPosition);
            }

            visualStartPosition = transform.position;
            visualTargetPosition = transform.position;
            previousGridPosition = gridPosition;

            GridPositionManager.Instance?.RegisterEntity(this);
        }

        protected virtual void OnDestroy() {
            GridPositionManager.Instance?.UnregisterEntity(this);
        }

        protected virtual void Update() {
            if (movementProgress < 1f) {
                movementProgress += Time.deltaTime * visualInterpolationSpeed;
                movementProgress = Mathf.Clamp01(movementProgress);
                
                // Use animation curve for smoother movement
                float curvedProgress = movementCurve.Evaluate(movementProgress);
                transform.position = Vector3.Lerp(visualStartPosition, visualTargetPosition, curvedProgress);
                
                if (movementProgress >= 1f) {
                    transform.position = visualTargetPosition;
                    isMoving = false;
                    OnMovementComplete?.Invoke(gridPosition);
                }
            }
        }

        protected virtual void OnGridPositionChanged() {
            visualStartPosition = transform.position;
            visualTargetPosition = GridPositionManager.Instance.GridToWorld(gridPosition);
            movementProgress = 0f;
            
            if (!isMoving) {
                isMoving = true;
                OnMovementStart?.Invoke();
            }
            
            OnPositionChanged?.Invoke(previousGridPosition, gridPosition);
        }

        public void SetPosition(GridPosition newPosition, bool instant = false) {
            Position = newPosition;

            if (instant) {
                transform.position = visualTargetPosition;
                visualStartPosition = visualTargetPosition;
                movementProgress = 1f;
                isMoving = false;
            }
        }

        public void MoveInDirection(GridPosition direction) {
            SetPosition(gridPosition + direction);
        }

        public bool CanMoveTo(GridPosition targetPosition) {
            return GridPositionManager.Instance?.IsPositionValid(targetPosition) ?? false;
        }
        
        // Force complete current movement
        public void CompleteMovement() {
            if (isMoving) {
                transform.position = visualTargetPosition;
                movementProgress = 1f;
                isMoving = false;
                OnMovementComplete?.Invoke(gridPosition);
            }
        }
    }

    public class GridPositionManager : MonoBehaviour {
        public static GridPositionManager Instance { get; private set; }

        [SerializeField] float cellSize = 1f;
        [SerializeField] Vector3 gridOrigin = Vector3.zero;
        [SerializeField] Vector2Int gridBounds = new Vector2Int(100, 100);

        [SerializeField] bool showGridGizmos = true;
        [SerializeField] Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        [SerializeField] int gizmoGridSize = 20;
        [SerializeField] bool debugMode = false;

        readonly Dictionary<GridPosition, HashSet<GridEntity>> entitiesByPosition = new Dictionary<GridPosition, HashSet<GridEntity>>();
        readonly HashSet<GridEntity> allEntities = new HashSet<GridEntity>();

        void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void OnDestroy() {
            if (Instance == this) {
                Instance = null;
            }
        }

        public GridPosition WorldToGrid(Vector3 worldPosition) {
            Vector3 localPosition = worldPosition - gridOrigin;
            int x = Mathf.RoundToInt(localPosition.x / cellSize);
            int y = Mathf.RoundToInt(localPosition.y / cellSize);
            return new GridPosition(x, y);
        }

        public Vector3 GridToWorld(GridPosition gridPosition) {
            return new Vector3(
                gridPosition.x * cellSize + gridOrigin.x,
                gridPosition.y * cellSize + gridOrigin.y,
                gridOrigin.z
            );
        }

        public Vector3 GetCellCenter(GridPosition gridPosition) {
            return GridToWorld(gridPosition);
        }

        public bool IsPositionValid(GridPosition position) {
            return position.x >= -gridBounds.x / 2 && position.x < gridBounds.x / 2 &&
                   position.y >= -gridBounds.y / 2 && position.y < gridBounds.y / 2;
        }

        public bool IsPositionOccupied(GridPosition position) {
            return entitiesByPosition.ContainsKey(position) && entitiesByPosition[position].Count > 0;
        }

        public void RegisterEntity(GridEntity entity) {
            if (entity == null || allEntities.Contains(entity)) return;

            allEntities.Add(entity);
            AddEntityToPosition(entity, entity.Position);

            entity.OnPositionChanged += OnEntityPositionChanged;
        }

        public void UnregisterEntity(GridEntity entity) {
            if (entity == null || !allEntities.Contains(entity)) return;

            allEntities.Remove(entity);
            RemoveEntityFromPosition(entity, entity.Position);

            entity.OnPositionChanged -= OnEntityPositionChanged;
        }

        void OnEntityPositionChanged(GridPosition oldPosition, GridPosition newPosition) {
            var entity = allEntities.FirstOrDefault(e => e.Position == newPosition && e.PreviousPosition == oldPosition);
            if (entity != null) {
                RemoveEntityFromPosition(entity, oldPosition);
                AddEntityToPosition(entity, newPosition);
            }
        }

        void AddEntityToPosition(GridEntity entity, GridPosition position) {
            if (!entitiesByPosition.ContainsKey(position)) {
                entitiesByPosition[position] = new HashSet<GridEntity>();
            }
            entitiesByPosition[position].Add(entity);
        }

        void RemoveEntityFromPosition(GridEntity entity, GridPosition position) {
            if (entitiesByPosition.ContainsKey(position)) {
                entitiesByPosition[position].Remove(entity);
                if (entitiesByPosition[position].Count == 0) {
                    entitiesByPosition.Remove(position);
                }
            }
        }

        public HashSet<GridEntity> GetEntitiesAt(GridPosition position) {
            return entitiesByPosition.ContainsKey(position)
                ? new HashSet<GridEntity>(entitiesByPosition[position])
                : new HashSet<GridEntity>();
        }

        public List<GridEntity> GetEntitiesInRadius(GridPosition center, int radius, bool useManhattan = true) {
            var result = new List<GridEntity>();

            for (int x = -radius; x <= radius; x++) {
                for (int y = -radius; y <= radius; y++) {
                    var checkPos = new GridPosition(center.x + x, center.y + y);

                    if (useManhattan) {
                        if (checkPos.ManhattanDistance(center) > radius) continue;
                    }
                    else {
                        if (checkPos.ChebyshevDistance(center) > radius) continue;
                    }

                    if (entitiesByPosition.ContainsKey(checkPos)) {
                        result.AddRange(entitiesByPosition[checkPos]);
                    }
                }
            }

            return result;
        }

        public GridEntity GetNearestEntity(GridPosition position, System.Func<GridEntity, bool> predicate = null) {
            GridEntity nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var entity in allEntities) {
                if (predicate != null && !predicate(entity)) continue;

                float distance = entity.Position.EuclideanDistance(position);
                if (distance < nearestDistance) {
                    nearest = entity;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        // Improved A* pathfinding
        public List<GridPosition> GetPath(GridPosition start, GridPosition end, bool allowDiagonal = false) {
            var path = new List<GridPosition>();
            
            if (!IsPositionValid(start) || !IsPositionValid(end)) {
                return path;
            }
            
            // Simple A* implementation
            var openSet = new HashSet<GridPosition>();
            var closedSet = new HashSet<GridPosition>();
            var cameFrom = new Dictionary<GridPosition, GridPosition>();
            var gScore = new Dictionary<GridPosition, float>();
            var fScore = new Dictionary<GridPosition, float>();
            
            openSet.Add(start);
            gScore[start] = 0;
            fScore[start] = HeuristicCost(start, end);
            
            while (openSet.Count > 0) {
                GridPosition current = GetLowestFScore(openSet, fScore);
                
                if (current == end) {
                    // Reconstruct path
                    while (cameFrom.ContainsKey(current)) {
                        path.Add(current);
                        current = cameFrom[current];
                    }
                    path.Reverse();
                    return path;
                }
                
                openSet.Remove(current);
                closedSet.Add(current);
                
                foreach (var neighbor in current.GetNeighbors(allowDiagonal)) {
                    if (!IsPositionValid(neighbor) || IsPositionOccupied(neighbor) || closedSet.Contains(neighbor)) {
                        continue;
                    }
                    
                    float tentativeGScore = gScore[current] + 1;
                    
                    if (!openSet.Contains(neighbor)) {
                        openSet.Add(neighbor);
                    } else if (gScore.ContainsKey(neighbor) && tentativeGScore >= gScore[neighbor]) {
                        continue;
                    }
                    
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + HeuristicCost(neighbor, end);
                }
            }
            
            return path; // No path found
        }
        
        float HeuristicCost(GridPosition a, GridPosition b) {
            // Manhattan distance for grid-based movement
            return a.ManhattanDistance(b);
        }
        
        GridPosition GetLowestFScore(HashSet<GridPosition> openSet, Dictionary<GridPosition, float> fScore) {
            GridPosition lowest = openSet.First();
            float lowestScore = fScore.ContainsKey(lowest) ? fScore[lowest] : float.MaxValue;
            
            foreach (var pos in openSet) {
                float score = fScore.ContainsKey(pos) ? fScore[pos] : float.MaxValue;
                if (score < lowestScore) {
                    lowest = pos;
                    lowestScore = score;
                }
            }
            
            return lowest;
        }
        
        // Helper to check if a path exists
        public bool PathExists(GridPosition start, GridPosition end, bool allowDiagonal = false) {
            var path = GetPath(start, end, allowDiagonal);
            return path.Count > 0;
        }

        // Add this helper method for consistent grid snapping
        public void SnapEntityToGrid(GameObject entity) {
            if (entity == null) return;
            
            GridEntity gridEntity = entity.GetComponent<GridEntity>();
            if (gridEntity == null) {
                gridEntity = entity.AddComponent<GridEntity>();
            }
            
            // Calculate nearest grid position
            GridPosition nearestGrid = WorldToGrid(entity.transform.position);
            Vector3 snappedWorldPos = GridToWorld(nearestGrid);
            
            // Snap transform
            entity.transform.position = snappedWorldPos;
            
            // Update grid entity
            gridEntity.SetPosition(nearestGrid, instant: true);
            
            if (debugMode) {
                Debug.Log($"[GridPositionManager] Snapped {entity.name} from {entity.transform.position} to grid {nearestGrid} at world {snappedWorldPos}");
            }
        }
        
        // Batch snap all entities with a specific tag or component
        public void SnapAllEntitiesToGrid<T>() where T : Component {
            T[] entities = FindObjectsByType<T>(FindObjectsSortMode.None);
            foreach (var entity in entities) {
                SnapEntityToGrid(entity.gameObject);
            }
            Debug.Log($"[GridPositionManager] Snapped {entities.Length} entities of type {typeof(T).Name} to grid");
        }

        void OnDrawGizmos() {
            if (!showGridGizmos) return;

            Gizmos.color = gridColor;

            int halfWidth = gizmoGridSize / 2;
            int halfHeight = gizmoGridSize / 2;

            for (int x = -halfWidth; x <= halfWidth; x++) {
                Vector3 start = GridToWorld(new GridPosition(x, -halfHeight));
                Vector3 end = GridToWorld(new GridPosition(x, halfHeight));
                Gizmos.DrawLine(start, end);
            }

            for (int y = -halfHeight; y <= halfHeight; y++) {
                Vector3 start = GridToWorld(new GridPosition(-halfWidth, y));
                Vector3 end = GridToWorld(new GridPosition(halfWidth, y));
                Gizmos.DrawLine(start, end);
            }

            Gizmos.color = Color.red;
            foreach (var kvp in entitiesByPosition) {
                if (kvp.Value.Count > 0) {
                    Vector3 cellCenter = GridToWorld(kvp.Key);
                    Gizmos.DrawWireCube(cellCenter, Vector3.one * cellSize * 0.8f);
                }
            }
        }
    }
}