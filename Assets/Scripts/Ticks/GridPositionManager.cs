// Assets/Scripts/Ticks/GridPositionManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WegoSystem
{
    public class GridPositionManager : SingletonMonoBehaviour<GridPositionManager>
    {
        [Tooltip("The central configuration for map size and properties. This is the single source of truth.")]
        [SerializeField] private MapConfiguration mapConfig;

        [SerializeField] private TileInteractionManager tileInteractionManager;

        private Grid _tilemapGrid;
        private Grid TilemapGrid => _tilemapGrid;

        [Header("Gizmos & Debugging")]
        [SerializeField] private bool showGridGizmos = true;
        [SerializeField] private Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        [SerializeField] private bool debugMode = false;

        private readonly Dictionary<GridPosition, HashSet<GridEntity>> entitiesByPosition = new Dictionary<GridPosition, HashSet<GridEntity>>();
        private readonly HashSet<GridEntity> allEntities = new HashSet<GridEntity>();

        // Multi-tile entity support: tracks which positions are occupied by multi-tile entities
        private readonly Dictionary<GridPosition, HashSet<GridEntity>> multiTileOccupancy = new Dictionary<GridPosition, HashSet<GridEntity>>();

        /// <summary>
        /// Whether debug mode is enabled.
        /// </summary>
        public bool DebugMode => debugMode;

        protected override void OnAwake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_tilemapGrid != null) return;

            if (mapConfig == null)
            {
                Debug.LogError("[GridPositionManager] CRITICAL: MapConfiguration is not assigned! Grid system will not function correctly.", this);
            }

            if (debugMode) Debug.Log("[GridPositionManager] Grid reference is null. Initializing now.");
            SyncWithTileGrid();
        }

        public void Initialize()
        {
            EnsureInitialized();
        }

        public void SyncWithTileGrid()
        {
            if (tileInteractionManager != null && tileInteractionManager.interactionGrid != null)
            {
                this._tilemapGrid = tileInteractionManager.interactionGrid;
                if (debugMode) Debug.Log($"[GridPositionManager] Synced with assigned TileInteractionManager's grid: '{this._tilemapGrid.name}'.");
                return;
            }

            if (TileInteractionManager.Instance != null && TileInteractionManager.Instance.interactionGrid != null)
            {
                this._tilemapGrid = TileInteractionManager.Instance.interactionGrid;
                tileInteractionManager = TileInteractionManager.Instance;
                if (debugMode) Debug.Log($"[GridPositionManager] Synced with singleton TileInteractionManager.Instance's grid: '{this._tilemapGrid.name}'.");
                return;
            }

            if (_tilemapGrid == null)
            {
                Debug.LogError("[GridPositionManager] Could not find TileInteractionManager or its grid to sync with! Grid system may be misaligned. Please assign the TileInteractionManager in the Inspector.");
            }
        }

        public GridPosition WorldToGrid(Vector3 worldPosition)
        {
            EnsureInitialized();
            if (TilemapGrid == null) return GridPosition.Zero;
            Vector3Int cellPos = TilemapGrid.WorldToCell(worldPosition);
            return new GridPosition(cellPos);
        }

        public Vector3 GridToWorld(GridPosition gridPosition)
        {
            EnsureInitialized();
            if (TilemapGrid == null) return Vector3.zero;
            return TilemapGrid.GetCellCenterWorld(gridPosition.ToVector3Int());
        }

        public Vector3 GetCellCenter(GridPosition gridPosition)
        {
            return GridToWorld(gridPosition);
        }

        public bool IsPositionValid(GridPosition position)
        {
            if (mapConfig == null)
            {
                Debug.LogError("[GridPositionManager] MapConfiguration not assigned!");
                return false;
            }
            return position.x >= 0 && position.x < mapConfig.mapSize.x &&
                   position.y >= 0 && position.y < mapConfig.mapSize.y;
        }

        public GridPosition GetMapCenter()
        {
            if (mapConfig == null)
            {
                Debug.LogError("[GridPositionManager] MapConfiguration not assigned! Returning fallback center.");
                return new GridPosition(50, 50);
            }
            return mapConfig.GetMapCenter();
        }

        public Vector3 GetMapCenterWorld()
        {
            return GridToWorld(GetMapCenter());
        }

        /// <summary>
        /// Checks if a position is occupied by any tile-occupying entity (single or multi-tile).
        /// </summary>
        public bool IsPositionOccupied(GridPosition position)
        {
            // Check single-tile entities
            if (entitiesByPosition.TryGetValue(position, out var entities))
            {
                if (entities.Any(entity => entity.isTileOccupant))
                {
                    return true;
                }
            }

            // Check multi-tile entity occupancy
            if (multiTileOccupancy.TryGetValue(position, out var multiEntities))
            {
                if (multiEntities.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        #region Standard Entity Registration

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

        #endregion

        #region Multi-Tile Entity Registration

        /// <summary>
        /// Registers an entity at a specific position. Used by MultiTileEntity to register at multiple positions.
        /// </summary>
        /// <param name="entity">The GridEntity to register.</param>
        /// <param name="position">The grid position to register at.</param>
        /// <param name="occupiesTile">If true, this position is marked as occupied for pathfinding.</param>
        public void RegisterEntityAtPosition(GridEntity entity, GridPosition position, bool occupiesTile = false)
        {
            if (entity == null) return;

            // Add to the general entity-by-position tracking
            if (!entitiesByPosition.ContainsKey(position))
            {
                entitiesByPosition[position] = new HashSet<GridEntity>();
            }
            entitiesByPosition[position].Add(entity);

            // If this blocks tiles, add to multi-tile occupancy
            if (occupiesTile)
            {
                if (!multiTileOccupancy.ContainsKey(position))
                {
                    multiTileOccupancy[position] = new HashSet<GridEntity>();
                }
                multiTileOccupancy[position].Add(entity);
            }

            // Add to allEntities if not already there
            if (!allEntities.Contains(entity))
            {
                allEntities.Add(entity);
            }
        }

        /// <summary>
        /// Unregisters an entity from a specific position. Used by MultiTileEntity for cleanup.
        /// </summary>
        /// <param name="entity">The GridEntity to unregister.</param>
        /// <param name="position">The grid position to unregister from.</param>
        public void UnregisterEntityFromPosition(GridEntity entity, GridPosition position)
        {
            if (entity == null) return;

            // Remove from general entity tracking
            if (entitiesByPosition.ContainsKey(position))
            {
                entitiesByPosition[position].Remove(entity);
                if (entitiesByPosition[position].Count == 0)
                {
                    entitiesByPosition.Remove(position);
                }
            }

            // Remove from multi-tile occupancy
            if (multiTileOccupancy.ContainsKey(position))
            {
                multiTileOccupancy[position].Remove(entity);
                if (multiTileOccupancy[position].Count == 0)
                {
                    multiTileOccupancy.Remove(position);
                }
            }
        }

        /// <summary>
        /// Gets the MultiTileEntity at a given position, if any.
        /// </summary>
        public MultiTileEntity GetMultiTileEntityAt(GridPosition position)
        {
            if (!entitiesByPosition.TryGetValue(position, out var entities))
            {
                return null;
            }

            foreach (var entity in entities)
            {
                var multiTile = entity.GetComponent<MultiTileEntity>();
                if (multiTile != null && multiTile.OccupiesPosition(position))
                {
                    return multiTile;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets all MultiTileEntities at a given position.
        /// </summary>
        public List<MultiTileEntity> GetAllMultiTileEntitiesAt(GridPosition position)
        {
            var result = new List<MultiTileEntity>();

            if (!entitiesByPosition.TryGetValue(position, out var entities))
            {
                return result;
            }

            foreach (var entity in entities)
            {
                var multiTile = entity.GetComponent<MultiTileEntity>();
                if (multiTile != null && multiTile.OccupiesPosition(position))
                {
                    result.Add(multiTile);
                }
            }

            return result;
        }

        #endregion

        #region Entity Queries

        public HashSet<GridEntity> GetEntitiesAt(GridPosition position)
        {
            return entitiesByPosition.ContainsKey(position)
                ? new HashSet<GridEntity>(entitiesByPosition[position])
                : new HashSet<GridEntity>();
        }

        public List<GridEntity> GetEntitiesInRadius(GridPosition center, int radius, bool useCircle = true)
        {
            var result = new List<GridEntity>();

            if (useCircle)
            {
                var tilesInRadius = GridRadiusUtility.GetTilesInCircle(center, radius);
                foreach (var pos in tilesInRadius)
                {
                    if (entitiesByPosition.ContainsKey(pos))
                    {
                        result.AddRange(entitiesByPosition[pos]);
                    }
                }
            }
            else
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        var checkPos = new GridPosition(center.x + x, center.y + y);
                        if (checkPos.ManhattanDistance(center) <= radius && entitiesByPosition.ContainsKey(checkPos))
                        {
                            result.AddRange(entitiesByPosition[checkPos]);
                        }
                    }
                }
            }

            // Remove duplicates (same multi-tile entity might be at multiple positions)
            return result.Distinct().ToList();
        }

        public bool IsPositionWithinRadius(GridPosition position, GridPosition center, int radius, bool useCircle = true)
        {
            if (useCircle)
            {
                return GridRadiusUtility.IsWithinCircleRadius(position, center, radius);
            }
            else
            {
                return position.ManhattanDistance(center) <= radius;
            }
        }

        public GridEntity GetNearestEntity(GridPosition position, Func<GridEntity, bool> predicate = null)
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

        #endregion

        #region Utility Methods

        public Grid GetTilemapGrid()
        {
            EnsureInitialized();
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
                    if (!IsPositionValid(neighbor) || closedSet.Contains(neighbor) || (neighbor != end && IsPositionOccupied(neighbor)))
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
            return path;
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

            // Check if this is a multi-tile entity - if so, use its own registration
            var multiTileEntity = entity.GetComponent<MultiTileEntity>();
            if (multiTileEntity != null)
            {
                multiTileEntity.SnapToGridAndRegister();
                if (debugMode)
                {
                    Debug.Log($"[GridPositionManager] Snapped MultiTileEntity {entity.name} to grid at {multiTileEntity.AnchorPosition}");
                }
                return;
            }

            gridEntity.SnapToGrid();
            RegisterEntity(gridEntity);

            if (debugMode)
            {
                Debug.Log($"[GridPositionManager] Snapped and Registered {entity.name} to grid {gridEntity.Position}");
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

        public static List<GridPosition> GetTilesInRadius(GridPosition center, int radius, bool useManhattan = true)
        {
            var result = new List<GridPosition>();

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    var checkPos = new GridPosition(center.x + x, center.y + y);

                    if (Instance != null && !Instance.IsPositionValid(checkPos)) continue;

                    int distance = useManhattan
                        ? Mathf.Abs(x) + Mathf.Abs(y)
                        : Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));

                    if (distance <= radius)
                    {
                        result.Add(checkPos);
                    }
                }
            }

            return result;
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (!showGridGizmos || mapConfig == null) return;

            EnsureInitialized();
            if (_tilemapGrid == null) return;

            Gizmos.color = gridColor;

            int displaySize = mapConfig.GetAdaptiveGizmoSize();
            int displayWidth = Mathf.Min(displaySize, mapConfig.mapSize.x);
            int displayHeight = Mathf.Min(displaySize, mapConfig.mapSize.y);

            for (int x = 0; x <= displayWidth; x++)
            {
                Vector3 start = GridToWorld(new GridPosition(x, 0));
                Vector3 end = GridToWorld(new GridPosition(x, displayHeight));
                Gizmos.DrawLine(start, end);
            }

            for (int y = 0; y <= displayHeight; y++)
            {
                Vector3 start = GridToWorld(new GridPosition(0, y));
                Vector3 end = GridToWorld(new GridPosition(displayWidth, y));
                Gizmos.DrawLine(start, end);
            }

            Gizmos.color = Color.green;
            Vector3 origin = GridToWorld(GridPosition.Zero);
            Gizmos.DrawWireSphere(origin, _tilemapGrid.cellSize.x * 0.2f);

            // Draw occupied tiles
            Gizmos.color = Color.red;
            if (entitiesByPosition != null)
            {
                foreach (var kvp in entitiesByPosition)
                {
                    if (kvp.Value.Count > 0)
                    {
                        Vector3 cellCenter = GridToWorld(kvp.Key);
                        Gizmos.DrawWireCube(cellCenter, Vector3.one * _tilemapGrid.cellSize.x * 0.8f);
                    }
                }
            }

            // Draw multi-tile occupancy with different color
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange
            if (multiTileOccupancy != null)
            {
                foreach (var kvp in multiTileOccupancy)
                {
                    if (kvp.Value.Count > 0)
                    {
                        Vector3 cellCenter = GridToWorld(kvp.Key);
                        Gizmos.DrawCube(cellCenter, Vector3.one * _tilemapGrid.cellSize.x * 0.5f);
                    }
                }
            }
        }

        #endregion
    }
}