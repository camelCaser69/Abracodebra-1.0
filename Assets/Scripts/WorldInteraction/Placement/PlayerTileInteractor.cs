using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Components;
using Abracodabra.UI.Genes;
using WegoSystem;
using Abracodabra.UI.Toolkit;

public sealed class PlayerTileInteractor : MonoBehaviour {
    [Header("References")]
    [SerializeField] TileInteractionManager tileInteractionManager;
    [SerializeField] Transform playerTransform;

    [Header("Debug")]
    [SerializeField] bool showDebug = false;

    bool pendingLeftClick;
    bool pendingRightClick;

    void Awake() {
        if (playerTransform == null)
            playerTransform = transform;
    }

    void Start() {
        FindReferences();
    }

    void Update() {
        if (RunManager.Instance?.CurrentState != RunState.GrowthAndThreat)
            return;

        if (Input.GetMouseButtonDown(0)) pendingLeftClick = true;
        if (Input.GetMouseButtonDown(1)) pendingRightClick = true;
    }

    void LateUpdate() {
        if (pendingLeftClick) {
            pendingLeftClick = false;
            HandleLeftClick();
        }
        if (pendingRightClick) {
            pendingRightClick = false;
            HandleRightClick();
        }
    }

    void HandleRightClick() {
        if (!EnsureReferences()) return;

        if (TryEatFoodFromWorld())
            return;

        TryEatFromInventory();
    }

    bool TryEatFoodFromWorld() {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        if (!tileInteractionManager.IsWithinInteractionRange)
            return false;

        Collider2D[] colliders = Physics2D.OverlapPointAll(mouseWorldPos);

        foreach (var collider in colliders) {
            FoodItem foodItem = collider.GetComponent<FoodItem>();
            if (foodItem == null) continue;

            Fruit fruit = collider.GetComponent<Fruit>();

            if (fruit != null && fruit.RepresentingItemDefinition == null) {
                if (showDebug) Debug.Log("[PlayerTileInteractor] Fruit doesn't have item definition yet");
                continue;
            }

            GardenerController player = playerTransform.GetComponent<GardenerController>();
            if (player == null || player.HungerSystem == null) {
                if (showDebug) Debug.LogWarning("[PlayerTileInteractor] Player has no HungerSystem");
                return false;
            }

            float nutrition = 0f;
            if (fruit != null && fruit.RepresentingItemDefinition != null) {
                nutrition = fruit.RepresentingItemDefinition.baseNutrition;
                if (fruit.DynamicProperties != null &&
                    fruit.DynamicProperties.TryGetValue("nutrition_multiplier", out float mult)) {
                    nutrition *= mult;
                }
            }
            else if (foodItem.foodType != null) {
                nutrition = foodItem.foodType.baseSatiationValue;
            }

            player.HungerSystem.Eat(nutrition);

            if (showDebug) Debug.Log($"[PlayerTileInteractor] Ate food from world for {nutrition:F1} nutrition");

            Destroy(collider.gameObject);

            if (PlayerActionManager.Instance != null) {
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

    void TryEatFromInventory()
    {
        UIInventoryItem selected = HotbarSelectionService.SelectedItem;
        if (selected == null || !selected.IsValid())
        {
            if (showDebug) Debug.Log("[PlayerTileInteractor] Right-click: No valid item selected");
            return;
        }

        if (selected.Type != UIInventoryItem.ItemType.Resource)
        {
            if (showDebug) Debug.Log("[PlayerTileInteractor] Right-click: Selected item is not a resource");
            return;
        }

        ItemInstance itemToConsume = selected.ResourceInstance;
        if (itemToConsume == null || !itemToConsume.definition.isConsumable)
        {
            if (showDebug) Debug.Log("[PlayerTileInteractor] Right-click: Item is not consumable");
            return;
        }

        GardenerController player = playerTransform.GetComponent<GardenerController>();
        if (player == null || player.HungerSystem == null)
        {
            if (showDebug) Debug.LogWarning("[PlayerTileInteractor] Player has no HungerSystem");
            return;
        }

        float nutrition = itemToConsume.GetNutrition();
        player.HungerSystem.Eat(nutrition);

        if (showDebug) Debug.Log($"[PlayerTileInteractor] Ate '{selected.GetDisplayName()}' for {nutrition:F1} nutrition");

        int selectedIndex = HotbarSelectionService.SelectedIndex;
        InventoryService.RemoveItemAtIndex(selectedIndex);

        if (PlayerActionManager.Instance != null)
        {
            PlayerActionManager.Instance.ExecutePlayerAction(
                PlayerActionType.Interact,
                tileInteractionManager.WorldToCell(playerTransform.position),
                "EatingFromInventory"
            );
        }
    }

    void HandleLeftClick()
    {
        if (!EnsureReferences()) return;

        if (tileInteractionManager.DidRefillThisFrame)
        {
            if (showDebug) Debug.Log("[PlayerTileInteractor] Left-click ignored: Refill happened this frame.");
            return;
        }

        UIInventoryItem selected = HotbarSelectionService.SelectedItem;
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
            case UIInventoryItem.ItemType.Tool:
                HandleToolUse(selected, cellPos);
                break;

            case UIInventoryItem.ItemType.Seed:
                HandleSeedPlanting(selected, cellPos);
                break;
        }
    }

    void HandleToolUse(UIInventoryItem selected, Vector3Int cellPos)
    {
        var toolDef = selected.ToolDefinition;
        if (toolDef == null) return;

        if (ToolSwitcher.Instance != null && ToolSwitcher.Instance.CurrentTool != toolDef)
        {
            ToolSwitcher.Instance.SelectToolByDefinition(toolDef);
        }

        if (toolDef.toolType == ToolType.HarvestPouch)
        {
            HandleHarvest(cellPos);
        }
        else
        {
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

    void HandleSeedPlanting(UIInventoryItem selected, Vector3Int cellPos)
    {
        int selectedIndex = HotbarSelectionService.SelectedIndex;

        System.Action onSuccess = () =>
        {
            if (showDebug) Debug.Log($"[PlayerTileInteractor] Successfully planted '{selected.GetDisplayName()}'. Removing from inventory.");
            InventoryService.RemoveItemAtIndex(selectedIndex);
        };

        bool success = PlayerActionManager.Instance?.ExecutePlayerAction(
            PlayerActionType.PlantSeed, cellPos, selected, onSuccess
        ) ?? false;

        if (!success && showDebug)
        {
            Debug.Log($"[PlayerTileInteractor] Failed to plant '{selected.GetDisplayName()}' at {cellPos}");
        }
    }

    void HandleHarvest(Vector3Int cellPos) {
        bool success = PlayerActionManager.Instance?.ExecutePlayerAction(
            PlayerActionType.Harvest, 
            cellPos
        ) ?? false;

        if (!success && showDebug) {
            Debug.Log($"[PlayerTileInteractor] Harvest failed at {cellPos}");
        }
    }

    void FindReferences() {
        if (tileInteractionManager == null)
            tileInteractionManager = TileInteractionManager.Instance;

        if (playerTransform == null)
            playerTransform = FindFirstObjectByType<GardenerController>()?.transform;
    }

    bool EnsureReferences() {
        if (tileInteractionManager == null) {
            tileInteractionManager = TileInteractionManager.Instance;
            if (tileInteractionManager == null) {
                if (showDebug) Debug.LogWarning("[PlayerTileInteractor] TileInteractionManager reference missing.");
                return false;
            }
        }
        return true;
    }
}