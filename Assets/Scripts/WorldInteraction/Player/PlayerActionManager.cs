using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using WegoSystem;
using Abracodabra.Genes;
using Abracodabra.UI.Genes;

public enum PlayerActionType
{
    Move,
    UseTool,
    PlantSeed,
    Harvest,
    Interact
}

public class PlayerActionManager : MonoBehaviour
{
    public static PlayerActionManager Instance { get; private set; }

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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool ExecutePlayerAction(PlayerActionType actionType, Vector3Int gridPosition, object actionData = null, Action onSuccessCallback = null)
    {
        if (debugMode) Debug.Log($"[PlayerActionManager] Executing {actionType} at {gridPosition}");

        bool success = false;
        int tickCost = tickCostPerAction;
        object eventPayload = actionData;

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

            // FIX: This case has been restructured to be synchronous, following the analysis document.
            case PlayerActionType.PlantSeed:
                tickCost = 2; // Planting takes longer
                var seedItem = actionData as InventoryBarItem;
                success = ExecutePlantSeed(gridPosition, seedItem);
                // We no longer start a coroutine or return early. The standard success handling below will now work correctly.
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
            onSuccessCallback?.Invoke();
            OnActionExecuted?.Invoke(actionType, eventPayload);
        }
        else
        {
            OnActionFailed?.Invoke($"{actionType} failed at {gridPosition}");
        }
        return success;
    }

    private bool ExecuteToolUse(Vector3Int gridPosition, ToolDefinition tool)
    {
        if (tool == null) return false;
        TileInteractionManager.Instance?.ApplyToolAction(tool);
        return true;
    }

    // FIX: Added the recommended debug logs for better traceability.
    private bool ExecutePlantSeed(Vector3Int gridPosition, InventoryBarItem seedItem)
    {
        if (debugMode) Debug.Log($"[ExecutePlantSeed] Attempting to plant {seedItem?.GetDisplayName()} at {gridPosition}");

        if (seedItem == null || seedItem.Type != InventoryBarItem.ItemType.Seed) 
        {
            Debug.LogError("[ExecutePlantSeed] Action failed: Provided item was not a valid seed.");
            return false;
        }
    
        bool result = PlantPlacementManager.Instance?.TryPlantSeedFromInventory(
            seedItem.SeedRuntimeState, 
            gridPosition, 
            TileInteractionManager.Instance.interactionGrid.GetCellCenterWorld(gridPosition)
        ) ?? false;
    
        if (debugMode) Debug.Log($"[ExecutePlantSeed] PlantPlacementManager returned: {result}");
        return result;
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

        Debug.LogWarning("PlayerActionManager.ExecuteHarvest needs to be updated to handle returned items from PlantGrowth.");
        bool wasHarvested = true;

        return wasHarvested;
    }

    private bool ExecuteInteraction(Vector3Int gridPosition, object interactionData)
    {
        if (debugMode) Debug.Log($"[PlayerActionManager] Interaction at {gridPosition}");
        return true;
    }

    // This method is no longer used by the planting action, but is kept for other potential delayed actions.
    IEnumerator ExecuteDelayedAction(Func<bool> action, int tickCost, Action onSuccessCallback, PlayerActionType actionType, object actionData)
    {
        // This simulates the action taking time before the result is known.
        for (int i = 0; i < tickCost -1; i++)
        {
            yield return new WaitForSeconds(multiTickActionDelay);
            AdvanceGameTick(1);
        }

        bool success = action.Invoke();

        if (success)
        {
            AdvanceGameTick(1); // Final tick
            onSuccessCallback?.Invoke();
            OnActionExecuted?.Invoke(actionType, actionData);
        }
        else
        {
            OnActionFailed?.Invoke($"{actionType} (delayed) failed");
        }
    }

    public int GetMovementTickCost(Vector3 worldPosition, Component movingEntity = null)
    {
        int totalCost = tickCostPerAction;
        if (movingEntity != null)
        {
            IStatusEffectable effectable = movingEntity.GetComponent<IStatusEffectable>();
            if (effectable != null)
            {
                totalCost += effectable.StatusManager.AdditionalMoveTicks;
            }
        }
        return totalCost;
    }

    private void AdvanceGameTick(int tickCount = 1)
    {
        if (TickManager.Instance == null) return;
        for (int i = 0; i < tickCount; i++)
        {
            TickManager.Instance.AdvanceTick();
        }
    }
}