using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WegoSystem
{
    public class GridPositionManager : MonoBehaviour
    {
        public static GridPositionManager Instance { get; set; }

        // These are now private to enforce synchronization from the tile grid
        private float cellSize = 1f;
        private Vector3 gridOrigin = Vector3.zero;

        [SerializeField] private Vector2Int gridBounds = new Vector2Int(100, 100);

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
            // The single source of truth for grid alignment
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
                var tileGrid = TileInteractionManager.Instance.interactionGrid;

                // Set internal parameters from the authoritative grid
                this.cellSize = tileGrid.cellSize.x;
                this.gridOrigin = tileGrid.transform.position;

                if (debugMode)
                    Debug.Log($"[GridPositionManager] Successfully synced with TileInteractionManager's grid. CellSize: {cellSize}, Origin: {gridOrigin}");
            }
            else
            {
                Debug.LogError("[GridPositionManager] Could not find TileInteractionManager or its grid to sync with! Grid system may be misaligned.");
            }
        }

        public GridPosition WorldToGrid(Vector3 worldPosition)
        {
            Vector3 localPosition = worldPosition - gridOrigin;
            int x = Mathf.RoundToInt(localPosition.x / cellSize);
            int y = Mathf.RoundToInt(localPosition.y / cellSize);
            return new GridPosition(x, y);
        }

        public Vector3 GridToWorld(GridPosition gridPosition)
        {
            return new Vector3(
                gridPosition.x * cellSize + gridOrigin.x,
                gridPosition.y * cellSize + gridOrigin.y,
                gridOrigin.z
            );
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

            GridPosition nearestGrid = WorldToGrid(entity.transform.position);
            Vector3 snappedWorldPos = GridToWorld(nearestGrid);

            entity.transform.position = snappedWorldPos;

            gridEntity.SetPosition(nearestGrid, instant: true);

            if (debugMode)
            {
                Debug.Log($"[GridPositionManager] Snapped {entity.name} from {entity.transform.position} to grid {nearestGrid} at world {snappedWorldPos}");
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
            if (!showGridGizmos) return;

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
                    Gizmos.DrawWireCube(cellCenter, Vector3.one * cellSize * 0.8f);
                }
            }
        }
    }
}