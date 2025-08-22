using UnityEngine;
using WegoSystem;

namespace WegoSystem
{
    [CreateAssetMenu(fileName = "MapConfiguration", menuName = "Game/Map Configuration")]
    public class MapConfiguration : ScriptableObject
    {
        [Header("Map Dimensions")]
        public Vector2Int mapSize = new Vector2Int(100, 100);
        public Vector2Int gridOffset = Vector2Int.zero; // For future: offset from origin if needed

        [Header("Visual Settings")]
        public int gizmoDisplayRadius = 20;
        public bool autoScaleGizmos = true;

        [Header("Camera Settings - Pulled from PixelPerfectSetup")]
        public Vector2Int referenceResolution = new Vector2Int(320, 180);
        public int pixelsPerUnit = 6;

        /// <summary>
        /// Gets the center grid position of the map.
        /// </summary>
        public GridPosition GetMapCenter() => new GridPosition(mapSize.x / 2, mapSize.y / 2);

        /// <summary>
        /// Gets the world-space bounds of the map.
        /// </summary>
        public Bounds GetMapBounds()
        {
            Vector3 center = new Vector3(mapSize.x / 2f, mapSize.y / 2f, 0);
            Vector3 size = new Vector3(mapSize.x, mapSize.y, 1);
            return new Bounds(center + (Vector3Int)gridOffset, size);
        }

        /// <summary>
        /// Gets a gizmo display size that adapts to the map size to prevent excessive drawing.
        /// </summary>
        public int GetAdaptiveGizmoSize()
        {
            if (!autoScaleGizmos) return gizmoDisplayRadius;
            return Mathf.Min(gizmoDisplayRadius, Mathf.Max(mapSize.x, mapSize.y) / 5);
        }
    }
}