using UnityEngine;
using WegoSystem;

namespace WegoSystem
{
    /// <summary>
    /// A static helper class to snap world positions to the pixel grid defined
    /// by the global MapConfiguration.
    /// </summary>
    public static class PixelGridSnapper
    {
        private static MapConfiguration cachedConfig;
        private static float cachedPixelSize;
        private static bool isInitialized = false;
        
        private static void CacheConfig()
        {
            if (isInitialized) return;

            // In a built game, we assume the config is loaded. 
            // In the editor, Resources.Load is a reliable fallback.
            cachedConfig = Resources.Load<MapConfiguration>("MapConfiguration");
            if (cachedConfig != null)
            {
                cachedPixelSize = 1f / cachedConfig.pixelsPerUnit;
            }
            else
            {
                Debug.LogError("[PixelGridSnapper] Could not find 'MapConfiguration' in Resources. Snapping will be disabled.");
            }
            isInitialized = true;
        }

        /// <summary>
        /// Snaps a 3D vector position to the nearest pixel grid coordinate.
        /// </summary>
        /// <param name="position">The world position to snap.</param>
        /// <returns>The snapped world position.</returns>
        public static Vector3 SnapToGrid(Vector3 position)
        {
            CacheConfig();
            if (cachedConfig == null || cachedPixelSize <= 0) return position;

            position.x = Mathf.Round(position.x / cachedPixelSize) * cachedPixelSize;
            position.y = Mathf.Round(position.y / cachedPixelSize) * cachedPixelSize;
            // Z-axis is not snapped
            return position;
        }

        /// <summary>
        /// Snaps a 2D vector position to the nearest pixel grid coordinate.
        /// </summary>
        /// <param name="position">The world position to snap.</param>
        /// <returns>The snapped world position.</returns>
        public static Vector2 SnapToGrid(Vector2 position)
        {
            Vector3 snapped = SnapToGrid((Vector3)position);
            return new Vector2(snapped.x, snapped.y);
        }
    }
}