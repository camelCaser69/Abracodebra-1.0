// FILE: Assets/Scripts/WorldInteraction/Player/PlayerActionManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;
using Abracodabra.Genes;
using Abracodabra.UI.Genes;
using Abracodabra.UI.Toolkit;
using Abracodabra.Minigames;

public enum PlayerActionType {
    Move,
    UseTool,
    PlantSeed,
    Harvest,
    Interact
}

public class PlayerActionManager : MonoBehaviour {
    public static PlayerActionManager Instance { get; private set; }

    public class ToolActionData {
        public ToolDefinition Tool;
        public Vector3Int GridPosition;
    }

    [SerializeField] bool debugMode = true;
    [SerializeField] int tickCostPerAction = 1;
    [SerializeField] float multiTickActionDelay = 0.5f;

    public event Action<PlayerActionType, object> OnActionExecuted;
    public event Action<string> OnActionFailed;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool ExecutePlayerAction(PlayerActionType actionType, Vector3Int gridPosition, object actionData = null, Action onSuccessCallback = null) {
        if (debugMode) Debug.Log($"[PlayerActionManager] Executing {actionType} at {gridPosition}");

        bool success = false;
        int tickCost = tickCostPerAction;
        object eventPayload = actionData;

        switch (actionType) {
            case PlayerActionType.UseTool:
                var toolDef = actionData as ToolDefinition;
                success = ExecuteToolUse(gridPosition, toolDef);
                if (success) {
                    eventPayload = new ToolActionData { Tool = toolDef, GridPosition = gridPosition };
                }
                break;

            case PlayerActionType.PlantSeed:
                tickCost = 2; // Planting takes longer
                var seedItem = actionData as UIInventoryItem;
                success = ExecutePlantSeed(gridPosition, seedItem);
                break;

            case PlayerActionType.Harvest:
                success = ExecuteHarvest(gridPosition);
                break;

            case PlayerActionType.Interact:
                success = ExecuteInteraction(gridPosition, actionData);
                break;
        }

        if (success) {
            AdvanceGameTick(tickCost);
            onSuccessCallback?.Invoke();
            OnActionExecuted?.Invoke(actionType, eventPayload);
        }
        else {
            OnActionFailed?.Invoke($"{actionType} failed at {gridPosition}");
        }

        return success;
    }

    bool ExecuteToolUse(Vector3Int gridPosition, ToolDefinition tool) {
        if (tool == null) return false;

        bool actionSucceeded = TileInteractionManager.Instance?.ApplyToolAction(tool) ?? false;

        if (actionSucceeded) {
            if (ToolSwitcher.Instance != null && tool.limitedUses) {
                ToolSwitcher.Instance.TryConsumeUse();
            }

            if (debugMode) Debug.Log($"[PlayerActionManager] Tool '{tool.displayName}' action succeeded at {gridPosition}");
        }
        else {
            if (debugMode) Debug.Log($"[PlayerActionManager] Tool '{tool.displayName}' action had no effect at {gridPosition} - no use consumed");
        }

        return actionSucceeded;
    }

    bool ExecutePlantSeed(Vector3Int gridPosition, UIInventoryItem seedItem) {
        if (debugMode) Debug.Log($"[ExecutePlantSeed] Attempting to plant {seedItem?.GetDisplayName()} at {gridPosition}");

        if (seedItem == null || seedItem.Type != UIInventoryItem.ItemType.Seed) {
            Debug.LogError("[ExecutePlantSeed] Action failed: Provided item was not a valid seed.");
            return false;
        }

        Vector3 worldPosition = TileInteractionManager.Instance.interactionGrid.GetCellCenterWorld(gridPosition);

        bool result = PlantPlacementManager.Instance?.TryPlantSeedFromInventory(
            seedItem.SeedRuntimeState,
            gridPosition,
            worldPosition
        ) ?? false;

        if (debugMode) Debug.Log($"[ExecutePlantSeed] PlantPlacementManager returned: {result}");

        // If planting succeeded, trigger the minigame for bonus opportunity
        if (result) {
            TriggerPlantingMinigame(gridPosition, worldPosition);
        }

        return result;
    }

    /// <summary>
    /// Trigger the planting minigame for a chance at bonus effects (e.g., auto-watering)
    /// </summary>
    void TriggerPlantingMinigame(Vector3Int gridPosition, Vector3 worldPosition) {
        if (MinigameManager.Instance == null) {
            if (debugMode) Debug.Log("[PlayerActionManager] MinigameManager not found - skipping minigame");
            return;
        }

        // Check if planting minigames are enabled
        if (!MinigameManager.Instance.IsTriggerEnabled(MinigameTrigger.Planting)) {
            if (debugMode) Debug.Log("[PlayerActionManager] Planting minigame not enabled - skipping");
            return;
        }

        // Trigger the minigame - rewards are handled by MinigameManager
        bool triggered = MinigameManager.Instance.TryTriggerMinigame(
            MinigameTrigger.Planting,
            gridPosition,
            worldPosition,
            OnPlantingMinigameComplete
        );

        if (debugMode && triggered) {
            Debug.Log($"[PlayerActionManager] Planting minigame triggered at {gridPosition}");
        }
    }

    void OnPlantingMinigameComplete(MinigameResult result) {
        if (debugMode) {
            string resultText = result.Tier switch {
                MinigameResultTier.Perfect => "PERFECT! Plant auto-watered!",
                MinigameResultTier.Good => "Good! Plant auto-watered!",
                MinigameResultTier.Miss => "Missed - no bonus",
                MinigameResultTier.Skipped => "Skipped - no bonus",
                _ => "Unknown result"
            };
            Debug.Log($"[PlayerActionManager] Planting minigame result: {resultText}");
        }
    }

    bool ExecuteHarvest(Vector3Int gridPosition) {
        var entities = GridPositionManager.Instance?.GetEntitiesAt(new GridPosition(gridPosition));
        var plantEntity = entities?.FirstOrDefault(e => e.GetComponent<PlantGrowth>() != null);

        if (plantEntity == null) {
            if (debugMode) Debug.Log($"[PlayerActionManager] Harvest failed: No plant found at {gridPosition}");
            return false;
        }

        var plant = plantEntity.GetComponent<PlantGrowth>();
        if (plant == null) return false;

        List<HarvestedItem> harvestedItems = plant.HarvestAllFruits();

        if (harvestedItems == null || harvestedItems.Count == 0) {
            if (debugMode) Debug.Log($"[PlayerActionManager] Harvest action on plant '{plant.name}' yielded no items.");
            return false;
        }

        int itemsAdded = 0;

        if (InventoryService.IsInitialized) {
            foreach (var harvestedItem in harvestedItems) {
                if (InventoryService.AddHarvestedItem(harvestedItem.Item)) {
                    itemsAdded++;
                    if (debugMode) Debug.Log($"[PlayerActionManager] Added '{harvestedItem.Item.definition.itemName}' to inventory via InventoryService");
                }
                else {
                    if (debugMode) Debug.LogWarning($"[PlayerActionManager] Failed to add harvested item '{harvestedItem.Item.definition.itemName}' to inventory. Inventory may be full.");
                }
            }
        }
        else {
            Debug.LogError("[PlayerActionManager] InventoryService not initialized! Cannot add harvested items.");
        }

        if (debugMode) Debug.Log($"[PlayerActionManager] Successfully added {itemsAdded}/{harvestedItems.Count} harvested items to inventory.");

        return itemsAdded > 0;
    }

    bool ExecuteInteraction(Vector3Int gridPosition, object interactionData) {
        if (debugMode) Debug.Log($"[PlayerActionManager] Interaction at {gridPosition}: {interactionData}");
        return true;
    }

    IEnumerator ExecuteDelayedAction(Func<bool> action, int tickCost, Action onSuccessCallback, PlayerActionType actionType, object actionData) {
        for (int i = 0; i < tickCost - 1; i++) {
            yield return new WaitForSeconds(multiTickActionDelay);
            AdvanceGameTick(1);
        }

        bool success = action.Invoke();

        if (success) {
            AdvanceGameTick(1);
            onSuccessCallback?.Invoke();
            OnActionExecuted?.Invoke(actionType, actionData);
        }
        else {
            OnActionFailed?.Invoke($"{actionType} (delayed) failed");
        }
    }

    public int GetMovementTickCost(Vector3 worldPosition, Component movingEntity = null) {
        int totalCost = tickCostPerAction;
        if (movingEntity != null) {
            IStatusEffectable effectable = movingEntity.GetComponent<IStatusEffectable>();
            if (effectable != null) {
                totalCost += effectable.StatusManager.AdditionalMoveTicks;
            }
        }
        return totalCost;
    }

    void AdvanceGameTick(int tickCount) {
        if (TickManager.Instance != null) {
            for (int i = 0; i < tickCount; i++) {
                TickManager.Instance.AdvanceTick();
            }
        }
    }
}