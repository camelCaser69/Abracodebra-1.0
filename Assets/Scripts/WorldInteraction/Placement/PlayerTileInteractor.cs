using System.Collections.Generic;
using Abracodabra.Genes.Runtime;
using UnityEngine;
using Abracodabra.UI.Genes;
using WegoSystem;

public sealed class PlayerTileInteractor : MonoBehaviour
{
    [SerializeField] private InventoryBarController inventoryBar;
    [SerializeField] private TileInteractionManager tileInteractionManager;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private bool showDebug = false;

    private bool pendingLeftClick;
    private bool pendingRightClick;

    private void Awake()
    {
        if (playerTransform == null) playerTransform = transform;
    }

    private void Start()
    {
        FindSingletons();
    }

    private void Update()
    {
        if (RunManager.Instance?.CurrentState != RunState.GrowthAndThreat) return;
        if (Input.GetMouseButtonDown(0)) pendingLeftClick = true;
        if (Input.GetMouseButtonDown(1)) pendingRightClick = true;
    }

    private void LateUpdate()
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

    private void HandleRightClick()
    {
        if (!EnsureManagers()) return;

        InventoryBarItem selected = inventoryBar.SelectedItem;
        if (selected == null || !selected.IsValid()) return;

        if (selected.Type == InventoryBarItem.ItemType.Gene)
        {
            // FIX: Wrap the single GeneInstance in a new List to match the updated constructor.
            var itemData = new HarvestedItem(new List<RuntimeGeneInstance> { selected.GeneInstance });

            if (!itemData.IsConsumable()) return;

            GardenerController player = playerTransform.GetComponent<GardenerController>();
            if (player == null || player.HungerSystem == null) return;

            player.HungerSystem.Eat(itemData.GetNutritionValue());

            System.Action onSuccess = () => {
                InventoryGridController.Instance.RemoveItemFromInventory(selected);
                inventoryBar.ShowBar(); // Refresh the bar after removing an item
            };

            PlayerActionManager.Instance.ExecutePlayerAction(PlayerActionType.Interact,
                tileInteractionManager.WorldToCell(playerTransform.position), "Eating", onSuccess);
        }
    }

    private void HandleLeftClick()
    {
        if (!EnsureManagers()) return;

        InventoryBarItem selected = inventoryBar.SelectedItem;
        if (selected == null || !selected.IsValid())
        {
            if (showDebug) Debug.Log("[PlayerTileInteractor] Left-click ignored: No valid item selected.");
            return;
        }

        if (!tileInteractionManager.IsWithinInteractionRange)
        {
            if (showDebug) Debug.Log($"[PlayerTileInteractor] Left-click ignored: Target cell is out of range according to TileInteractionManager.");
            return;
        }

        Vector3Int cellPos = tileInteractionManager.CurrentlyHoveredCell.Value;

        if (showDebug) Debug.Log($"[PlayerTileInteractor] Attempting action '{selected.Type}' with item '{selected.GetDisplayName()}' at {cellPos}.");

        switch (selected.Type)
        {
            case InventoryBarItem.ItemType.Tool:
                var toolDef = selected.ToolDefinition;

                if (ToolSwitcher.Instance != null && ToolSwitcher.Instance.CurrentTool != toolDef)
                {
                    ToolSwitcher.Instance.SelectToolByDefinition(toolDef);
                }
                
                // NEW: Special case for Harvest Pouch to call the correct action type.
                if (toolDef.toolType == ToolType.HarvestPouch)
                {
                    // We don't consume a use for harvesting until we know it was successful.
                    // PlayerActionManager will handle tick cost.
                    PlayerActionManager.Instance.ExecutePlayerAction(PlayerActionType.Harvest, cellPos, toolDef);
                }
                else
                {
                    // Standard tool usage
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
                break;

            case InventoryBarItem.ItemType.Seed:
                System.Action onSuccess = () =>
                {
                    if (showDebug) Debug.Log($"[PlayerTileInteractor] Successfully planted '{selected.GetDisplayName()}'. Removing from inventory.");
                    InventoryGridController.Instance?.RemoveItemFromInventory(selected);
                    inventoryBar.SelectSlotByIndex(0);
                };
                PlayerActionManager.Instance.ExecutePlayerAction(PlayerActionType.PlantSeed, cellPos, selected, onSuccess);
                break;
        }
    }

    private bool EnsureManagers()
    {
        if (tileInteractionManager == null || inventoryBar == null) FindSingletons();
        return tileInteractionManager != null && inventoryBar != null;
    }

    private void FindSingletons()
    {
        if (inventoryBar == null) inventoryBar = InventoryBarController.Instance;
        if (tileInteractionManager == null) tileInteractionManager = TileInteractionManager.Instance;
    }
}