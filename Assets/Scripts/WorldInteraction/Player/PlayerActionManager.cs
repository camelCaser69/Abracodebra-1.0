// Assets\Scripts\WorldInteraction\Player\PlayerActionManager.cs

using System;
using UnityEngine;
using WegoSystem;

public enum PlayerActionType {
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

    [SerializeField] bool debugMode = true;
    [SerializeField] int tickCostPerAction = 1;

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
            AdvanceGameTick(tickCost);
            OnActionExecuted?.Invoke(actionType, true);
        }
        else
        {
            OnActionFailed?.Invoke($"{actionType} failed");
        }

        return success;
    }

    bool ValidateMovement(GridPosition from, GridPosition to)
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

    bool ExecuteImmediateMove(GardenerController gardener, GridPosition to)
    {
        var gridEntity = gardener.GetComponent<GridEntity>();
        if (gridEntity == null) return false;

        gridEntity.SetPosition(to);
        return true;
    }

    public int GetMovementTickCost(Vector3 worldPosition, Component movingEntity = null) {
        int baseCost = tickCostPerAction;
        int maxAdditionalCost = 0; // Find the highest penalty if zones overlap
    
        // Determine the precise position to check, using the ground point if available.
        Vector3 positionToCheck = worldPosition;
        if (movingEntity != null)
        {
            GridEntity gridEntity = movingEntity.GetComponent<GridEntity>();
            if (gridEntity != null)
            {
                positionToCheck = gridEntity.GroundWorldPosition;
            }
        }
    
        SlowdownZone[] allZones = FindObjectsByType<SlowdownZone>(FindObjectsSortMode.None);
    
        foreach (var zone in allZones) {
            // Use the calculated positionToCheck instead of the raw worldPosition
            if (zone.IsPositionInZone(positionToCheck)) {
                bool shouldAffect = false;
                if (movingEntity == null) // If entity not specified, assume it affects by default 
                {
                    shouldAffect = true;
                }
                else if (movingEntity is GardenerController && zone.AffectsPlayer) {
                    shouldAffect = true;
                }
                else if (movingEntity is AnimalController && zone.AffectsAnimals) {
                    shouldAffect = true;
                }
    
                if (shouldAffect) {
                    maxAdditionalCost = Mathf.Max(maxAdditionalCost, zone.GetAdditionalTickCost());
                }
            }
        }
    
        if (maxAdditionalCost > 0 && debugMode) {
            string entityName = movingEntity != null ? movingEntity.gameObject.name : "Unknown Entity";
            Debug.Log($"[PlayerActionManager] Movement for '{entityName}' from {positionToCheck} is in a slowdown zone. Additional cost: {maxAdditionalCost}. Total cost: {baseCost + maxAdditionalCost} ticks");
        }
    
        return baseCost + maxAdditionalCost;
    }

    bool ExecuteToolUse(Vector3Int gridPosition, ToolDefinition tool)
    {
        if (tool == null) return false;

        TileInteractionManager.Instance?.ApplyToolAction(tool);
        return true;
    }

    bool ExecutePlantSeed(Vector3Int gridPosition, InventoryBarItem seedItem)
    {
        if (seedItem == null || !seedItem.IsSeed()) return false;

        return PlantPlacementManager.Instance?.TryPlantSeedFromInventory(
            seedItem, gridPosition, TileInteractionManager.Instance.interactionGrid.GetCellCenterWorld(gridPosition)
        ) ?? false;
    }

    bool ExecuteWatering(Vector3Int gridPosition)
    {
        if (debugMode) Debug.Log($"[PlayerActionManager] Watering at {gridPosition} - NOT IMPLEMENTED");
        return false;
    }

    bool ExecuteHarvest(Vector3Int gridPosition)
    {
        if (debugMode) Debug.Log($"[PlayerActionManager] Harvesting at {gridPosition} - NOT IMPLEMENTED");
        return false;
    }

    bool ExecuteInteraction(Vector3Int gridPosition, object interactionData)
    {
        if (debugMode) Debug.Log($"[PlayerActionManager] Interaction at {gridPosition}");
        return true;
    }

    // Replace the AdvanceGameTick method in PlayerActionManager.cs:

    void AdvanceGameTick(int tickCount = 1)
    {
        if (TickManager.Instance == null)
        {
            Debug.LogError("[PlayerActionManager] TickManager not found!");
            return;
        }
    
        // Process each tick individually to ensure proper updates
        for (int i = 0; i < tickCount; i++)
        {
            TickManager.Instance.AdvanceTick();
        
            if (debugMode && tickCount > 1)
            {
                Debug.Log($"[PlayerActionManager] Processing tick {i + 1}/{tickCount}");
            }
        }
    
        if (debugMode)
        {
            Debug.Log($"[PlayerActionManager] Advanced game by {tickCount} tick(s)");
        }
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