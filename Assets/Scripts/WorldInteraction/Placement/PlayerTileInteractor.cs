using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class PlayerTileInteractor : MonoBehaviour
{
    [SerializeField] private InventoryBarController inventoryBar;
    [SerializeField] private TileInteractionManager tileInteractionManager;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private bool showDebug = false;

    private bool pendingClick;

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
        if (Input.GetMouseButtonDown(0)) pendingClick = true;
    }

    void LateUpdate()
    {
        if (!pendingClick) return;
        pendingClick = false;
        HandleLeftClick();
    }

    private void HandleLeftClick()
    {
        if (!EnsureManagers()) return;

        InventoryBarItem selected = inventoryBar.SelectedItem;
        if (selected == null || !selected.IsValid()) return;

        Vector3 mouseW = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseW.z = 0f;
        Vector3Int cellPos = tileInteractionManager.WorldToCell(mouseW);
        Vector3 cellCenter = tileInteractionManager.interactionGrid.GetCellCenterWorld(cellPos);

        if (Vector2.Distance(playerTransform.position, cellCenter) > tileInteractionManager.hoverRadius) return;

        if (selected.Type == InventoryBarItem.ItemType.Tool)
        {
            tileInteractionManager.ApplyToolAction(selected.ToolDefinition);
        }
        else if (selected.Type == InventoryBarItem.ItemType.Node && selected.IsSeed())
        {
            bool planted = PlantPlacementManager.Instance?.TryPlantSeedFromInventory(selected, cellPos, cellCenter) ?? false;
            if (planted)
            {
                RemoveSeedFromInventory(selected);
                inventoryBar.ShowBar(); // Refresh the bar
            }
        }
    }

    private void RemoveSeedFromInventory(InventoryBarItem seed)
    {
        InventoryGridController grid = InventoryGridController.Instance;
        if (grid == null) return;
        
        // --- MODIFIED: Uses GetItemView() ---
        if (seed.ViewGameObject != null)
        {
            for (int i = 0; i < grid.ActualCellCount; i++)
            {
                NodeCell cell = grid.GetInventoryCellAtIndex(i);
                if (cell != null && cell.GetItemView()?.gameObject == seed.ViewGameObject)
                {
                    cell.RemoveNode();
                    return;
                }
            }
        }

        // Fallback check by data ID if view object doesn't match
        for (int i = 0; i < grid.ActualCellCount; i++)
        {
            NodeCell cell = grid.GetInventoryCellAtIndex(i);
            if (cell != null && cell.GetNodeData()?.nodeId == seed.NodeData.nodeId)
            {
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