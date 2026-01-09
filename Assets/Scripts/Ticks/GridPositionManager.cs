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

        // NEW: Store MultiTileEntity references for granular blocking queries
        private readonly Dictionary<GridPosition, HashSet<MultiTileEntity>> multiTileEntitiesByPosition = new Dictionary<GridPosition, HashSet<MultiTileEntity>>();

        // Legacy: Keep for backwards compatibility with existing code
        private readonly Dictionary<GridPosition, HashSet<GridEntity>> multiTileOccupancy = new Dictionary<GridPosition, HashSet<GridEntity>>();

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

        #region Generic Occupancy Checks (Legacy + Combined)

        /// <summary>
        /// Returns true if ANY entity is occupying this position (legacy behavior).
        /// For granular checks, use IsMovementBlockedAt, IsSeedPlantingBlockedAt, or IsToolUsageBlockedAt.
        /// </summary>
        public bool IsPositionOccupied(GridPosition position)
        {
            // Check single-tile occupants
            if (entitiesByPosition.TryGetValue(position, out var entities))
            {
                if (entities.Any(entity => entity.isTileOccupant))
                {
                    return true;
                }
            }

            // Check multi-tile occupants (legacy dictionary)
            if (multiTileOccupancy.TryGetValue(position, out var multiEntities))
            {
                if (multiEntities.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Granular Blocking Checks (NEW)

        /// <summary>
        /// Returns true if player/entity movement is blocked at this position.
        /// </summary>
        public bool IsMovementBlockedAt(GridPosition position)
        {
            // Check single-tile occupants (always block movement)
            if (entitiesByPosition.TryGetValue(position, out var entities))
            {
                if (entities.Any(entity => entity.isTileOccupant))
                {
                    return true;
                }
            }

            // Check multi-tile entities with movement blocking enabled
            if (multiTileEntitiesByPosition.TryGetValue(position, out var multiTileEntities))
            {
                foreach (var mte in multiTileEntities)
                {
                    if (mte != null && mte.BlocksMovement)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if seed planting is blocked at this position.
        /// </summary>
        public bool IsSeedPlantingBlockedAt(GridPosition position)
        {
            // Check single-tile occupants (plants always block planting on same tile)
            if (entitiesByPosition.TryGetValue(position, out var entities))
            {
                if (entities.Any(entity => entity.isTileOccupant))
                {
                    return true;
                }
            }

            // Check multi-tile entities with seed planting blocking enabled
            if (multiTileEntitiesByPosition.TryGetValue(position, out var multiTileEntities))
            {
                foreach (var mte in multiTileEntities)
                {
                    if (mte != null && mte.BlocksSeedPlanting)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if tool usage (hoe, watering can, etc.) is blocked at this position.
        /// </summary>
        public bool IsToolUsageBlockedAt(GridPosition position)
        {
            // Check multi-tile entities with tool usage blocking enabled
            if (multiTileEntitiesByPosition.TryGetValue(position, out var multiTileEntities))
            {
                foreach (var mte in multiTileEntities)
                {
                    if (mte != null && mte.BlocksToolUsage)
                    {
                        return true;
                    }
                }
            }

            // Note: Single-tile entities (plants) typically don't block tool usage
            // because you might want to water plants, etc.
            // If you want plants to block tool usage, add that check here.

            return false;
        }

        /// <summary>
        /// Gets the combined blocking settings for a position from all multi-tile entities.
        /// </summary>
        public TileBlockingSettings GetBlockingSettingsAt(GridPosition position)
        {
            var combined = TileBlockingSettings.None;

            if (multiTileEntitiesByPosition.TryGetValue(position, out var multiTileEntities))
            {
                foreach (var mte in multiTileEntities)
                {
                    if (mte != null)
                    {
                        var settings = mte.BlockingSettings;
                        combined.blocksMovement |= settings.blocksMovement;
                        combined.blocksSeedPlanting |= settings.blocksSeedPlanting;
                        combined.blocksToolUsage |= settings.blocksToolUsage;
                    }
                }
            }

            // Check single-tile occupants
            if (entitiesByPosition.TryGetValue(position, out var entities))
            {
                if (entities.Any(entity => entity.isTileOccupant))
                {
                    combined.blocksMovement = true;
                    combined.blocksSeedPlanting = true;
                }
            }

            return combined;
        }

        #endregion

        #region Entity Registration

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

        /// <summary>
        /// Legacy method - registers entity at position with optional occupancy flag.
        /// Prefer RegisterMultiTileEntityAtPosition for multi-tile entities.
        /// </summary>
        public void RegisterEntityAtPosition(GridEntity entity, GridPosition position, bool occupiesTile = false)
        {
            if (entity == null) return;

            if (!entitiesByPosition.ContainsKey(position))
            {
                entitiesByPosition[position] = new HashSet<GridEntity>();
            }
            entitiesByPosition[position].Add(entity);

            if (occupiesTile)
            {
                if (!multiTileOccupancy.ContainsKey(position))
                {
                    multiTileOccupancy[position] = new HashSet<GridEntity>();
                }
                multiTileOccupancy[position].Add(entity);
            }

            if (!allEntities.Contains(entity))
            {
                allEntities.Add(entity);
            }
        }

        /// <summary>
        /// Legacy method - unregisters entity from position.
        /// </summary>
        public void UnregisterEntityFromPosition(GridEntity entity, GridPosition position)
        {
            if (entity == null) return;

            if (entitiesByPosition.ContainsKey(position))
            {
                entitiesByPosition[position].Remove(entity);
                if (entitiesByPosition[position].Count == 0)
                {
                    entitiesByPosition.Remove(position);
                }
            }

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
        /// Registers a multi-tile entity at a specific position with its blocking settings.
        /// </summary>
        public void RegisterMultiTileEntityAtPosition(MultiTileEntity multiTileEntity, GridPosition position)
        {
            if (multiTileEntity == null) return;

            // Register in the new multi-tile entity dictionary
            if (!multiTileEntitiesByPosition.ContainsKey(position))
            {
                multiTileEntitiesByPosition[position] = new HashSet<MultiTileEntity>();
            }
            multiTileEntitiesByPosition[position].Add(multiTileEntity);

            // Also register in legacy dictionaries for backwards compatibility
            var gridEntity = multiTileEntity.GetComponent<GridEntity>();
            if (gridEntity != null)
            {
                if (!entitiesByPosition.ContainsKey(position))
                {
                    entitiesByPosition[position] = new HashSet<GridEntity>();
                }
                entitiesByPosition[position].Add(gridEntity);

                // Add to legacy multiTileOccupancy if any blocking is enabled
                if (multiTileEntity.BlocksTiles)
                {
                    if (!multiTileOccupancy.ContainsKey(position))
                    {
                        multiTileOccupancy[position] = new HashSet<GridEntity>();
                    }
                    multiTileOccupancy[position].Add(gridEntity);
                }

                if (!allEntities.Contains(gridEntity))
                {
                    allEntities.Add(gridEntity);
                }
            }
        }

        /// <summary>
        /// Unregisters a multi-tile entity from a specific position.
        /// </summary>
        public void UnregisterMultiTileEntityFromPosition(MultiTileEntity multiTileEntity, GridPosition position)
        {
            if (multiTileEntity == null) return;

            // Remove from new multi-tile entity dictionary
            if (multiTileEntitiesByPosition.ContainsKey(position))
            {
                multiTileEntitiesByPosition[position].Remove(multiTileEntity);
                if (multiTileEntitiesByPosition[position].Count == 0)
                {
                    multiTileEntitiesByPosition.Remove(position);
                }
            }

            // Also remove from legacy dictionaries
            var gridEntity = multiTileEntity.GetComponent<GridEntity>();
            if (gridEntity != null)
            {
                if (entitiesByPosition.ContainsKey(position))
                {
                    entitiesByPosition[position].Remove(gridEntity);
                    if (entitiesByPosition[position].Count == 0)
                    {
                        entitiesByPosition.Remove(position);
                    }
                }

                if (multiTileOccupancy.ContainsKey(position))
                {
                    multiTileOccupancy[position].Remove(gridEntity);
                    if (multiTileOccupancy[position].Count == 0)
                    {
                        multiTileOccupancy.Remove(position);
                    }
                }
            }
        }

        #endregion

        #region Entity Queries

        public MultiTileEntity GetMultiTileEntityAt(GridPosition position)
        {
            if (multiTileEntitiesByPosition.TryGetValue(position, out var multiTileEntities))
            {
                return multiTileEntities.FirstOrDefault(mte => mte != null && mte.OccupiesPosition(position));
            }

            // Fallback to legacy check
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

        public List<MultiTileEntity> GetAllMultiTileEntitiesAt(GridPosition position)
        {
            var result = new List<MultiTileEntity>();

            if (multiTileEntitiesByPosition.TryGetValue(position, out var multiTileEntities))
            {
                result.AddRange(multiTileEntities.Where(mte => mte != null && mte.OccupiesPosition(position)));
            }

            return result;
        }

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

        #region Pathfinding

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
                    // Use IsMovementBlockedAt for pathfinding instead of IsPositionOccupied
                    if (!IsPositionValid(neighbor) || closedSet.Contains(neighbor) || (neighbor != end && IsMovementBlockedAt(neighbor)))
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

        #endregion

        #region Utility Methods

        public void SnapEntityToGrid(GameObject entity)
        {
            if (entity == null) return;

            GridEntity gridEntity = entity.GetComponent<GridEntity>();
            if (gridEntity == null)
            {
                gridEntity = entity.AddComponent<GridEntity>();
            }

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