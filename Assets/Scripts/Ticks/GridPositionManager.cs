using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WegoSystem
{
    public class GridPositionManager : MonoBehaviour
    {
        public static GridPositionManager Instance { get; set; }

        [Header("Dependencies")]
        [Tooltip("Optional: Manually assign the TileInteractionManager to avoid script execution order issues.")]
        [SerializeField] private TileInteractionManager tileInteractionManager;

        private Grid _tilemapGrid;

        private Grid TilemapGrid
        {
            get
            {
                if (_tilemapGrid == null)
                {
                    SyncWithTileGrid(); // Attempt to sync again if it's null
                }
                return _tilemapGrid;
            }
        }

        [Header("Grid Settings")]
        [SerializeField] private Vector2Int gridBounds = new Vector2Int(100, 100);

        [Header("Gizmos & Debug")]
        [SerializeField] private bool showGridGizmos = true;
        [SerializeField] private Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        [SerializeField] private int gizmoGridSize = 20;
        [SerializeField] private bool debugMode = false;

        private readonly Dictionary<GridPosition, HashSet<GridEntity>> entitiesByPosition = new Dictionary<GridPosition, HashSet<GridEntity>>();
        private readonly HashSet<GridEntity> allEntities = new HashSet<GridEntity>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SyncWithTileGrid();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SyncWithTileGrid()
        {
            // Prioritize the explicitly assigned manager
            if (tileInteractionManager != null && tileInteractionManager.interactionGrid != null)
            {
                this._tilemapGrid = tileInteractionManager.interactionGrid;
                if (debugMode)
                {
                    Debug.Log($"[GridPositionManager] Synced with assigned TileInteractionManager's grid: '{this._tilemapGrid.name}'.");
                }
                return; // Success
            }

            // Fallback to singleton instance if manual assignment is missing
            if (TileInteractionManager.Instance != null && TileInteractionManager.Instance.interactionGrid != null)
            {
                this._tilemapGrid = TileInteractionManager.Instance.interactionGrid;
                // Assign the found instance to our field for future reference and inspector visibility
                tileInteractionManager = TileInteractionManager.Instance;
                if (debugMode)
                {
                    Debug.Log($"[GridPositionManager] Synced with singleton TileInteractionManager.Instance's grid: '{this._tilemapGrid.name}'.");
                }
                return; // Success
            }

            // If we reach here, both methods failed.
            if (_tilemapGrid == null)
            {
                Debug.LogError("[GridPositionManager] Could not find TileInteractionManager or its grid to sync with! Grid system may be misaligned. Please assign the TileInteractionManager in the Inspector.");
            }
        }

        public GridPosition WorldToGrid(Vector3 worldPosition)
        {
            if (TilemapGrid == null) return GridPosition.Zero;
            Vector3Int cellPos = TilemapGrid.WorldToCell(worldPosition);
            return new GridPosition(cellPos);
        }

        public Vector3 GridToWorld(GridPosition gridPosition)
        {
            if (TilemapGrid == null) return Vector3.zero;
            return TilemapGrid.GetCellCenterWorld(gridPosition.ToVector3Int());
        }

        public Vector3 GetCellCenter(GridPosition gridPosition)
        {
            return GridToWorld(gridPosition);
        }

        public bool IsPositionValid(GridPosition position)
        {
            return position.x >= -gridBounds.x / 2 && position.x < gridBounds.x / 2 &&
                   position.y >= -gridBounds.y / 2 && position.y < gridBounds.y / 2;
        }

        public bool IsPositionOccupied(GridPosition position)
        {
            // A position is occupied only if it contains at least one entity
            // that is flagged as a tile occupant.
            if (entitiesByPosition.TryGetValue(position, out var entities))
            {
                return entities.Any(entity => entity.isTileOccupant);
            }
            return false;
        }

        public void RegisterEntity(GridEntity entity)
        {
            if (entity == null || allEntities.Contains(entity)) return;

            allEntities.Add(entity);
            AddEntityToPosition(entity, entity.Position);

            entity.OnPositionChanged += OnEntityPositionChanged;
        }

        public void UnregisterEntity(GridEntity entity)
        {
            if (entity == null || !allEntities.Contains(entity)) return;

            allEntities.Remove(entity);
            RemoveEntityFromPosition(entity, entity.Position);

            entity.OnPositionChanged -= OnEntityPositionChanged;
        }

        private void OnEntityPositionChanged(GridPosition oldPosition, GridPosition newPosition)
        {
            var entity = allEntities.FirstOrDefault(e => e.Position == newPosition && e.PreviousPosition == oldPosition);
            if (entity != null)
            {
                RemoveEntityFromPosition(entity, oldPosition);
                AddEntityToPosition(entity, newPosition);
            }
        }

        private void AddEntityToPosition(GridEntity entity, GridPosition position)
        {
            if (!entitiesByPosition.ContainsKey(position))
            {
                entitiesByPosition[position] = new HashSet<GridEntity>();
            }
            entitiesByPosition[position].Add(entity);
        }

        private void RemoveEntityFromPosition(GridEntity entity, GridPosition position)
        {
            if (entitiesByPosition.ContainsKey(position))
            {
                entitiesByPosition[position].Remove(entity);
                if (entitiesByPosition[position].Count == 0)
                {
                    entitiesByPosition.Remove(position);
                }
            }
        }

        public HashSet<GridEntity> GetEntitiesAt(GridPosition position)
        {
            return entitiesByPosition.ContainsKey(position)
                ? new HashSet<GridEntity>(entitiesByPosition[position])
                : new HashSet<GridEntity>();
        }

        // Add these methods to GridPositionManager class:

        public List<GridEntity> GetEntitiesInRadius(GridPosition center, int radius, bool useCircle = true) {
            var result = new List<GridEntity>();
    
            if (useCircle) {
                var tilesInRadius = GridRadiusUtility.GetTilesInCircle(center, radius);
                foreach (var pos in tilesInRadius) {
                    if (entitiesByPosition.ContainsKey(pos)) {
                        result.AddRange(entitiesByPosition[pos]);
                    }
                }
            } else {
                // Fallback to Manhattan distance if needed
                for (int x = -radius; x <= radius; x++) {
                    for (int y = -radius; y <= radius; y++) {
                        var checkPos = new GridPosition(center.x + x, center.y + y);
                        if (checkPos.ManhattanDistance(center) <= radius && entitiesByPosition.ContainsKey(checkPos)) {
                            result.AddRange(entitiesByPosition[checkPos]);
                        }
                    }
                }
            }
    
            return result;
        }

        public bool IsPositionWithinRadius(GridPosition position, GridPosition center, int radius, bool useCircle = true) {
            if (useCircle) {
                return GridRadiusUtility.IsWithinCircleRadius(position, center, radius);
            } else {
                return position.ManhattanDistance(center) <= radius;
            }
        }

        public GridEntity GetNearestEntity(GridPosition position, System.Func<GridEntity, bool> predicate = null)
        {
            GridEntity nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var entity in allEntities)
            {
                if (predicate != null && !predicate(entity)) continue;

                float distance = entity.Position.EuclideanDistance(position);
                if (distance < nearestDistance)
                {
                    nearest = entity;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }
        
        public Grid GetTilemapGrid() {
            return TilemapGrid;
        }

        public List<GridPosition> GetPath(GridPosition start, GridPosition end, bool allowDiagonal = false)
        {
            var path = new List<GridPosition>();

            if (!IsPositionValid(start) || !IsPositionValid(end))
            {
                return path;
            }

            var openSet = new HashSet<GridPosition>();
            var closedSet = new HashSet<GridPosition>();
            var cameFrom = new Dictionary<GridPosition, GridPosition>();
            var gScore = new Dictionary<GridPosition, float>();
            var fScore = new Dictionary<GridPosition, float>();

            openSet.Add(start);
            gScore[start] = 0;
            fScore[start] = HeuristicCost(start, end);

            while (openSet.Count > 0)
            {
                GridPosition current = GetLowestFScore(openSet, fScore);

                if (current == end)
                {
                    // Reconstruct path
                    while (cameFrom.ContainsKey(current))
                    {
                        path.Add(current);
                        current = cameFrom[current];
                    }
                    path.Reverse();
                    return path;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (var neighbor in current.GetNeighbors(allowDiagonal))
                {
                    // Pathfinding now correctly checks IsPositionOccupied, which respects the new flag.
                    // It also ignores the end tile for occupancy checks, allowing movement *to* an occupied tile.
                    if (!IsPositionValid(neighbor) || closedSet.Contains(neighbor) || (neighbor != end && IsPositionOccupied(neighbor)))
                    {
                        continue;
                    }

                    float tentativeGScore = gScore[current] + 1; // Assuming cost of 1 per tile

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                    else if (gScore.ContainsKey(neighbor) && tentativeGScore >= gScore[neighbor])
                    {
                        continue; // This path is not better
                    }

                    // This path is the best until now. Record it.
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + HeuristicCost(neighbor, end);
                }
            }
            return path; // No path found
        }

        private float HeuristicCost(GridPosition a, GridPosition b)
        {
            return a.ManhattanDistance(b);
        }

        private GridPosition GetLowestFScore(HashSet<GridPosition> openSet, Dictionary<GridPosition, float> fScore)
        {
            GridPosition lowest = openSet.First();
            float lowestScore = fScore.ContainsKey(lowest) ? fScore[lowest] : float.MaxValue;

            foreach (var pos in openSet)
            {
                float score = fScore.ContainsKey(pos) ? fScore[pos] : float.MaxValue;
                if (score < lowestScore)
                {
                    lowest = pos;
                    lowestScore = score;
                }
            }
            return lowest;
        }

        public bool PathExists(GridPosition start, GridPosition end, bool allowDiagonal = false)
        {
            var path = GetPath(start, end, allowDiagonal);
            return path.Count > 0;
        }

        public void SnapEntityToGrid(GameObject entity)
        {
            if (entity == null) return;

            GridEntity gridEntity = entity.GetComponent<GridEntity>();
            if (gridEntity == null)
            {
                gridEntity = entity.AddComponent<GridEntity>();
            }

            gridEntity.SnapToGrid();

            if (debugMode)
            {
                Debug.Log($"[GridPositionManager] Snapped {entity.name} to grid {gridEntity.Position}");
            }
        }

        public void SnapAllEntitiesToGrid<T>() where T : Component
        {
            // Use modern, faster FindObjectsByType
            T[] entities = FindObjectsByType<T>(FindObjectsSortMode.None);
            foreach (var entity in entities)
            {
                SnapEntityToGrid(entity.gameObject);
            }
            Debug.Log($"[GridPositionManager] Snapped {entities.Length} entities of type {typeof(T).Name} to grid");
        }
        
        public static List<GridPosition> GetTilesInRadius(GridPosition center, int radius, bool useManhattan = true) {
            var result = new List<GridPosition>();
    
            for (int x = -radius; x <= radius; x++) {
                for (int y = -radius; y <= radius; y++) {
                    var offset = new GridPosition(x, y);
                    var checkPos = center + offset;
            
                    int distance = useManhattan 
                        ? Mathf.Abs(x) + Mathf.Abs(y) 
                        : Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
            
                    if (distance <= radius) {
                        result.Add(checkPos);
                    }
                }
            }
    
            return result;
        }

        private void OnDrawGizmos()
        {
            if (!showGridGizmos || TilemapGrid == null) return;

            Gizmos.color = gridColor;

            int halfWidth = gizmoGridSize / 2;
            int halfHeight = gizmoGridSize / 2;

            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                Vector3 start = GridToWorld(new GridPosition(x, -halfHeight));
                Vector3 end = GridToWorld(new GridPosition(x, halfHeight));
                Gizmos.DrawLine(start, end);
            }

            for (int y = -halfHeight; y <= halfHeight; y++)
            {
                Vector3 start = GridToWorld(new GridPosition(-halfWidth, y));
                Vector3 end = GridToWorld(new GridPosition(halfWidth, y));
                Gizmos.DrawLine(start, end);
            }

            Gizmos.color = Color.red;
            foreach (var kvp in entitiesByPosition)
            {
                if (kvp.Value.Count > 0)
                {
                    Vector3 cellCenter = GridToWorld(kvp.Key);
                    Gizmos.DrawWireCube(cellCenter, Vector3.one * TilemapGrid.cellSize.x * 0.8f);
                }
            }
        }
    }
}