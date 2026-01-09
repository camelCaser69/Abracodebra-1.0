// Assets/Scripts/Ticks/MultiTileFootprint.cs
using System.Collections.Generic;
using UnityEngine;

namespace WegoSystem
{
    /// <summary>
    /// Defines the footprint (shape) of a multi-tile entity.
    /// The anchor is always at (0,0) - bottom-left corner.
    /// </summary>
    [CreateAssetMenu(fileName = "New MultiTileFootprint", menuName = "Abracodabra/Multi-Tile Footprint")]
    public class MultiTileFootprint : ScriptableObject
    {
        [Tooltip("Size of the footprint in tiles. (2,2) = 2x2 grid.")]
        [SerializeField] private Vector2Int size = new Vector2Int(2, 2);

        [Tooltip("If true, the visual pivot will be centered on the footprint. If false, pivot stays at anchor (bottom-left).")]
        [SerializeField] private bool centerPivot = true;

        [Tooltip("Interaction priority. Higher values take precedence over tiles and other entities.")]
        [SerializeField] private int interactionPriority = 200;

        [Tooltip("If true, this entity blocks movement/pathfinding on all occupied tiles.")]
        [SerializeField] private bool blocksTiles = true;

        public Vector2Int Size => size;
        public bool CenterPivot => centerPivot;
        public int InteractionPriority => interactionPriority;
        public bool BlocksTiles => blocksTiles;

        /// <summary>
        /// Returns all local tile offsets from the anchor (0,0).
        /// For a 2x2: (0,0), (1,0), (0,1), (1,1)
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
        /// Returns the center offset for visual positioning.
        /// For a 2x2 with 1-unit cells, center is at (0.5, 0.5) from anchor.
        /// </summary>
        public Vector2 GetCenterOffset(float cellSize = 1f)
        {
            // Center is at half the size, minus half a cell (since anchor is at cell center)
            return new Vector2(
                (size.x - 1) * cellSize * 0.5f,
                (size.y - 1) * cellSize * 0.5f
            );
        }

        /// <summary>
        /// Returns the world position for the visual center given an anchor world position.
        /// </summary>
        public Vector3 GetCenteredWorldPosition(Vector3 anchorWorldPosition, float cellSize = 1f)
        {
            if (!centerPivot) return anchorWorldPosition;

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
