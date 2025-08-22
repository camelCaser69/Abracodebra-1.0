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
            if (mapConfig == null)
            {
                // Attempt to find it if not assigned
                if (GridPositionManager.Instance != null)
                {
                    // A common pattern would be to have the config on the manager
                    // This is just a guess to make it more user-friendly.
                }
                if (mapConfig == null) return;
            }

            // Draw map bounds
            Gizmos.color = boundsColor;
            Vector3 center = mapConfig.GetMapBounds().center;
            Vector3 size = mapConfig.GetMapBounds().size;
            Gizmos.DrawWireCube(center, size);

            // Draw safe area (using a 2-tile padding as an example)
            Gizmos.color = safeAreaColor;
            Vector3 safeSize = size - new Vector3(4, 4, 0); // 2 tiles padding on each side
            if (safeSize.x > 0 && safeSize.y > 0)
            {
                Gizmos.DrawWireCube(center, safeSize);
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