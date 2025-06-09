using UnityEngine;
using System;
using System.Linq;
using WegoSystem;

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
    [SerializeField] int tickCostPerAction = 1;

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

    public bool ExecutePlayerMove(GardenerController gardener, GridPosition from, GridPosition to) {
        if (gardener == null) {
            OnActionFailed?.Invoke("No gardener controller");
            return false;
        }

        if (!ValidateMovement(from, to)) {
            OnActionFailed?.Invoke("Invalid movement");
            return false;
        }

        // During planning phase, just queue the move
        if (TurnPhaseManager.Instance?.IsInPlanningPhase == true) {
            gardener.QueueMovement(to);
            if (debugMode) Debug.Log($"[PlayerActionManager] Queued move from {from} to {to}");
            OnActionExecuted?.Invoke(PlayerActionType.Move, true);
            return true;
        }
        
        // During execution phase, moves are handled by GardenerController.OnTickUpdate
        // This method shouldn't be called during execution phase
        if (debugMode) Debug.Log("[PlayerActionManager] Move requested outside planning phase - ignored");
        return false;
    }

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
            // Only advance ticks if we're in execution phase
            // During planning phase, tick costs are applied when actions execute
            if (TurnPhaseManager.Instance?.CurrentPhase == TurnPhase.Execution) {
                AdvanceGameTick(tickCost);
            }
            OnActionExecuted?.Invoke(actionType, true);
        } else {
            OnActionFailed?.Invoke($"{actionType} failed");
        }

        return success;
    }

    bool ValidateMovement(GridPosition from, GridPosition to) {
        int distance = from.ManhattanDistance(to);
        if (distance != 1) {
            if (debugMode) Debug.Log($"[PlayerActionManager] Movement validation failed: distance {distance} != 1");
            return false;
        }

        if (!GridPositionManager.Instance.IsPositionValid(to)) {
            if (debugMode) Debug.Log($"[PlayerActionManager] Movement validation failed: position {to} out of bounds");
            return false;
        }

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

    public int GetMovementTickCost(Vector3 worldPosition) {
        int baseCost = tickCostPerAction;

        SlowdownZone[] allZones = FindObjectsByType<SlowdownZone>(FindObjectsSortMode.None);

        foreach (var zone in allZones) {
            if (zone.IsPositionInZone(worldPosition)) {
                baseCost += zone.GetAdditionalTickCost();
                if (debugMode) {
                    Debug.Log($"[PlayerActionManager] Movement to {worldPosition} is in slowdown zone, costs {baseCost} ticks");
                }
                break;
            }
        }

        return baseCost;
    }

    bool ExecuteToolUse(Vector3Int gridPosition, ToolDefinition tool) {
        if (tool == null) return false;

        TileInteractionManager.Instance?.ApplyToolAction(tool);
        return true;
    }

    bool ExecutePlantSeed(Vector3Int gridPosition, InventoryBarItem seedItem) {
        if (seedItem == null || !seedItem.IsSeed()) return false;

        return PlantPlacementManager.Instance?.TryPlantSeedFromInventory(
            seedItem, gridPosition, TileInteractionManager.Instance.interactionGrid.GetCellCenterWorld(gridPosition)
        ) ?? false;
    }

    bool ExecuteWatering(Vector3Int gridPosition) {
        if (debugMode) Debug.Log($"[PlayerActionManager] Watering at {gridPosition} - NOT IMPLEMENTED");
        return false;
    }

    bool ExecuteHarvest(Vector3Int gridPosition) {
        if (debugMode) Debug.Log($"[PlayerActionManager] Harvesting at {gridPosition} - NOT IMPLEMENTED");
        return false;
    }

    bool ExecuteInteraction(Vector3Int gridPosition, object interactionData) {
        if (debugMode) Debug.Log($"[PlayerActionManager] Interaction at {gridPosition}");
        return true;
    }

    void AdvanceGameTick(int tickCount = 1) {
        if (TickManager.Instance == null) {
            Debug.LogError("[PlayerActionManager] TickManager not found!");
            return;
        }

        if (debugMode) Debug.Log($"[PlayerActionManager] Advancing {tickCount} tick(s) after player action");

        TickManager.Instance.AdvanceMultipleTicks(tickCount);
    }

    public bool CanExecuteAction(PlayerActionType actionType, Vector3Int gridPosition, object actionData = null) {
        switch (actionType) {
            case PlayerActionType.Move:
                return false;

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