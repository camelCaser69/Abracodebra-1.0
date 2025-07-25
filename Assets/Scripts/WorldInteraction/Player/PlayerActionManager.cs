using System;
using System.Collections;
using System.Linq;
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

    public class ToolActionData
    {
        public ToolDefinition Tool;
        public Vector3Int GridPosition;
    }

    [SerializeField] private bool debugMode = true;
    [SerializeField] private int tickCostPerAction = 1;
    [SerializeField] private float multiTickActionDelay = 0.5f;

    public event Action<PlayerActionType, object> OnActionExecuted;
    public event Action<string> OnActionFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool ExecutePlayerAction(PlayerActionType actionType, Vector3Int gridPosition, object actionData = null, Action onSuccessCallback = null)
    {
        if (debugMode) Debug.Log($"[PlayerActionManager] Executing {actionType} at {gridPosition}");

        bool success = false;
        int tickCost = tickCostPerAction; // Declare here so it's accessible throughout the method
        object eventPayload = actionData;

        // Re-route the action if the tool is the Harvest Pouch before the switch
        var toolDefForCheck = actionData as ToolDefinition;
        if (actionType == PlayerActionType.UseTool && toolDefForCheck != null && toolDefForCheck.toolType == ToolType.HarvestPouch)
        {
            actionType = PlayerActionType.Harvest;
        }

        switch (actionType)
        {
            case PlayerActionType.UseTool:
                var toolDef = actionData as ToolDefinition;
                success = ExecuteToolUse(gridPosition, toolDef);
                if (success)
                {
                    eventPayload = new ToolActionData { Tool = toolDef, GridPosition = gridPosition };
                }
                break;

            case PlayerActionType.PlantSeed:
                tickCost = 2; // Override default tick cost for this specific action
                if (tickCost > 1)
                {
                    StartCoroutine(ExecuteDelayedAction(() => ExecutePlantSeed(gridPosition, actionData as InventoryBarItem), tickCost, onSuccessCallback, actionType, actionData));
                    return true; // Return early, coroutine will handle the rest
                }
                else
                {
                    success = ExecutePlantSeed(gridPosition, actionData as InventoryBarItem);
                }
                break;

            case PlayerActionType.Harvest:
                success = ExecuteHarvest(gridPosition);
                break;

            case PlayerActionType.Water:
                success = ExecuteWatering(gridPosition);
                break;

            case PlayerActionType.Interact:
                success = ExecuteInteraction(gridPosition, actionData);
                break;
        }

        if (success)
        {
            AdvanceGameTick(tickCost);
            onSuccessCallback?.Invoke();
            OnActionExecuted?.Invoke(actionType, eventPayload);
        }
        else
        {
            OnActionFailed?.Invoke($"{actionType} failed");
        }
        return success;
    }

    private bool ExecuteHarvest(Vector3Int gridPosition)
    {
        var plantEntity = GridPositionManager.Instance?.GetEntitiesAt(new GridPosition(gridPosition))
            .FirstOrDefault(e => e.GetComponent<PlantGrowth>() != null);

        if (plantEntity == null)
        {
            if (debugMode) Debug.Log($"Harvest failed: No plant found at {gridPosition}");
            return false;
        }

        var plant = plantEntity.GetComponent<PlantGrowth>();
        if (plant == null) return false;

        var harvestedDefs = plant.Harvest();

        if (harvestedDefs.Count > 0)
        {
            foreach (var definition in harvestedDefs)
            {
                InventoryGridController.Instance.AddGeneToInventoryFromDefinition(definition);
            }
            return true;
        }

        if (debugMode) Debug.Log($"Harvest action at {gridPosition}, but nothing was harvested.");
        return false;
    }

    private IEnumerator ExecuteDelayedAction(Func<bool> action, int tickCost, Action onSuccessCallback, PlayerActionType actionType, object actionData)
    {
        for (int i = 0; i < tickCost - 1; i++)
        {
            TickManager.Instance.AdvanceTick();
            yield return new WaitForSeconds(multiTickActionDelay);
        }
        bool success = action.Invoke();
        TickManager.Instance.AdvanceTick();
        if (success)
        {
            onSuccessCallback?.Invoke();
            OnActionExecuted?.Invoke(actionType, actionData);
        }
        else
        {
            OnActionFailed?.Invoke("Delayed action failed");
        }
    }

    public int GetMovementTickCost(Vector3 worldPosition, Component movingEntity = null)
    {
        int totalCost = tickCostPerAction;
        int statusEffectCost = 0;
        if (movingEntity != null)
        {
            IStatusEffectable effectable = movingEntity.GetComponent<IStatusEffectable>();
            if (effectable != null)
            {
                statusEffectCost = effectable.StatusManager.AdditionalMoveTicks;
            }
        }

        totalCost += statusEffectCost;
        if (debugMode && totalCost > tickCostPerAction)
        {
            string entityName = movingEntity != null ? movingEntity.gameObject.name : "Unknown Entity";
            Debug.Log($"[PlayerActionManager] Movement for '{entityName}' cost breakdown: Base({tickCostPerAction}) + Status({statusEffectCost}) = {totalCost} ticks total.");
        }
        return totalCost;
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
        return PlantPlacementManager.Instance?.TryPlantSeedFromInventory(seedItem, gridPosition, TileInteractionManager.Instance.interactionGrid.GetCellCenterWorld(gridPosition)) ?? false;
    }

    private bool ExecuteWatering(Vector3Int gridPosition)
    {
        if (debugMode) Debug.Log($"[PlayerActionManager] Watering at {gridPosition} - NOT IMPLEMENTED");
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
        for (int i = 0; i < tickCount; i++)
        {
            TickManager.Instance.AdvanceTick();
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