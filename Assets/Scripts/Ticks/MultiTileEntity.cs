using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WegoSystem
{
    /// <summary>
    /// Represents an entity that occupies multiple tiles on the grid.
    /// Registers itself at all occupied positions so GetEntitiesAt() returns this entity
    /// for any of its tiles.
    /// </summary>
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

        /// <summary>
        /// The anchor position (bottom-left tile of the footprint).
        /// </summary>
        public GridPosition AnchorPosition => gridEntity != null ? gridEntity.Position : GridPosition.Zero;

        /// <summary>
        /// All grid positions this entity occupies.
        /// </summary>
        public IReadOnlyList<GridPosition> OccupiedPositions => occupiedPositions;

        /// <summary>
        /// The footprint definition.
        /// </summary>
        public MultiTileFootprint Footprint => footprint;

        /// <summary>
        /// Interaction priority from footprint.
        /// </summary>
        public int InteractionPriority => footprint != null ? footprint.InteractionPriority : 100;

        /// <summary>
        /// Whether this entity blocks tiles for pathfinding.
        /// </summary>
        public bool BlocksTiles => footprint != null && footprint.BlocksTiles;

        private void Awake()
        {
            gridEntity = GetComponent<GridEntity>();
            
            // Multi-tile entities manage their own tile occupancy
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
            
            // Re-register if we were previously registered
            if (occupiedPositions.Count > 0)
            {
                RegisterAtAllPositions();
            }
        }

        /// <summary>
        /// Snaps the anchor to the grid and registers at all footprint positions.
        /// Also centers the visual based on the footprint's PivotMode.
        /// </summary>
        public void SnapToGridAndRegister()
        {
            if (GridPositionManager.Instance == null)
            {
                Debug.LogError("[MultiTileEntity] GridPositionManager not found!", this);
                return;
            }

            // Snap anchor to grid
            GridPosition anchorPos = GridPositionManager.Instance.WorldToGrid(transform.position);
            
            // Set the position
            SetAnchorPosition(anchorPos, true);
        }

        /// <summary>
        /// Sets the anchor position and updates all occupied tiles.
        /// </summary>
        /// <param name="newAnchor">The new anchor grid position (bottom-left).</param>
        /// <param name="instant">If true, no interpolation.</param>
        public void SetAnchorPosition(GridPosition newAnchor, bool instant = false)
        {
            if (GridPositionManager.Instance == null) return;

            // Validate all positions in the footprint
            if (!AreAllPositionsValid(newAnchor))
            {
                Debug.LogWarning($"[MultiTileEntity] Cannot place {gameObject.name} at {newAnchor} - some tiles are out of bounds.");
                return;
            }

            // Unregister from old positions
            UnregisterFromAllPositions();

            // Update anchor
            gridEntity.SetPosition(newAnchor, instant);

            // Calculate world position (centered if needed)
            UpdateVisualPosition();

            // Calculate and register at all positions
            CalculateOccupiedPositions(newAnchor);
            RegisterAtAllPositions();
        }

        /// <summary>
        /// Updates the visual position based on anchor and centering settings.
        /// </summary>
        private void UpdateVisualPosition()
        {
            if (GridPositionManager.Instance == null || footprint == null) return;

            Vector3 anchorWorld = GridPositionManager.Instance.GridToWorld(gridEntity.Position);
            
            // Get the grid's cell size (assuming uniform cells)
            Grid grid = GridPositionManager.Instance.GetTilemapGrid();
            float cellSize = grid != null ? grid.cellSize.x : 1f;
            
            // Logic change: The Footprint class now handles PivotMode (None, Automatic, Manual) internally.
            // If mode is 'None', it returns anchorWorld. If 'Automatic' or 'Manual', it calculates the offset.
            transform.position = footprint.GetCenteredWorldPosition(anchorWorld, cellSize) + visualOffset;
        }

        /// <summary>
        /// Calculates all grid positions this entity occupies based on anchor and footprint.
        /// </summary>
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
        /// Checks if all footprint positions would be valid at the given anchor.
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
        /// Checks if placing at the given anchor would overlap with any occupied tiles.
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

        /// <summary>
        /// Registers this entity at all occupied positions.
        /// </summary>
        private void RegisterAtAllPositions()
        {
            if (GridPositionManager.Instance == null || isRegistered) return;

            foreach (var pos in occupiedPositions)
            {
                GridPositionManager.Instance.RegisterEntityAtPosition(gridEntity, pos, BlocksTiles);
            }
            
            isRegistered = true;
            
            if (GridPositionManager.Instance.DebugMode)
            {
                Debug.Log($"[MultiTileEntity] Registered {gameObject.name} at {occupiedPositions.Count} positions: {string.Join(", ", occupiedPositions)}");
            }
        }

        /// <summary>
        /// Unregisters this entity from all occupied positions.
        /// </summary>
        private void UnregisterFromAllPositions()
        {
            if (GridPositionManager.Instance == null || !isRegistered) return;

            foreach (var pos in occupiedPositions)
            {
                GridPositionManager.Instance.UnregisterEntityFromPosition(gridEntity, pos);
            }
            
            isRegistered = false;
        }

        /// <summary>
        /// Checks if this entity occupies the given grid position.
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
                // In editor, estimate from transform position
                anchor = new GridPosition(
                    Mathf.FloorToInt(transform.position.x),
                    Mathf.FloorToInt(transform.position.y)
                );
            }

            // Draw each tile in the footprint
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

                // Footprint tile
                Gizmos.color = footprintColor;
                Gizmos.DrawCube(tileCenter, new Vector3(cellSize.x * 0.9f, cellSize.y * 0.9f, 0.1f));

                // Anchor marker
                if (offset == Vector2Int.zero)
                {
                    Gizmos.color = anchorColor;
                    Gizmos.DrawWireCube(tileCenter, new Vector3(cellSize.x, cellSize.y, 0.1f));
                }
            }

            // Draw center point
            Gizmos.color = Color.cyan;
            Vector3 centerPos = transform.position - visualOffset;
            Gizmos.DrawSphere(centerPos, 0.15f);
        }
#endif
    }
}