// REWORKED FILE: Assets/Scripts/WorldInteraction/Placement/PlayerTileInteractor.cs

using Abracodabra.UI.Genes;
using UnityEngine;
using WegoSystem;

public sealed class PlayerTileInteractor : MonoBehaviour
{
    [SerializeField] private InventoryBarController inventoryBar;
    [SerializeField] private TileInteractionManager tileInteractionManager;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private bool showDebug = false;

    private bool pendingLeftClick;
    private bool pendingRightClick;

    void Awake()
    {
        if (playerTransform == null) playerTransform = transform;
    }

    void Start()
    {
        FindSingletons();
    }

    void Update()
    {
        if (RunManager.Instance?.CurrentState != RunState.GrowthAndThreat) return;
        if (Input.GetMouseButtonDown(0)) pendingLeftClick = true;
        if (Input.GetMouseButtonDown(1)) pendingRightClick = true;
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

    // In file: Assets/Scripts/WorldInteraction/Placement/PlayerTileInteractor.cs

    private void HandleRightClick()
    {
        if (!EnsureManagers()) return;

        InventoryBarItem selected = inventoryBar.SelectedItem;
        if (selected == null || !selected.IsValid()) return;

        // Only consumable genes can be right-clicked to be eaten
        if (selected.Type == InventoryBarItem.ItemType.Gene)
        {
            var itemData = new HarvestedItem(selected.GeneInstance);
            if (!itemData.IsConsumable()) return;

            GardenerController player = playerTransform.GetComponent<GardenerController>();
            if (player == null || player.HungerSystem == null) return;

            // Eat the item
            player.HungerSystem.Eat(itemData.GetNutritionValue());

            // Define what happens on a successful action
            System.Action onSuccess = () =>
            {
                // THIS IS THE FIX: Call the correct generic removal method
                InventoryGridController.Instance.RemoveItemFromInventory(selected);

                // Refresh the bar to show the item has been removed
                inventoryBar.ShowBar(); 
            };

            // Execute the action with the success callback
            PlayerActionManager.Instance.ExecutePlayerAction(PlayerActionType.Interact,
                tileInteractionManager.WorldToCell(playerTransform.position), "Eating", onSuccess);
        }
    }

    
        // In file: Assets/Scripts/WorldInteraction/Placement/PlayerTileInteractor.cs

        // In file: Assets/Scripts/WorldInteraction/Placement/PlayerTileInteractor.cs

        // In file: Assets/Scripts/WorldInteraction/Placement/PlayerTileInteractor.cs

        // Only the HandleLeftClick method needs to be changed.

        // Only the HandleLeftClick method is changed.

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

            // NEW: Check for and consume a tool use before executing the action.
            if (ToolSwitcher.Instance != null)
            {
                if (!ToolSwitcher.Instance.TryConsumeUse())
                {
                    if (showDebug) Debug.Log($"[PlayerTileInteractor] Action blocked: Tool '{selected.GetDisplayName()}' is out of uses.");
                    // Optionally, provide player feedback here (e.g., sound effect)
                    return; // Abort the action
                }
            }

            PlayerActionManager.Instance.ExecutePlayerAction(PlayerActionType.UseTool, cellPos, toolDef);
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