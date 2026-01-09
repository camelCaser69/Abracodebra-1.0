using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WegoSystem
{
    [RequireComponent(typeof(GridEntity))]
    public class MultiTileEntity : MonoBehaviour
    {
        [Header("Footprint")]
        [Tooltip("Defines the size and behavior of this multi-tile entity.")]
        [SerializeField] private MultiTileFootprint footprint;

        [Header("Positioning")]
        [Tooltip("If true, automatically snaps to grid and centers on Start.")]
        [SerializeField] private bool autoSnapOnStart = true;

        [Tooltip("Visual offset applied after centering (for fine-tuning sprite position).")]
        [SerializeField] private Vector3 visualOffset = Vector3.zero;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private Color footprintColor = new Color(1f, 0.5f, 0f, 0.3f);
        [SerializeField] private Color anchorColor = Color.red;

        private GridEntity gridEntity;
        private readonly List<GridPosition> occupiedPositions = new List<GridPosition>();
        private bool isRegistered = false;

        // Public accessors
        public GridPosition AnchorPosition => gridEntity != null ? gridEntity.Position : GridPosition.Zero;
        public IReadOnlyList<GridPosition> OccupiedPositions => occupiedPositions;
        public MultiTileFootprint Footprint => footprint;
        public int InteractionPriority => footprint != null ? footprint.InteractionPriority : 100;

        // Legacy accessor - returns true if ANY blocking is enabled
        public bool BlocksTiles => footprint != null && footprint.BlocksTiles;

        // Granular blocking accessors
        public bool BlocksMovement => footprint != null && footprint.BlocksMovement;
        public bool BlocksSeedPlanting => footprint != null && footprint.BlocksSeedPlanting;
        public bool BlocksToolUsage => footprint != null && footprint.BlocksToolUsage;
        public TileBlockingSettings BlockingSettings => footprint != null ? footprint.BlockingSettings : TileBlockingSettings.None;

        private void Awake()
        {
            gridEntity = GetComponent<GridEntity>();
            // Multi-tile entities manage their own occupancy, don't use the default single-tile occupant system
            gridEntity.isTileOccupant = false;
        }

        private void Start()
        {
            if (footprint == null)
            {
                Debug.LogError($"[MultiTileEntity] {gameObject.name} has no MultiTileFootprint assigned!", this);
                enabled = false;
                return;
            }

            if (autoSnapOnStart)
            {
                SnapToGridAndRegister();
            }
        }

        private void OnDestroy()
        {
            UnregisterFromAllPositions();
        }

        private void OnDisable()
        {
            UnregisterFromAllPositions();
        }

        private void OnEnable()
        {
            if (isRegistered || !Application.isPlaying) return;

            if (occupiedPositions.Count > 0)
            {
                RegisterAtAllPositions();
            }
        }

        /// <summary>
        /// Snaps the entity to the grid and registers at all occupied positions.
        /// </summary>
        public void SnapToGridAndRegister()
        {
            if (GridPositionManager.Instance == null)
            {
                Debug.LogError("[MultiTileEntity] GridPositionManager not found!", this);
                return;
            }

            GridPosition anchorPos = GridPositionManager.Instance.WorldToGrid(transform.position);
            SetAnchorPosition(anchorPos, true);
        }

        /// <summary>
        /// Sets the anchor position and re-registers at all occupied tiles.
        /// </summary>
        public void SetAnchorPosition(GridPosition newAnchor, bool instant = false)
        {
            if (GridPositionManager.Instance == null) return;

            if (!AreAllPositionsValid(newAnchor))
            {
                Debug.LogWarning($"[MultiTileEntity] Cannot place {gameObject.name} at {newAnchor} - some tiles are out of bounds.");
                return;
            }

            UnregisterFromAllPositions();

            gridEntity.SetPosition(newAnchor, instant);

            UpdateVisualPosition();

            CalculateOccupiedPositions(newAnchor);
            RegisterAtAllPositions();
        }

        private void UpdateVisualPosition()
        {
            if (GridPositionManager.Instance == null || footprint == null) return;

            Vector3 anchorWorld = GridPositionManager.Instance.GridToWorld(gridEntity.Position);

            Grid grid = GridPositionManager.Instance.GetTilemapGrid();
            float cellSize = grid != null ? grid.cellSize.x : 1f;

            transform.position = footprint.GetCenteredWorldPosition(anchorWorld, cellSize) + visualOffset;
        }

        private void CalculateOccupiedPositions(GridPosition anchor)
        {
            occupiedPositions.Clear();

            if (footprint == null) return;

            foreach (var offset in footprint.GetLocalOffsets())
            {
                occupiedPositions.Add(new GridPosition(anchor.x + offset.x, anchor.y + offset.y));
            }
        }

        /// <summary>
        /// Checks if all positions for the given anchor would be valid (within map bounds).
        /// </summary>
        public bool AreAllPositionsValid(GridPosition anchor)
        {
            if (footprint == null || GridPositionManager.Instance == null) return false;

            foreach (var offset in footprint.GetLocalOffsets())
            {
                var pos = new GridPosition(anchor.x + offset.x, anchor.y + offset.y);
                if (!GridPositionManager.Instance.IsPositionValid(pos))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if placing at the given anchor would overlap with other occupied tiles.
        /// </summary>
        public bool WouldOverlapOccupiedTiles(GridPosition anchor, bool ignoreSelf = true)
        {
            if (footprint == null || GridPositionManager.Instance == null) return false;

            foreach (var offset in footprint.GetLocalOffsets())
            {
                var pos = new GridPosition(anchor.x + offset.x, anchor.y + offset.y);

                if (ignoreSelf && occupiedPositions.Contains(pos))
                {
                    continue;
                }

                if (GridPositionManager.Instance.IsPositionOccupied(pos))
                {
                    return true;
                }
            }
            return false;
        }

        private void RegisterAtAllPositions()
        {
            if (GridPositionManager.Instance == null || isRegistered) return;

            foreach (var pos in occupiedPositions)
            {
                GridPositionManager.Instance.RegisterMultiTileEntityAtPosition(this, pos);
            }

            isRegistered = true;

            if (GridPositionManager.Instance.DebugMode)
            {
                Debug.Log($"[MultiTileEntity] Registered {gameObject.name} at {occupiedPositions.Count} positions: {string.Join(", ", occupiedPositions)}");
            }
        }

        private void UnregisterFromAllPositions()
        {
            if (GridPositionManager.Instance == null || !isRegistered) return;

            foreach (var pos in occupiedPositions)
            {
                GridPositionManager.Instance.UnregisterMultiTileEntityFromPosition(this, pos);
            }

            isRegistered = false;
        }

        /// <summary>
        /// Checks if this entity occupies the given position.
        /// </summary>
        public bool OccupiesPosition(GridPosition position)
        {
            return occupiedPositions.Contains(position);
        }

        /// <summary>
        /// Gets the center world position of this entity.
        /// </summary>
        public Vector3 GetCenterWorldPosition()
        {
            return transform.position - visualOffset;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || footprint == null) return;
            DrawFootprintGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            if (footprint == null) return;
            DrawFootprintGizmos();
        }

        private void DrawFootprintGizmos()
        {
            GridPosition anchor;
            Vector3 cellSize = Vector3.one;

            if (Application.isPlaying && GridPositionManager.Instance != null)
            {
                anchor = gridEntity != null ? gridEntity.Position : GridPosition.Zero;
                Grid grid = GridPositionManager.Instance.GetTilemapGrid();
                if (grid != null) cellSize = grid.cellSize;
            }
            else
            {
                anchor = new GridPosition(
                    Mathf.FloorToInt(transform.position.x),
                    Mathf.FloorToInt(transform.position.y)
                );
            }

            foreach (var offset in footprint.GetLocalOffsets())
            {
                Vector3 tileCenter;

                if (Application.isPlaying && GridPositionManager.Instance != null)
                {
                    var pos = new GridPosition(anchor.x + offset.x, anchor.y + offset.y);
                    tileCenter = GridPositionManager.Instance.GridToWorld(pos);
                }
                else
                {
                    tileCenter = new Vector3(
                        anchor.x + offset.x + 0.5f,
                        anchor.y + offset.y + 0.5f,
                        0f
                    );
                }

                Gizmos.color = footprintColor;
                Gizmos.DrawCube(tileCenter, new Vector3(cellSize.x * 0.9f, cellSize.y * 0.9f, 0.1f));

                if (offset == Vector2Int.zero)
                {
                    Gizmos.color = anchorColor;
                    Gizmos.DrawWireCube(tileCenter, new Vector3(cellSize.x, cellSize.y, 0.1f));
                }
            }

            Gizmos.color = Color.cyan;
            Vector3 centerPos = transform.position - visualOffset;
            Gizmos.DrawSphere(centerPos, 0.15f);
        }
#endif
    }
}