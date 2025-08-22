using UnityEngine;
using WegoSystem;

namespace WegoSystem
{
    public static class PixelGridSnapper
    {
        private static MapConfiguration cachedConfig;
        private static float cachedPixelSize;
        private static bool isInitialized = false;

        /// <summary>
        /// Initializes the snapper with the game's map configuration.
        /// This should be called once by a manager at the start of the game.
        /// </summary>
        public static void Initialize(MapConfiguration config)
        {
            if (isInitialized) return;

            cachedConfig = config;
            if (cachedConfig != null)
            {
                cachedPixelSize = 1f / cachedConfig.pixelsPerUnit;
                isInitialized = true;
                Debug.Log("[PixelGridSnapper] Initialized successfully.");
            }
            else
            {
                Debug.LogError("[PixelGridSnapper] Initialization failed: Provided MapConfiguration was null. Snapping will be disabled.");
            }
        }

        public static Vector3 SnapToGrid(Vector3 position)
        {
            if (!isInitialized) return position;

            position.x = Mathf.Round(position.x / cachedPixelSize) * cachedPixelSize;
            position.y = Mathf.Round(position.y / cachedPixelSize) * cachedPixelSize;
            return position;
        }

        public static Vector2 SnapToGrid(Vector2 position)
        {
            Vector3 snapped = SnapToGrid((Vector3)position);
            return new Vector2(snapped.x, snapped.y);
        }
    }
}