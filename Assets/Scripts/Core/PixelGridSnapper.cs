using UnityEngine;
using WegoSystem;

namespace WegoSystem
{
    public static class PixelGridSnapper
    {
        private static float cachedPixelSize;
        private static bool pixelSizeIsCached = false;

        public static Vector3 SnapToGrid(Vector3 position)
        {
            // The snapper is now dynamic and always gets the latest PPU from the manager.
            if (ResolutionManager.HasInstance)
            {
                if (!pixelSizeIsCached) // Cache it once for performance
                {
                    cachedPixelSize = 1f / ResolutionManager.Instance.CurrentPPU;
                    pixelSizeIsCached = true;
                }
                
                position.x = Mathf.Round(position.x / cachedPixelSize) * cachedPixelSize;
                position.y = Mathf.Round(position.y / cachedPixelSize) * cachedPixelSize;
                return position;
            }

            // Fallback if the manager doesn't exist yet
            return position;
        }

        public static Vector2 SnapToGrid(Vector2 position)
        {
            Vector3 snapped = SnapToGrid((Vector3)position);
            return new Vector2(snapped.x, snapped.y);
        }
    }
}