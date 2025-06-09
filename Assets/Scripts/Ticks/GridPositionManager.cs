using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WegoSystem
{
    public class GridPositionManager : MonoBehaviour
    {
        public static GridPositionManager Instance { get; set; }

        // The authoritative grid, obtained from TileInteractionManager.
        private Grid tilemapGrid;

        [Header("Grid Boundaries")]
        [SerializeField] private Vector2Int gridBounds = new Vector2Int(100, 100);

        [Header("Gizmos & Debug")]
        [SerializeField] private bool showGridGizmos = true;
        [SerializeField] private Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        [SerializeField] private int gizmoGridSize = 20;
        [SerializeField] private bool debugMode = false;

        private readonly Dictionary<GridPosition, HashSet<GridEntity>> entitiesByPosition = new Dictionary<GridPosition, HashSet<GridEntity>>();
        private readonly HashSet<GridEntity> allEntities = new HashSet<GridEntity>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void Start()
        {
            // The single source of truth for grid alignment.
            SyncWithTileGrid();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SyncWithTileGrid()
        {
            if (TileInteractionManager.Instance != null && TileInteractionManager.Instance.interactionGrid != null)
            {
                // Store a direct reference to the authoritative grid component.
                this.tilemapGrid = TileInteractionManager.Instance.interactionGrid;

                if (debugMode)
                    Debug.Log($"[GridPositionManager] Successfully synced with TileInteractionManager's grid: '{this.tilemapGrid.name}'.");
            }
            else
            {
                Debug.LogError("[GridPositionManager] Could not find TileInteractionManager or its grid to sync with! Grid system may be misaligned.");
            }
        }

        public GridPosition WorldToGrid(Vector3 worldPosition)
        {
            if (tilemapGrid == null) return GridPosition.Zero;
            // Use the authoritative grid's built-in conversion method.
            Vector3Int cellPos = tilemapGrid.WorldToCell(worldPosition);
            return new GridPosition(cellPos);
        }

        public Vector3 GridToWorld(GridPosition gridPosition)
        {
            if (tilemapGrid == null) return Vector3.zero;
            // Use the authoritative grid's method to get the *center* of the cell.
            // This solves the corner-snapping issue.
            return tilemapGrid.GetCellCenterWorld(gridPosition.ToVector3Int());
        }

        public Vector3 GetCellCenter(GridPosition gridPosition)
        {
            return GridToWorld(gridPosition);
        }

        public bool IsPositionValid(GridPosition position)
        {
            // This boundary check is independent of the grid component itself.
            return position.x >= -gridBounds.x / 2 && position.x < gridBounds.x / 2 &&
                   position.y >= -gridBounds.y / 2 && position.y < gridBounds.y / 2;
        }

        public bool IsPositionOccupied(GridPosition position)
        {
            return entitiesByPosition.ContainsKey(position) && entitiesByPosition[position].Count > 0;
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

        public List<GridEntity> GetEntitiesInRadius(GridPosition center, int radius, bool useManhattan = true)
        {
            var result = new List<GridEntity>();

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    var checkPos = new GridPosition(center.x + x, center.y + y);

                    if (useManhattan)
                    {
                        if (checkPos.ManhattanDistance(center) > radius) continue;
                    }
                    else
                    {
                        if (checkPos.ChebyshevDistance(center) > radius) continue;
                    }

                    if (entitiesByPosition.ContainsKey(checkPos))
                    {
                        result.AddRange(entitiesByPosition[checkPos]);
                    }
                }
            }

            return result;
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
                    if (!IsPositionValid(neighbor) || IsPositionOccupied(neighbor) || closedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    float tentativeGScore = gScore[current] + 1;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                    else if (gScore.ContainsKey(neighbor) && tentativeGScore >= gScore[neighbor])
                    {
                        continue;
                    }

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
            T[] entities = FindObjectsByType<T>(FindObjectsSortMode.None);
            foreach (var entity in entities)
            {
                SnapEntityToGrid(entity.gameObject);
            }
            Debug.Log($"[GridPositionManager] Snapped {entities.Length} entities of type {typeof(T).Name} to grid");
        }

        void OnDrawGizmos()
        {
            if (!showGridGizmos || tilemapGrid == null) return;

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
                    Gizmos.DrawWireCube(cellCenter, Vector3.one * tilemapGrid.cellSize.x * 0.8f);
                }
            }
        }
    }
}