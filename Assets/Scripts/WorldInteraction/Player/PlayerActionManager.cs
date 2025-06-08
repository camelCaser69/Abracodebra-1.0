// Assets\Scripts\WorldInteraction\Player\PlayerActionManager.cs

using UnityEngine;
using WegoSystem;
using System;

public enum PlayerActionType {
    Move,
    UseTool,
    PlantSeed,
    Harvest,
    Water,
    Interact
}

public class PlayerActionManager : MonoBehaviour {
    public static PlayerActionManager Instance { get; private set; }

    [SerializeField] bool debugMode = true;
    [SerializeField] int tickCostPerAction = 1; // Most actions cost 1 tick
    
    // Events for UI/feedback
    public event Action<PlayerActionType, bool> OnActionExecuted;
    public event Action<string> OnActionFailed;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    // Core movement action - only advances tick if successful
    public bool ExecutePlayerMove(GardenerController gardener, GridPosition from, GridPosition to) {
        if (gardener == null) {
            OnActionFailed?.Invoke("No gardener controller");
            return false;
        }

        // Validate movement
        if (!ValidateMovement(from, to)) {
            OnActionFailed?.Invoke("Invalid movement");
            return false;
        }

        // Check if we're in planning phase (if using turn system)
        if (TurnPhaseManager.Instance?.IsInPlanningPhase == true) {
            // Queue the move but don't advance tick yet
            gardener.QueueMovement(to);
            if (debugMode) Debug.Log($"[PlayerActionManager] Queued move to {to}");
            OnActionExecuted?.Invoke(PlayerActionType.Move, true);
            return true;
        }

        // Execute immediate movement (for simplified system)
        if (ExecuteImmediateMove(gardener, to)) {
            // CRITICAL: Only advance tick on successful action
            AdvanceGameTick(tickCostPerAction);
            OnActionExecuted?.Invoke(PlayerActionType.Move, true);
            return true;
        }

        OnActionFailed?.Invoke("Movement blocked");
        return false;
    }

    // Tool/Seed usage action
    public bool ExecutePlayerAction(PlayerActionType actionType, Vector3Int gridPosition, object actionData = null) {
        if (debugMode) Debug.Log($"[PlayerActionManager] Executing {actionType} at {gridPosition}");

        bool success = false;
        int tickCost = tickCostPerAction;

        switch (actionType) {
            case PlayerActionType.UseTool:
                success = ExecuteToolUse(gridPosition, actionData as ToolDefinition);
                break;
                
            case PlayerActionType.PlantSeed:
                success = ExecutePlantSeed(gridPosition, actionData as InventoryBarItem);
                // Planting might cost more ticks
                tickCost = 2;
                break;
                
            case PlayerActionType.Water:
                success = ExecuteWatering(gridPosition);
                break;
                
            case PlayerActionType.Harvest:
                success = ExecuteHarvest(gridPosition);
                break;
                
            case PlayerActionType.Interact:
                success = ExecuteInteraction(gridPosition, actionData);
                break;
        }

        if (success) {
            // CRITICAL: Only advance tick on successful action
            AdvanceGameTick(tickCost);
            OnActionExecuted?.Invoke(actionType, true);
        } else {
            OnActionFailed?.Invoke($"{actionType} failed");
        }

        return success;
    }

    // Validation methods
    bool ValidateMovement(GridPosition from, GridPosition to) {
        // Must be adjacent
        int distance = from.ManhattanDistance(to);
        if (distance != 1) {
            if (debugMode) Debug.Log($"[PlayerActionManager] Movement validation failed: distance {distance} != 1");
            return false;
        }

        // Check bounds
        if (!GridPositionManager.Instance.IsPositionValid(to)) {
            if (debugMode) Debug.Log($"[PlayerActionManager] Movement validation failed: position {to} out of bounds");
            return false;
        }

        // Check occupancy
        if (GridPositionManager.Instance.IsPositionOccupied(to)) {
            if (debugMode) Debug.Log($"[PlayerActionManager] Movement validation failed: position {to} occupied");
            return false;
        }

        return true;
    }

    bool ExecuteImmediateMove(GardenerController gardener, GridPosition to) {
        var gridEntity = gardener.GetComponent<GridEntity>();
        if (gridEntity == null) return false;

        gridEntity.SetPosition(to);
        return true;
    }

    bool ExecuteToolUse(Vector3Int gridPosition, ToolDefinition tool) {
        if (tool == null) return false;

        // Route through existing tile interaction system
        TileInteractionManager.Instance?.ApplyToolAction(tool);
        return true;
    }

    bool ExecutePlantSeed(Vector3Int gridPosition, InventoryBarItem seedItem) {
        if (seedItem == null || !seedItem.IsSeed()) return false;

        // Use existing plant placement system
        return PlantPlacementManager.Instance?.TryPlantSeedFromInventory(
            seedItem, gridPosition, TileInteractionManager.Instance.interactionGrid.GetCellCenterWorld(gridPosition)
        ) ?? false;
    }

    bool ExecuteWatering(Vector3Int gridPosition) {
        // TODO: Implement watering logic
        if (debugMode) Debug.Log($"[PlayerActionManager] Watering at {gridPosition} - NOT IMPLEMENTED");
        return false;
    }

    bool ExecuteHarvest(Vector3Int gridPosition) {
        // TODO: Implement harvest logic
        if (debugMode) Debug.Log($"[PlayerActionManager] Harvesting at {gridPosition} - NOT IMPLEMENTED");
        return false;
    }

    bool ExecuteInteraction(Vector3Int gridPosition, object interactionData) {
        // Generic interaction point for future expansion
        if (debugMode) Debug.Log($"[PlayerActionManager] Interaction at {gridPosition}");
        return true;
    }

    // Central tick advancement - ALL player actions go through here
    void AdvanceGameTick(int tickCount = 1) {
        if (TickManager.Instance == null) {
            Debug.LogError("[PlayerActionManager] TickManager not found!");
            return;
        }

        if (debugMode) Debug.Log($"[PlayerActionManager] Advancing {tickCount} tick(s) after player action");
        
        // This is the ONLY place outside debug that should advance ticks
        TickManager.Instance.AdvanceMultipleTicks(tickCount);
    }

    // Public method to check if an action would be valid without executing
    public bool CanExecuteAction(PlayerActionType actionType, Vector3Int gridPosition, object actionData = null) {
        switch (actionType) {
            case PlayerActionType.Move:
                // Special case for movement validation
                return false; // Use ValidateMovement instead
                
            case PlayerActionType.UseTool:
                return actionData is ToolDefinition;
                
            case PlayerActionType.PlantSeed:
                var seedItem = actionData as InventoryBarItem;
                return seedItem != null && seedItem.IsSeed();
                
            default:
                return true;
        }
    }
}