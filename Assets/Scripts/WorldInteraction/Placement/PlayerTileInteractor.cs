using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Components; // For Fruit
using Abracodabra.UI.Genes;
using WegoSystem;

/// <summary>
/// Handles player interactions with tiles in the game world.
/// 
/// UPDATED: Now uses the new UI Toolkit services:
/// - HotbarSelectionService for getting selected item
/// - InventoryService for inventory management (add/remove items)
/// 
/// Left-click: Use tool, plant seed, harvest
/// Right-click: Eat consumable from inventory, eat food from world
/// </summary>
public sealed class PlayerTileInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TileInteractionManager tileInteractionManager;
    [SerializeField] private Transform playerTransform;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Input flags
    private bool pendingLeftClick;
    private bool pendingRightClick;

    void Awake()
    {
        if (playerTransform == null)
            playerTransform = transform;
    }

    void Start()
    {
        FindReferences();
    }

    void Update()
    {
        // Only process input during gameplay
        if (RunManager.Instance?.CurrentState != RunState.GrowthAndThreat)
            return;

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

    #region Right Click - Eat/Consume
    /// <summary>
    /// Right-click: Eat consumable from inventory OR eat food directly from world
    /// </summary>
    private void HandleRightClick()
    {
        if (!EnsureReferences()) return;

        // First, check if clicking on food in the world
        if (TryEatFoodFromWorld())
            return;

        // Otherwise, try to eat from inventory
        TryEatFromInventory();
    }

    /// <summary>
    /// Try to eat a FoodItem that the player clicked on in the world
    /// </summary>
    private bool TryEatFoodFromWorld()
    {
        // Raycast to find FoodItem under mouse
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // Check if within interaction range
        if (!tileInteractionManager.IsWithinInteractionRange)
            return false;

        // Use Physics2D overlap to find FoodItem
        Collider2D[] colliders = Physics2D.OverlapPointAll(mouseWorldPos);
        
        foreach (var collider in colliders)
        {
            FoodItem foodItem = collider.GetComponent<FoodItem>();
            if (foodItem == null) continue;

            // Check if it's attached to a plant (fruit on plant)
            Fruit fruit = collider.GetComponent<Fruit>();
            
            // If it's a fruit that's still growing (hasn't been detached), skip it
            // We can eat it if it has no Rigidbody2D (still attached) only if it has an item definition
            if (fruit != null && fruit.RepresentingItemDefinition == null)
            {
                if (showDebug) Debug.Log("[PlayerTileInteractor] Fruit doesn't have item definition yet");
                continue;
            }

            // Get player hunger system
            GardenerController player = playerTransform.GetComponent<GardenerController>();
            if (player == null || player.HungerSystem == null)
            {
                if (showDebug) Debug.LogWarning("[PlayerTileInteractor] Player has no HungerSystem");
                return false;
            }

            // Calculate nutrition value
            float nutrition = 0f;
            if (fruit != null && fruit.RepresentingItemDefinition != null)
            {
                nutrition = fruit.RepresentingItemDefinition.baseNutrition;
                if (fruit.DynamicProperties != null && 
                    fruit.DynamicProperties.TryGetValue("nutrition_multiplier", out float mult))
                {
                    nutrition *= mult;
                }
            }
            else if (foodItem.foodType != null)
            {
                nutrition = foodItem.foodType.baseSatiationValue;
            }

            // Eat the food
            player.HungerSystem.Eat(nutrition);

            if (showDebug) Debug.Log($"[PlayerTileInteractor] Ate food from world for {nutrition:F1} nutrition");

            // Destroy the food object
            Destroy(collider.gameObject);

            // Advance tick for the action
            if (PlayerActionManager.Instance != null)
            {
                PlayerActionManager.Instance.ExecutePlayerAction(
                    PlayerActionType.Interact,
                    tileInteractionManager.WorldToCell(mouseWorldPos),
                    "EatingFromWorld"
                );
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Try to eat a consumable item from the inventory
    /// </summary>
    private void TryEatFromInventory()
    {
        // Get selected item from new service
        InventoryBarItem selected = HotbarSelectionService.SelectedItem;
        if (selected == null || !selected.IsValid())
        {
            if (showDebug) Debug.Log("[PlayerTileInteractor] Right-click: No valid item selected");
            return;
        }

        // Only resources can be eaten
        if (selected.Type != InventoryBarItem.ItemType.Resource)
        {
            if (showDebug) Debug.Log("[PlayerTileInteractor] Right-click: Selected item is not a resource");
            return;
        }

        ItemInstance itemToConsume = selected.ItemInstance;
        if (itemToConsume == null || !itemToConsume.definition.isConsumable)
        {
            if (showDebug) Debug.Log("[PlayerTileInteractor] Right-click: Item is not consumable");
            return;
        }

        // Get player hunger system
        GardenerController player = playerTransform.GetComponent<GardenerController>();
        if (player == null || player.HungerSystem == null)
        {
            if (showDebug) Debug.LogWarning("[PlayerTileInteractor] Player has no HungerSystem");
            return;
        }

        // Eat the item
        float nutrition = itemToConsume.GetNutrition();
        player.HungerSystem.Eat(nutrition);

        if (showDebug) Debug.Log($"[PlayerTileInteractor] Ate '{selected.GetDisplayName()}' for {nutrition:F1} nutrition");

        // Remove item from inventory using NEW service
        int selectedIndex = HotbarSelectionService.SelectedIndex;
        InventoryService.RemoveItemAtIndex(selectedIndex);

        // Advance tick
        if (PlayerActionManager.Instance != null)
        {
            PlayerActionManager.Instance.ExecutePlayerAction(
                PlayerActionType.Interact,
                tileInteractionManager.WorldToCell(playerTransform.position),
                "EatingFromInventory"
            );
        }
    }
    #endregion

    #region Left Click - Tool/Plant/Harvest
    /// <summary>
    /// Left-click: Use tool, plant seed, or harvest
    /// </summary>
    private void HandleLeftClick()
    {
        if (!EnsureReferences()) return;

        // Get selected item from new service
        InventoryBarItem selected = HotbarSelectionService.SelectedItem;
        if (selected == null || !selected.IsValid())
        {
            if (showDebug) Debug.Log("[PlayerTileInteractor] Left-click ignored: No valid item selected.");
            return;
        }

        if (!tileInteractionManager.IsWithinInteractionRange)
        {
            if (showDebug) Debug.Log("[PlayerTileInteractor] Left-click ignored: Target cell out of range.");
            return;
        }

        if (!tileInteractionManager.CurrentlyHoveredCell.HasValue)
        {
            if (showDebug) Debug.Log("[PlayerTileInteractor] Left-click ignored: No hovered cell.");
            return;
        }

        Vector3Int cellPos = tileInteractionManager.CurrentlyHoveredCell.Value;

        if (showDebug) Debug.Log($"[PlayerTileInteractor] Action '{selected.Type}' with '{selected.GetDisplayName()}' at {cellPos}");

        switch (selected.Type)
        {
            case InventoryBarItem.ItemType.Tool:
                HandleToolUse(selected, cellPos);
                break;

            case InventoryBarItem.ItemType.Seed:
                HandleSeedPlanting(selected, cellPos);
                break;
        }
    }

    /// <summary>
    /// Handle using a tool
    /// </summary>
    private void HandleToolUse(InventoryBarItem selected, Vector3Int cellPos)
    {
        var toolDef = selected.ToolDefinition;
        if (toolDef == null) return;

        // Ensure tool is selected in ToolSwitcher
        if (ToolSwitcher.Instance != null && ToolSwitcher.Instance.CurrentTool != toolDef)
        {
            ToolSwitcher.Instance.SelectToolByDefinition(toolDef);
        }

        if (toolDef.toolType == ToolType.HarvestPouch)
        {
            // Harvest action
            HandleHarvest(cellPos);
        }
        else
        {
            // Normal tool action
            if (ToolSwitcher.Instance != null && toolDef.limitedUses)
            {
                if (!ToolSwitcher.Instance.TryConsumeUse())
                {
                    if (showDebug) Debug.Log($"[PlayerTileInteractor] Tool '{toolDef.displayName}' is out of uses.");
                    return;
                }
            }
            PlayerActionManager.Instance?.ExecutePlayerAction(PlayerActionType.UseTool, cellPos, toolDef);
        }
    }

    /// <summary>
    /// Handle planting a seed
    /// </summary>
    private void HandleSeedPlanting(InventoryBarItem selected, Vector3Int cellPos)
    {
        int selectedIndex = HotbarSelectionService.SelectedIndex;

        // Define success callback - removes seed from inventory AFTER successful planting
        System.Action onSuccess = () =>
        {
            if (showDebug) Debug.Log($"[PlayerTileInteractor] Successfully planted '{selected.GetDisplayName()}'. Removing from inventory.");
            
            // Remove from inventory using NEW service
            InventoryService.RemoveItemAtIndex(selectedIndex);
        };

        // Attempt to plant
        bool success = PlayerActionManager.Instance?.ExecutePlayerAction(
            PlayerActionType.PlantSeed,
            cellPos,
            selected,
            onSuccess
        ) ?? false;

        if (!success && showDebug)
        {
            Debug.Log($"[PlayerTileInteractor] Failed to plant '{selected.GetDisplayName()}' at {cellPos}");
        }
    }

    /// <summary>
    /// Handle harvesting
    /// </summary>
    private void HandleHarvest(Vector3Int cellPos)
    {
        // Define success callback - note: PlayerActionManager already adds items to inventory
        // But it uses the OLD system, so we need to intercept this
        
        // For now, just execute the harvest - we'll need to update PlayerActionManager
        // to use InventoryService instead of InventoryGridController
        
        bool success = PlayerActionManager.Instance?.ExecutePlayerAction(
            PlayerActionType.Harvest,
            cellPos
        ) ?? false;

        if (showDebug)
        {
            if (success)
                Debug.Log($"[PlayerTileInteractor] Harvest successful at {cellPos}");
            else
                Debug.Log($"[PlayerTileInteractor] Harvest failed at {cellPos}");
        }
    }
    #endregion

    #region Helpers
    private bool EnsureReferences()
    {
        if (tileInteractionManager == null)
            FindReferences();

        return tileInteractionManager != null;
    }

    private void FindReferences()
    {
        if (tileInteractionManager == null)
            tileInteractionManager = TileInteractionManager.Instance;
    }
    #endregion
}