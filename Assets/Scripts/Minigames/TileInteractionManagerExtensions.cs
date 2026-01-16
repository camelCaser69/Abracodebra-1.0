using UnityEngine;
using WegoSystem;

namespace Abracodabra.Minigames {
    
    /// <summary>
    /// Extension methods to add minigame support to existing systems.
    /// </summary>
    public static class TileInteractionManagerExtensions {
        
        /// <summary>
        /// Apply a tool effect at a specific grid position (without requiring hover state).
        /// Used by minigame rewards to apply effects like watering.
        /// </summary>
        public static bool ApplyToolAtPosition(this TileInteractionManager manager, ToolDefinition toolDef, Vector3Int gridPosition) {
            if (manager == null || toolDef == null) return false;

            // Check if position is blocked by multi-tile entity
            GridPosition gridPos = new GridPosition(gridPosition);
            if (GridPositionManager.Instance != null) {
                var multiTileEntity = GridPositionManager.Instance.GetMultiTileEntityAt(gridPos);
                if (multiTileEntity != null && multiTileEntity.BlocksToolUsage) {
                    Debug.Log($"[TileInteractionManagerExt] Tool action blocked at {gridPosition} by '{multiTileEntity.gameObject.name}'");
                    return false;
                }
            }

            // Get tile at position
            TileDefinition topTile = manager.FindWhichTileDefinitionAt(gridPosition);
            if (topTile == null) {
                Debug.Log($"[TileInteractionManagerExt] No tile at {gridPosition}");
                return false;
            }

            // Find matching rule
            var library = manager.interactionLibrary;
            if (library == null || library.rules == null) {
                Debug.LogWarning("[TileInteractionManagerExt] No interaction library configured");
                return false;
            }

            TileInteractionRule matchingRule = null;
            foreach (var rule in library.rules) {
                if (rule != null && rule.tool == toolDef && rule.fromTile == topTile) {
                    matchingRule = rule;
                    break;
                }
            }

            if (matchingRule == null) {
                Debug.Log($"[TileInteractionManagerExt] No rule for '{toolDef.displayName}' on '{topTile.displayName}'");
                return false;
            }

            // Apply transformation using PUBLIC methods on TileInteractionManager
            if (matchingRule.toTile == null) {
                // Remove tile
                manager.RemoveTile(matchingRule.fromTile, gridPosition);
            } else {
                if (!matchingRule.toTile.keepBottomTile) {
                    manager.RemoveTile(matchingRule.fromTile, gridPosition);
                }
                manager.PlaceTile(matchingRule.toTile, gridPosition);
            }
            
            Debug.Log($"[TileInteractionManagerExt] Applied '{toolDef.displayName}' at {gridPosition}: '{matchingRule.fromTile.displayName}' -> '{matchingRule.toTile?.displayName ?? "REMOVE"}'");
            
            return true;
        }
    }
}