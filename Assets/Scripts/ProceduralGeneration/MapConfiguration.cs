using UnityEngine;
using WegoSystem;

namespace WegoSystem
{
    [CreateAssetMenu(fileName = "MapConfiguration", menuName = "Game/Map Configuration")]
    public class MapConfiguration : ScriptableObject
    {
        [Header("Map Dimensions")]
        public Vector2Int mapSize = new Vector2Int(100, 100);
        public Vector2Int gridOffset = Vector2Int.zero;

        [Header("Visual Settings")]
        public int gizmoDisplayRadius = 20;
        public bool autoScaleGizmos = true;

        [Header("Camera Settings")] // Renamed header for clarity
        public Vector2Int referenceResolution = new Vector2Int(320, 180);
        public int pixelsPerUnit = 6;
        
        public GridPosition GetMapCenter() => new GridPosition(mapSize.x / 2, mapSize.y / 2);
        
        // REMOVED: This method was misleading as it didn't account for Grid Cell Size.
        // The calculation is now correctly handled in MapBoundsVisualizer.
        // public Bounds GetMapBounds() { ... }

        public int GetAdaptiveGizmoSize()
        {
            if (!autoScaleGizmos) return gizmoDisplayRadius;
            return Mathf.Min(gizmoDisplayRadius, Mathf.Max(mapSize.x, mapSize.y) / 5);
        }
    }
}