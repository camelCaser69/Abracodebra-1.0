using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;
using Abracodabra.Genes;
using Abracodabra.UI.Genes;
using Abracodabra.UI.Toolkit;

public enum PlayerActionType
{
    Move,
    UseTool,
    PlantSeed,
    Harvest,
    Interact
}

/// <summary>
/// Manages player actions in the game world.
/// 
/// UPDATED: Now uses InventoryService for harvesting instead of old InventoryGridController.
/// This integrates with the UI Toolkit inventory system.
/// </summary>
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

    /// <summary>
    /// Execute a player action at the specified grid position
    /// </summary>
    public bool ExecutePlayerAction(PlayerActionType actionType, Vector3Int gridPosition, object actionData = null, Action onSuccessCallback = null)
    {
        if (debugMode) Debug.Log($"[PlayerActionManager] Executing {actionType} at {gridPosition}");

        bool success = false;
        int tickCost = tickCostPerAction;
        object eventPayload = actionData;

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

    /// <summary>
    /// Execute harvest action - NOW uses InventoryService instead of old InventoryGridController
    /// </summary>
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

        // Use NEW InventoryService instead of old InventoryGridController
        if (InventoryService.IsInitialized)
        {
            foreach (var harvestedItem in harvestedItems)
            {
                // Add harvested item using new service
                if (InventoryService.AddHarvestedItem(harvestedItem.Item))
                {
                    itemsAdded++;
                    if (debugMode) Debug.Log($"[PlayerActionManager] Added '{harvestedItem.Item.definition.itemName}' to inventory via InventoryService");
                }
                else
                {
                    if (debugMode) Debug.LogWarning($"[PlayerActionManager] Failed to add harvested item '{harvestedItem.Item.definition.itemName}' to inventory. Inventory may be full.");
                }
            }
        }
        else
        {
            // Fallback to legacy system if new system not initialized
            Debug.LogWarning("[PlayerActionManager] InventoryService not initialized, trying legacy InventoryGridController");
            
            if (InventoryGridController.Instance != null)
            {
                foreach (var harvestedItem in harvestedItems)
                {
                    var inventoryItem = InventoryBarItem.FromItem(harvestedItem.Item);
                    if (InventoryGridController.Instance.AddItemToInventory(inventoryItem))
                    {
                        itemsAdded++;
                    }
                }
            }
            else
            {
                Debug.LogError("[PlayerActionManager] Neither InventoryService nor InventoryGridController available!");
            }
        }

        if (debugMode) Debug.Log($"[PlayerActionManager] Successfully added {itemsAdded}/{harvestedItems.Count} harvested items to inventory.");

        return itemsAdded > 0;
    }

    private bool ExecuteInteraction(Vector3Int gridPosition, object interactionData)
    {
        if (debugMode) Debug.Log($"[PlayerActionManager] Interaction at {gridPosition}: {interactionData}");
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
            AdvanceGameTick(1);
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