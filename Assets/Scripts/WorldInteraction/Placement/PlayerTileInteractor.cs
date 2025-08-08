// REWORKED FILE: Assets/Scripts/WorldInteraction/Placement/PlayerTileInteractor.cs
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

    private void HandleRightClick()
    {
        if (!EnsureManagers()) return;

        InventoryBarItem selected = inventoryBar.SelectedItem;
        if (selected == null || !selected.IsValid()) return;
        
        // FIX: Logic for consuming items (right-click)
        if (selected.Type == InventoryBarItem.ItemType.Gene)
        {
            var itemData = new HarvestedItem(selected.GeneInstance);
            if (!itemData.IsConsumable()) return;

            GardenerController player = playerTransform.GetComponent<GardenerController>();
            if (player == null || player.HungerSystem == null) return;
            
            player.HungerSystem.Eat(itemData.GetNutritionValue());

            System.Action onSuccess = () => {
                // Remove the gene from the main inventory grid
                InventoryGridController.Instance.RemoveGeneFromInventory(selected.GeneInstance);
                inventoryBar.ShowBar(); // Refresh the bar
            };
            
            PlayerActionManager.Instance.ExecutePlayerAction(PlayerActionType.Interact, 
                tileInteractionManager.WorldToCell(playerTransform.position), "Eating", onSuccess);
        }
    }

    private void HandleLeftClick()
    {
        if (!EnsureManagers()) return;

        InventoryBarItem selected = inventoryBar.SelectedItem;
        if (selected == null || !selected.IsValid()) return;

        Vector3 mouseW = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPos = tileInteractionManager.WorldToCell(mouseW);
        Vector3 cellCenter = tileInteractionManager.interactionGrid.GetCellCenterWorld(cellPos);

        if (Vector2.Distance(playerTransform.position, cellCenter) > tileInteractionManager.hoverRadius) return;
        
        // FIX: Use a switch on the item type to determine the action
        switch (selected.Type)
        {
            case InventoryBarItem.ItemType.Tool:
                PlayerActionManager.Instance.ExecutePlayerAction(PlayerActionType.UseTool, cellPos, selected.ToolDefinition);
                break;
            
            case InventoryBarItem.ItemType.Seed:
                System.Action onSuccess = () => {
                    // This logic is tricky. The bar item is a temporary representation.
                    // We need to find and remove the source seed from the inventory.
                    Debug.LogWarning("Seed removal from inventory after planting is not yet implemented.");
                    inventoryBar.ShowBar(); // Refresh the bar
                };
                PlayerActionManager.Instance.ExecutePlayerAction(PlayerActionType.PlantSeed, cellPos, selected, onSuccess);
                break;
                
            case InventoryBarItem.ItemType.Gene:
                // Do nothing when left-clicking a gene on the world
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