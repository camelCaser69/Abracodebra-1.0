using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Runtime;
using Abracodabra.UI.Genes;
using WegoSystem;

/// <summary>
/// Handles player tile interactions - clicking to use tools, plant seeds, etc.
/// Uses HotbarSelectionService to get the currently selected item.
/// </summary>
public sealed class PlayerTileInteractor : MonoBehaviour
{
    [SerializeField] private TileInteractionManager tileInteractionManager;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private bool showDebug = false;

    private bool pendingLeftClick;
    private bool pendingRightClick;

    void Awake()
    {
        if (playerTransform == null)
            playerTransform = transform;
    }

    void Start()
    {
        FindSingletons();
    }

    void Update()
    {
        if (RunManager.Instance?.CurrentState != RunState.GrowthAndThreat)
            return;

        if (Input.GetMouseButtonDown(0))
            pendingLeftClick = true;
        if (Input.GetMouseButtonDown(1))
            pendingRightClick = true;
    }

    void LateUpdate()
    {
        if (pendingLeftClick)
        {
            pendingLeftClick = false;
            HandleLeftClick();
        }
        if (pendingRightClick)
        {
            pendingRightClick = false;
            HandleRightClick();
        }
    }

    void HandleRightClick()
    {
        if (!EnsureManagers()) return;

        // Use HotbarSelectionService instead of old InventoryBarController
        InventoryBarItem selected = HotbarSelectionService.SelectedItem;
        if (selected == null || !selected.IsValid()) return;

        if (selected.Type == InventoryBarItem.ItemType.Resource)
        {
            ItemInstance itemToConsume = selected.ItemInstance;
            if (itemToConsume == null || !itemToConsume.definition.isConsumable)
            {
                return; // Not a valid consumable item
            }

            GardenerController player = playerTransform.GetComponent<GardenerController>();
            if (player == null || player.HungerSystem == null) return;

            player.HungerSystem.Eat(itemToConsume.GetNutrition());

            System.Action onSuccess = () =>
            {
                InventoryGridController.Instance?.RemoveItemFromInventory(selected);
                // Note: We don't need to call SelectSlotByIndex on the old controller anymore
            };

            PlayerActionManager.Instance.ExecutePlayerAction(
                PlayerActionType.Interact,
                tileInteractionManager.WorldToCell(playerTransform.position),
                "Eating",
                onSuccess
            );
        }
    }

    void HandleLeftClick()
    {
        if (!EnsureManagers()) return;

        // CRITICAL FIX: Use HotbarSelectionService instead of old InventoryBarController
        InventoryBarItem selected = HotbarSelectionService.SelectedItem;

        if (selected == null || !selected.IsValid())
        {
            if (showDebug) Debug.Log("[PlayerTileInteractor] Left-click ignored: No valid item selected.");
            return;
        }

        if (!tileInteractionManager.CurrentlyHoveredCell.HasValue)
        {
            if (showDebug) Debug.Log("[PlayerTileInteractor] Left-click ignored: No hovered cell.");
            return;
        }

        if (!tileInteractionManager.IsWithinInteractionRange)
        {
            if (showDebug) Debug.Log($"[PlayerTileInteractor] Left-click ignored: Target cell is out of range.");
            return;
        }

        Vector3Int cellPos = tileInteractionManager.CurrentlyHoveredCell.Value;

        if (showDebug) Debug.Log($"[PlayerTileInteractor] Attempting action '{selected.Type}' with item '{selected.GetDisplayName()}' at {cellPos}.");

        switch (selected.Type)
        {
            case InventoryBarItem.ItemType.Tool:
                HandleToolUse(selected, cellPos);
                break;

            case InventoryBarItem.ItemType.Seed:
                HandleSeedPlant(selected, cellPos);
                break;

            case InventoryBarItem.ItemType.Gene:
                if (showDebug) Debug.Log("[PlayerTileInteractor] Genes cannot be used directly on tiles.");
                break;

            case InventoryBarItem.ItemType.Resource:
                if (showDebug) Debug.Log("[PlayerTileInteractor] Resources cannot be used on tiles. Right-click to consume.");
                break;
        }
    }

    private void HandleToolUse(InventoryBarItem selected, Vector3Int cellPos)
    {
        var toolDef = selected.ToolDefinition;
        if (toolDef == null) return;

        if (ToolSwitcher.Instance != null && ToolSwitcher.Instance.CurrentTool != toolDef)
        {
            ToolSwitcher.Instance.SelectToolByDefinition(toolDef);
        }

        if (toolDef.toolType == ToolType.HarvestPouch)
        {
            PlayerActionManager.Instance.ExecutePlayerAction(PlayerActionType.Harvest, cellPos, toolDef);
        }
        else
        {
            if (ToolSwitcher.Instance != null)
            {
                if (!ToolSwitcher.Instance.TryConsumeUse())
                {
                    if (showDebug) Debug.Log($"[PlayerTileInteractor] Action blocked: Tool '{selected.GetDisplayName()}' is out of uses.");
                    return;
                }
            }
            PlayerActionManager.Instance.ExecutePlayerAction(PlayerActionType.UseTool, cellPos, toolDef);
        }
    }

    private void HandleSeedPlant(InventoryBarItem selected, Vector3Int cellPos)
    {
        System.Action onSuccess = () =>
        {
            if (showDebug) Debug.Log($"[PlayerTileInteractor] Successfully planted '{selected.GetDisplayName()}'. Removing from inventory.");
            InventoryGridController.Instance?.RemoveItemFromInventory(selected);
            // Note: Selection handling is now done through HotbarSelectionService
        };

        PlayerActionManager.Instance.ExecutePlayerAction(PlayerActionType.PlantSeed, cellPos, selected, onSuccess);
    }

    bool EnsureManagers()
    {
        if (tileInteractionManager == null)
            FindSingletons();
        return tileInteractionManager != null;
    }

    void FindSingletons()
    {
        if (tileInteractionManager == null)
            tileInteractionManager = TileInteractionManager.Instance;
    }
}