using System.Collections.Generic;
using UnityEngine;

namespace WegoSystem
{
    public enum PivotMode
    {
        None,
        Automatic,
        Manual
    }

    /// <summary>
    /// Defines the tile blocking behavior for multi-tile entities.
    /// </summary>
    [System.Serializable]
    public struct TileBlockingSettings
    {
        [Tooltip("If true, blocks player/entity movement and pathfinding on all occupied tiles.")]
        public bool blocksMovement;

        [Tooltip("If true, prevents seed planting on all occupied tiles.")]
        public bool blocksSeedPlanting;

        [Tooltip("If true, prevents tool usage (hoe, watering can, etc.) on all occupied tiles.")]
        public bool blocksToolUsage;

        /// <summary>
        /// Returns true if ANY blocking is enabled.
        /// </summary>
        public bool HasAnyBlocking => blocksMovement || blocksSeedPlanting || blocksToolUsage;

        /// <summary>
        /// Create settings with all blocking enabled (default for most multi-tile entities).
        /// </summary>
        public static TileBlockingSettings AllBlocked => new TileBlockingSettings
        {
            blocksMovement = true,
            blocksSeedPlanting = true,
            blocksToolUsage = true
        };

        /// <summary>
        /// Create settings with only movement blocking (allows interactions on tiles).
        /// </summary>
        public static TileBlockingSettings MovementOnly => new TileBlockingSettings
        {
            blocksMovement = true,
            blocksSeedPlanting = false,
            blocksToolUsage = false
        };

        /// <summary>
        /// Create settings with no blocking at all.
        /// </summary>
        public static TileBlockingSettings None => new TileBlockingSettings
        {
            blocksMovement = false,
            blocksSeedPlanting = false,
            blocksToolUsage = false
        };
    }

    [CreateAssetMenu(fileName = "NewMultiTileFootprint", menuName = "WegoSystem/Multi-Tile Footprint")]
    public class MultiTileFootprint : ScriptableObject
    {
        [Header("Size")]
        [Tooltip("Size of the footprint in tiles. (2,2) = 2x2 grid.")]
        [SerializeField] private Vector2Int size = new Vector2Int(2, 2);

        [Header("Visual Positioning")]
        [Tooltip("Determines how the visual position is calculated relative to the anchor (bottom-left) tile.")]
        [SerializeField] private PivotMode pivotMode = PivotMode.Automatic;

        [Tooltip("If Pivot Mode is Manual: The offset in tiles from the bottom-left anchor. (0.5, 0.5) is the center of the first tile.")]
        [SerializeField] private Vector2 manualPivot = new Vector2(0.5f, 0.5f);

        [Header("Interaction")]
        [Tooltip("Interaction priority. Higher values take precedence over tiles and other entities.")]
        [SerializeField] private int interactionPriority = 200;

        [Header("Tile Blocking")]
        [Tooltip("Configure what actions are blocked on tiles occupied by this entity.")]
        [SerializeField] private TileBlockingSettings blockingSettings = TileBlockingSettings.AllBlocked;

        // Public accessors
        public Vector2Int Size => size;
        public PivotMode CurrentPivotMode => pivotMode;
        public int InteractionPriority => interactionPriority;
        public TileBlockingSettings BlockingSettings => blockingSettings;

        // Convenience accessors for individual blocking types
        public bool BlocksMovement => blockingSettings.blocksMovement;
        public bool BlocksSeedPlanting => blockingSettings.blocksSeedPlanting;
        public bool BlocksToolUsage => blockingSettings.blocksToolUsage;

        /// <summary>
        /// Legacy property - returns true if ANY blocking is enabled.
        /// For backwards compatibility with existing code.
        /// </summary>
        public bool BlocksTiles => blockingSettings.HasAnyBlocking;

        /// <summary>
        /// Gets all local grid offsets for this footprint (relative to anchor at 0,0).
        /// </summary>
        public List<Vector2Int> GetLocalOffsets()
        {
            var offsets = new List<Vector2Int>();
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    offsets.Add(new Vector2Int(x, y));
                }
            }
            return offsets;
        }

        /// <summary>
        /// Gets the visual center offset based on pivot mode.
        /// </summary>
        public Vector2 GetCenterOffset(float cellSize = 1f)
        {
            switch (pivotMode)
            {
                case PivotMode.Automatic:
                    return new Vector2(
                        (size.x - 1) * cellSize * 0.5f,
                        (size.y - 1) * cellSize * 0.5f
                    );

                case PivotMode.Manual:
                    return manualPivot * cellSize;

                case PivotMode.None:
                default:
                    return Vector2.zero;
            }
        }

        /// <summary>
        /// Gets the centered world position from an anchor world position.
        /// </summary>
        public Vector3 GetCenteredWorldPosition(Vector3 anchorWorldPosition, float cellSize = 1f)
        {
            Vector2 offset = GetCenterOffset(cellSize);
            return anchorWorldPosition + new Vector3(offset.x, offset.y, 0f);
        }

        private void OnValidate()
        {
            size.x = Mathf.Max(1, size.x);
            size.y = Mathf.Max(1, size.y);
        }
    }
}