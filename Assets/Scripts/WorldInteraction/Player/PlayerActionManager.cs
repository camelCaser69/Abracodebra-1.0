using System;
using UnityEngine;
using WegoSystem;

public enum PlayerActionType
{
    Move,
    UseTool,
    PlantSeed,
    Harvest,
    Water,
    Interact
}

public class PlayerActionManager : MonoBehaviour
{
    public static PlayerActionManager Instance { get; set; }

    [SerializeField] private bool debugMode = true;
    [SerializeField] private int tickCostPerAction = 1;

    public event Action<PlayerActionType, bool> OnActionExecuted;
    public event Action<string> OnActionFailed;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    
    public bool ExecutePlayerAction(PlayerActionType actionType, Vector3Int gridPosition, object actionData = null)
    {
        if (debugMode) Debug.Log($"[PlayerActionManager] Executing {actionType} at {gridPosition}");

        bool success = false;
        int tickCost = tickCostPerAction;

        switch (actionType)
        {
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

        if (success)
        {
            // Only advance ticks if the action is successful
            AdvanceGameTick(tickCost);
            OnActionExecuted?.Invoke(actionType, true);
        }
        else
        {
            OnActionFailed?.Invoke($"{actionType} failed");
        }

        return success;
    }

    private bool ValidateMovement(GridPosition from, GridPosition to)
    {
        int distance = from.ManhattanDistance(to);
        if (distance != 1)
        {
            if (debugMode) Debug.Log($"[PlayerActionManager] Movement validation failed: distance {distance} != 1");
            return false;
        }

        if (!GridPositionManager.Instance.IsPositionValid(to))
        {
            if (debugMode) Debug.Log($"[PlayerActionManager] Movement validation failed: position {to} out of bounds");
            return false;
        }

        if (GridPositionManager.Instance.IsPositionOccupied(to))
        {
            if (debugMode) Debug.Log($"[PlayerActionManager] Movement validation failed: position {to} occupied");
            return false;
        }

        return true;
    }

    private bool ExecuteImmediateMove(GardenerController gardener, GridPosition to)
    {
        var gridEntity = gardener.GetComponent<GridEntity>();
        if (gridEntity == null) return false;

        gridEntity.SetPosition(to);
        return true;
    }

    public int GetMovementTickCost(Vector3 worldPosition)
    {
        int baseCost = tickCostPerAction;

        SlowdownZone[] allZones = FindObjectsByType<SlowdownZone>(FindObjectsSortMode.None);

        foreach (var zone in allZones)
        {
            if (zone.IsPositionInZone(worldPosition))
            {
                baseCost += zone.GetAdditionalTickCost();
                if (debugMode)
                {
                    Debug.Log($"[PlayerActionManager] Movement to {worldPosition} is in slowdown zone, costs {baseCost} ticks");
                }
                break;
            }
        }

        return baseCost;
    }

    private bool ExecuteToolUse(Vector3Int gridPosition, ToolDefinition tool)
    {
        if (tool == null) return false;

        TileInteractionManager.Instance?.ApplyToolAction(tool);
        return true;
    }

    private bool ExecutePlantSeed(Vector3Int gridPosition, InventoryBarItem seedItem)
    {
        if (seedItem == null || !seedItem.IsSeed()) return false;

        return PlantPlacementManager.Instance?.TryPlantSeedFromInventory(
            seedItem, gridPosition, TileInteractionManager.Instance.interactionGrid.GetCellCenterWorld(gridPosition)
        ) ?? false;
    }

    private bool ExecuteWatering(Vector3Int gridPosition)
    {
        if (debugMode) Debug.Log($"[PlayerActionManager] Watering at {gridPosition} - NOT IMPLEMENTED");
        return false;
    }

    private bool ExecuteHarvest(Vector3Int gridPosition)
    {
        if (debugMode) Debug.Log($"[PlayerActionManager] Harvesting at {gridPosition} - NOT IMPLEMENTED");
        return false;
    }

    private bool ExecuteInteraction(Vector3Int gridPosition, object interactionData)
    {
        if (debugMode) Debug.Log($"[PlayerActionManager] Interaction at {gridPosition}");
        return true;
    }

    private void AdvanceGameTick(int tickCount = 1)
    {
        if (TickManager.Instance == null)
        {
            Debug.LogError("[PlayerActionManager] TickManager not found!");
            return;
        }

        if (debugMode) Debug.Log($"[PlayerActionManager] Advancing {tickCount} tick(s) after player action");

        TickManager.Instance.AdvanceMultipleTicks(tickCount);
    }

    public bool CanExecuteAction(PlayerActionType actionType, Vector3Int gridPosition, object actionData = null)
    {
        switch (actionType)
        {
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