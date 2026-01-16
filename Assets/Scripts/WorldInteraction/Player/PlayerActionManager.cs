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
                var seedItem = actionData as UIInventoryItem;
                // Check if minigame will handle this
                if (ShouldUseMinigameForPlanting()) {
                    success = ExecutePlantSeedWithMinigame(gridPosition, seedItem, onSuccessCallback);
                    if (success) {
                        // Minigame started - it will handle tick advancement and seed consumption
                        // Return true but DON'T execute the normal success flow
                        return true;
                    }
                    // Minigame failed to start (validation failed) - fall through to normal flow
                }
                // No minigame or minigame failed - plant immediately
                success = ExecutePlantSeedImmediate(gridPosition, seedItem);
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

    bool ShouldUseMinigameForPlanting() {
        return MinigameManager.Instance != null && 
               MinigameManager.Instance.IsTriggerEnabled(MinigameTrigger.Planting);
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

    /// <summary>
    /// Execute planting with minigame - seed planted AFTER minigame completes
    /// </summary>
    bool ExecutePlantSeedWithMinigame(Vector3Int gridPosition, UIInventoryItem seedItem, Action onSuccessCallback) {
        if (debugMode) Debug.Log($"[ExecutePlantSeedWithMinigame] Starting for {seedItem?.GetDisplayName()} at {gridPosition}");

        if (seedItem == null || seedItem.Type != UIInventoryItem.ItemType.Seed) {
            Debug.LogError("[ExecutePlantSeedWithMinigame] Invalid seed item");
            return false;
        }

        // Validate that planting CAN happen
        if (!CanPlantAtPosition(gridPosition, seedItem)) {
            if (debugMode) Debug.Log("[ExecutePlantSeedWithMinigame] Planting validation failed");
            return false;
        }

        Vector3 worldPosition = TileInteractionManager.Instance.interactionGrid.GetCellCenterWorld(gridPosition);

        // Capture everything needed for deferred execution
        var capturedRuntimeState = seedItem.SeedRuntimeState;
        var capturedSeedItem = seedItem;
        var capturedGridPosition = gridPosition;
        var capturedWorldPosition = worldPosition;
        var capturedCallback = onSuccessCallback;
        var capturedTickCost = tickCostPerAction;

        // Define the deferred action that runs after minigame completes
        Action deferredPlantAction = () => {
            if (debugMode) Debug.Log($"[DeferredPlant] Executing deferred plant at {capturedGridPosition}");

            bool planted = false;
            try {
                planted = PlantPlacementManager.Instance?.TryPlantSeedFromInventory(
                    capturedRuntimeState,
                    capturedGridPosition,
                    capturedWorldPosition
                ) ?? false;
            }
            catch (Exception e) {
                Debug.LogError($"[DeferredPlant] Exception during planting: {e}");
            }

            if (debugMode) Debug.Log($"[DeferredPlant] TryPlantSeedFromInventory returned: {planted}");

            if (planted) {
                // Advance tick
                if (debugMode) Debug.Log($"[DeferredPlant] Advancing {capturedTickCost} tick(s)");
                AdvanceGameTickStatic(capturedTickCost);

                // Consume seed (via callback)
                if (debugMode) Debug.Log("[DeferredPlant] Invoking success callback to consume seed");
                capturedCallback?.Invoke();

                // Fire event
                OnActionExecuted?.Invoke(PlayerActionType.PlantSeed, capturedSeedItem);

                if (debugMode) Debug.Log("[DeferredPlant] Deferred planting completed successfully");
            }
            else {
                Debug.LogWarning("[DeferredPlant] Planting failed in deferred action!");
            }
        };

        // Start minigame with deferred action
        bool minigameStarted = MinigameManager.Instance.TryTriggerMinigameWithDeferredAction(
            MinigameTrigger.Planting,
            gridPosition,
            worldPosition,
            deferredPlantAction,
            null, // Use default reward handling
            OnPlantingMinigameComplete
        );

        if (debugMode) Debug.Log($"[ExecutePlantSeedWithMinigame] Minigame started: {minigameStarted}");

        return minigameStarted;
    }

    /// <summary>
    /// Execute planting immediately (no minigame)
    /// </summary>
    bool ExecutePlantSeedImmediate(Vector3Int gridPosition, UIInventoryItem seedItem) {
        if (debugMode) Debug.Log($"[ExecutePlantSeedImmediate] Planting {seedItem?.GetDisplayName()} at {gridPosition}");

        if (seedItem == null || seedItem.Type != UIInventoryItem.ItemType.Seed) {
            Debug.LogError("[ExecutePlantSeedImmediate] Invalid seed item");
            return false;
        }

        Vector3 worldPosition = TileInteractionManager.Instance.interactionGrid.GetCellCenterWorld(gridPosition);

        bool result = PlantPlacementManager.Instance?.TryPlantSeedFromInventory(
            seedItem.SeedRuntimeState,
            gridPosition,
            worldPosition
        ) ?? false;

        if (debugMode) Debug.Log($"[ExecutePlantSeedImmediate] Result: {result}");
        return result;
    }

    /// <summary>
    /// Static method to advance tick - can be called from lambda without closure issues
    /// </summary>
    static void AdvanceGameTickStatic(int tickCount) {
        if (TickManager.Instance == null) {
            Debug.LogError("[AdvanceGameTickStatic] TickManager.Instance is null!");
            return;
        }
        for (int i = 0; i < tickCount; i++) {
            TickManager.Instance.AdvanceTick();
        }
    }

    bool CanPlantAtPosition(Vector3Int gridPosition, UIInventoryItem seedItem) {
        if (PlantPlacementManager.Instance == null) return false;
        
        if (PlantPlacementManager.Instance.IsPositionOccupied(gridPosition)) {
            if (debugMode) Debug.Log($"[CanPlantAtPosition] Position {gridPosition} is occupied");
            return false;
        }

        var tileDef = TileInteractionManager.Instance?.FindWhichTileDefinitionAt(gridPosition);
        if (!PlantPlacementManager.Instance.IsTileValidForSeed(tileDef, seedItem.SeedRuntimeState)) {
            if (debugMode) Debug.Log($"[CanPlantAtPosition] Tile not valid for this seed at {gridPosition}");
            return false;
        }

        return true;
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

    void AdvanceGameTick(int tickCount = 1) {
        AdvanceGameTickStatic(tickCount);
    }
}