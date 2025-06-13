using UnityEngine;
using System.Collections.Generic;
using WegoSystem;

namespace WegoSystem {
    public static class GridRadiusUtility {
        /// <summary>
        /// Gets all grid positions within a radius using circle approximation (like Minecraft circles)
        /// </summary>
        public static List<GridPosition> GetTilesInCircle(GridPosition center, int radius, bool filled = true) {
            var result = new List<GridPosition>();
            
            if (radius <= 0) {
                result.Add(center);
                return result;
            }
            
            // Use the midpoint circle algorithm concept but adapted for filled/unfilled circles
            for (int dx = -radius; dx <= radius; dx++) {
                for (int dy = -radius; dy <= radius; dy++) {
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (filled) {
                        // Include tile if its center is within the radius
                        if (distance <= radius + 0.5f) {
                            result.Add(new GridPosition(center.x + dx, center.y + dy));
                        }
                    } else {
                        // For outline only, include tiles on the edge
                        if (distance >= radius - 0.5f && distance <= radius + 0.5f) {
                            result.Add(new GridPosition(center.x + dx, center.y + dy));
                        }
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets a more accurate circle using octant symmetry (best for visual display)
        /// </summary>
        public static List<GridPosition> GetPerfectCircleTiles(GridPosition center, int radius) {
            var result = new HashSet<GridPosition>();
            
            // Special case for radius 0
            if (radius == 0) {
                result.Add(center);
                return new List<GridPosition>(result);
            }
            
            // Use Bresenham's circle algorithm with octant symmetry
            int x = 0;
            int y = radius;
            int d = 3 - 2 * radius;
            
            while (x <= y) {
                // Add all 8 octants
                AddCirclePoints(result, center, x, y);
                
                if (d < 0) {
                    d = d + 4 * x + 6;
                } else {
                    d = d + 4 * (x - y) + 10;
                    y--;
                }
                x++;
            }
            
            // Fill the circle
            var filledResult = new List<GridPosition>();
            int minX = center.x - radius;
            int maxX = center.x + radius;
            
            for (int scanY = center.y - radius; scanY <= center.y + radius; scanY++) {
                bool inside = false;
                int startX = minX;
                
                // Find the leftmost and rightmost points on this scanline
                for (int scanX = minX; scanX <= maxX; scanX++) {
                    var pos = new GridPosition(scanX, scanY);
                    if (result.Contains(pos)) {
                        if (!inside) {
                            inside = true;
                            startX = scanX;
                        }
                    } else if (inside) {
                        // Fill from startX to scanX-1
                        for (int fillX = startX; fillX < scanX; fillX++) {
                            filledResult.Add(new GridPosition(fillX, scanY));
                        }
                        inside = false;
                    }
                }
                
                // Handle case where we're still inside at the end
                if (inside) {
                    for (int fillX = startX; fillX <= maxX; fillX++) {
                        var pos = new GridPosition(fillX, scanY);
                        if (result.Contains(pos) || fillX == maxX) {
                            for (int fill = startX; fill <= fillX; fill++) {
                                filledResult.Add(new GridPosition(fill, scanY));
                            }
                            break;
                        }
                    }
                }
            }
            
            return filledResult;
        }
        
        private static void AddCirclePoints(HashSet<GridPosition> result, GridPosition center, int x, int y) {
            result.Add(new GridPosition(center.x + x, center.y + y));
            result.Add(new GridPosition(center.x - x, center.y + y));
            result.Add(new GridPosition(center.x + x, center.y - y));
            result.Add(new GridPosition(center.x - x, center.y - y));
            result.Add(new GridPosition(center.x + y, center.y + x));
            result.Add(new GridPosition(center.x - y, center.y + x));
            result.Add(new GridPosition(center.x + y, center.y - x));
            result.Add(new GridPosition(center.x - y, center.y - x));
        }
        
        /// <summary>
        /// Checks if a position is within a circular radius of center
        /// </summary>
        public static bool IsWithinCircleRadius(GridPosition position, GridPosition center, int radius) {
            int dx = position.x - center.x;
            int dy = position.y - center.y;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            return distance <= radius + 0.5f;
        }
        
        /// <summary>
        /// Gets the border tiles of a circle (outline only)
        /// </summary>
        public static List<GridPosition> GetCircleOutline(GridPosition center, int radius) {
            return GetTilesInCircle(center, radius, false);
        }
        
        /// <summary>
        /// Visualizes the radius pattern in the console (for debugging)
        /// </summary>
        public static void DebugPrintRadius(GridPosition center, int radius) {
            var tiles = GetTilesInCircle(center, radius);
            var tileSet = new HashSet<GridPosition>(tiles);
            
            Debug.Log($"Circle pattern for radius {radius}:");
            
            for (int y = radius; y >= -radius; y--) {
                string line = "";
                for (int x = -radius; x <= radius; x++) {
                    var pos = new GridPosition(center.x + x, center.y + y);
                    if (pos.Equals(center)) {
                        line += "◉ "; // Center
                    } else if (tileSet.Contains(pos)) {
                        line += "● "; // Included tile
                    } else {
                        line += "· "; // Not included
                    }
                }
                Debug.Log(line);
            }
        }
    }
}