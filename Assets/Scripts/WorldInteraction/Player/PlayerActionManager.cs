using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
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

        // Note: The specific check for HarvestPouch is now handled in PlayerTileInteractor
        // to allow for more nuanced action calls.

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
                tickCost = 2; // Planting takes longer
                var seedItem = actionData as InventoryBarItem;
                success = ExecutePlantSeed(gridPosition, seedItem);
                break;

            case PlayerActionType.Harvest:
                // FIX: Use the new, functional harvest logic.
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

    // Only providing the changed method block as requested.
    // Only providing the changed method block as requested.
    private bool ExecuteHarvest(Vector3Int gridPosition)
    {
        var entities = GridPositionManager.Instance?.GetEntitiesAt(new GridPosition(gridPosition));
        var plantEntity = entities?.FirstOrDefault(e => e.GetComponent<PlantGrowth>() != null);

        if (plantEntity == null)
        {
            if (debugMode) Debug.Log($"[PlayerActionManager] Harvest failed: No plant found at {gridPosition}");
            return false;
        }

        var plant = plantEntity.GetComponent<PlantGrowth>();
        if (plant == null) return false;

        List<HarvestedItem> harvestedItems = plant.HarvestAllFruits();

        if (harvestedItems == null || harvestedItems.Count == 0)
        {
            if (debugMode) Debug.Log($"[PlayerActionManager] Harvest action on plant '{plant.name}' yielded no items.");
            return false;
        }

        int itemsAdded = 0;
        foreach (var harvestedItem in harvestedItems)
        {
            // NEW: Create the inventory item directly from the ItemInstance within the HarvestedItem.
            var inventoryItem = InventoryBarItem.FromItem(harvestedItem.Item);
            if (InventoryGridController.Instance.AddItemToInventory(inventoryItem))
            {
                itemsAdded++;
            }
            else
            {
                if (debugMode) Debug.LogWarning($"[PlayerActionManager] Failed to add harvested item '{harvestedItem.Item.definition.itemName}' to inventory. Inventory may be full.");
            }
        }

        if (debugMode) Debug.Log($"[PlayerActionManager] Successfully added {itemsAdded}/{harvestedItems.Count} harvested items to inventory.");
    
        return itemsAdded > 0;
    }

    private bool ExecuteInteraction(Vector3Int gridPosition, object interactionData)
    {
        if (debugMode) Debug.Log($"[PlayerActionManager] Interaction at {gridPosition}");
        return true;
    }

    private IEnumerator ExecuteDelayedAction(Func<bool> action, int tickCost, Action onSuccessCallback, PlayerActionType actionType, object actionData)
    {
        for (int i = 0; i < tickCost - 1; i++)
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