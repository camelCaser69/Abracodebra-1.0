using UnityEngine;
using WegoSystem;

namespace WegoSystem.EditorTools
{
    [ExecuteInEditMode]
    public class MapBoundsVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MapConfiguration mapConfig;

        [Header("Gizmo Settings")]
        [SerializeField] private Color boundsColor = Color.green;
        [SerializeField] private Color safeAreaColor = Color.yellow;
        [SerializeField] private bool showCameraArea = true;

        private void OnDrawGizmos()
        {
            if (mapConfig == null) return;

            var gridManager = GridPositionManager.Instance;
            if (gridManager == null)
            {
                // In Edit mode, the singleton might not be set. Try to find it in the scene.
                // UPDATED: Replaced obsolete FindObjectOfType with FindFirstObjectByType.
                gridManager = FindFirstObjectByType<GridPositionManager>();
                if (gridManager == null) return; // If still not found, we cannot proceed.
            }
            
            var grid = gridManager.GetTilemapGrid();
            if (grid == null) return;

            // 1. Get the world position of the CENTER of the first and last tiles.
            Vector3 firstCellCenter = grid.GetCellCenterWorld(new Vector3Int(0, 0, 0));
            Vector3 lastCellCenter = grid.GetCellCenterWorld(new Vector3Int(mapConfig.mapSize.x - 1, mapConfig.mapSize.y - 1, 0));

            // 2. The center of the entire map is the midpoint between these two points.
            Vector3 mapWorldCenter = (firstCellCenter + lastCellCenter) / 2f;

            // 3. The total size of the map is the distance between the centers, PLUS one full cell size
            //    to account for the outer halves of the edge tiles.
            Vector3 mapWorldSize = (lastCellCenter - firstCellCenter) + grid.cellSize;
            // Ensure size is always positive
            mapWorldSize.x = Mathf.Abs(mapWorldSize.x);
            mapWorldSize.y = Mathf.Abs(mapWorldSize.y);
            
            // Draw map bounds
            Gizmos.color = boundsColor;
            Gizmos.DrawWireCube(mapWorldCenter, mapWorldSize);

            // Draw safe area (with padding in world units)
            Gizmos.color = safeAreaColor;
            float padding = grid.cellSize.x * 2; // 2 tiles padding
            Vector3 safeSize = mapWorldSize - new Vector3(padding * 2, padding * 2, 0);
            if (safeSize.x > 0 && safeSize.y > 0)
            {
                Gizmos.DrawWireCube(mapWorldCenter, safeSize);
            }

            // Draw current camera view
            if (showCameraArea && Camera.main != null)
            {
                Gizmos.color = Color.blue;
                Camera cam = Camera.main;
                float height = cam.orthographicSize * 2;
                float width = height * cam.aspect;
                Gizmos.DrawWireCube(cam.transform.position, new Vector3(width, height, 0));
            }
        }
    }
}