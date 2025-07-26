// Assets/Scripts/WorldInteraction/Placement/PlayerTileInteractor.cs

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
        if (selected == null || !selected.IsValid() || selected.Type != InventoryBarItem.ItemType.Node)
        {
            if (showDebug) Debug.Log("[PlayerTileInteractor] Right-clicked, but no valid consumable node is selected.");
            return;
        }

        var itemData = new HarvestedItem(selected.NodeData);
        if (!itemData.IsConsumable())
        {
            if (showDebug) Debug.Log($"[PlayerTileInteractor] Item '{selected.GetDisplayName()}' is not consumable.");
            return;
        }
        
        GardenerController player = playerTransform.GetComponent<GardenerController>();
        if (player == null || player.HungerSystem == null)
        {
             Debug.LogError("[PlayerTileInteractor] Cannot consume item: Player or their HungerSystem not found!");
             return;
        }

        if (showDebug) Debug.Log($"[PlayerTileInteractor] Consuming item '{selected.GetDisplayName()}'.");
        
        player.HungerSystem.Eat(itemData.GetNutritionValue());
        
        System.Action onSuccess = () =>
        {
            if (showDebug) Debug.Log($"[PlayerTileInteractor] Consumption successful. Removing '{selected.GetDisplayName()}' from inventory.");
            RemoveSeedFromInventory(selected);
            inventoryBar.ShowBar(); 
        };

        PlayerActionManager.Instance.ExecutePlayerAction(
            PlayerActionType.Interact,
            tileInteractionManager.WorldToCell(playerTransform.position),
            "Eating",
            onSuccess
        );
    }

    private void HandleLeftClick()
    {
        if (!EnsureManagers()) return;

        InventoryBarItem selected = inventoryBar.SelectedItem;
        if (selected == null || !selected.IsValid())
        {
            if (showDebug) Debug.Log("[PlayerTileInteractor] Click handled, but no valid item selected in the bar.");
            return;
        }

        Vector3 mouseW = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseW.z = 0f;
        Vector3Int cellPos = tileInteractionManager.WorldToCell(mouseW);
        Vector3 cellCenter = tileInteractionManager.interactionGrid.GetCellCenterWorld(cellPos);

        if (Vector2.Distance(playerTransform.position, cellCenter) > tileInteractionManager.hoverRadius)
        {
            if (showDebug) Debug.Log($"[PlayerTileInteractor] Clicked on cell {cellPos}, but it's too far away.");
            return;
        }

        if (PlayerActionManager.Instance == null)
        {
            Debug.LogError("[PlayerTileInteractor] PlayerActionManager not found!");
            return;
        }

        if (selected.Type == InventoryBarItem.ItemType.Tool)
        {
            if (showDebug) Debug.Log($"[PlayerTileInteractor] Using tool '{selected.GetDisplayName()}' at cell {cellPos}.");
            PlayerActionManager.Instance.ExecutePlayerAction(
                PlayerActionType.UseTool,
                cellPos,
                selected.ToolDefinition
            );
        }
        else if (selected.Type == InventoryBarItem.ItemType.Node && selected.IsSeed())
        {
            if (showDebug) Debug.Log($"[PlayerTileInteractor] Planting seed '{selected.GetDisplayName()}' at cell {cellPos}.");

            System.Action onSuccess = () =>
            {
                if (showDebug) Debug.Log($"[PlayerTileInteractor] Planting was successful. Removing seed '{selected.GetDisplayName()}' from inventory via callback.");
                RemoveSeedFromInventory(selected);
                inventoryBar.ShowBar();
            };

            PlayerActionManager.Instance.ExecutePlayerAction(
                PlayerActionType.PlantSeed,
                cellPos,
                selected,
                onSuccess
            );
        }
    }

    public void RemoveSeedFromInventory(InventoryBarItem seed)
    {
        InventoryGridController grid = InventoryGridController.Instance;
        if (grid == null) return;

        if (seed.ViewGameObject != null)
        {
            for (int i = 0; i < grid.ActualCellCount; i++)
            {
                NodeCell cell = grid.GetInventoryCellAtIndex(i);
                if (cell != null && cell.GetItemView()?.gameObject == seed.ViewGameObject)
                {
                    if (showDebug) Debug.Log($"[PlayerTileInteractor] Removing seed '{seed.GetDisplayName()}' from inventory cell {i} by view reference.");
                    cell.RemoveNode();
                    return;
                }
            }
        }

        for (int i = 0; i < grid.ActualCellCount; i++)
        {
            NodeCell cell = grid.GetInventoryCellAtIndex(i);
            if (cell != null && cell.GetNodeData()?.nodeId == seed.NodeData.nodeId)
            {
                if (showDebug) Debug.Log($"[PlayerTileInteractor] Removing seed '{seed.GetDisplayName()}' from inventory cell {i} by data ID fallback.");
                cell.RemoveNode();
                return;
            }
        }
    }

    private bool EnsureManagers()
    {
        if (tileInteractionManager == null || inventoryBar == null) FindSingletons();
        return tileInteractionManager != null && inventoryBar != null;
    }

    private void FindSingletons()
    {
        if (inventoryBar == null) inventoryBar = InventoryBarController.Instance ?? FindAnyObjectByType<InventoryBarController>(FindObjectsInactive.Include);
        if (tileInteractionManager == null) tileInteractionManager = TileInteractionManager.Instance ?? FindAnyObjectByType<TileInteractionManager>(FindObjectsInactive.Include);
    }
}