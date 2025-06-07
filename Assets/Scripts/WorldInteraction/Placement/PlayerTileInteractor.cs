using UnityEngine;

public sealed class PlayerTileInteractor : MonoBehaviour
{
    [SerializeField] private InventoryBarController inventoryBar;
    [SerializeField] private TileInteractionManager tileInteractionManager;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private bool showDebug = false;

    private bool pendingClick;

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
        if (Input.GetMouseButtonDown(0)) pendingClick = true;
    }

    private void LateUpdate()
    {
        if (!pendingClick) return;
        pendingClick = false;
        HandleLeftClick();
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

        if (selected.Type == InventoryBarItem.ItemType.Tool)
        {
            if (showDebug) Debug.Log($"[PlayerTileInteractor] Applying tool '{selected.GetDisplayName()}' to cell {cellPos}.");
            tileInteractionManager.ApplyToolAction(selected.ToolDefinition);
        }
        else if (selected.Type == InventoryBarItem.ItemType.Node && selected.IsSeed())
        {
            if (showDebug) Debug.Log($"[PlayerTileInteractor] Attempting to plant seed '{selected.GetDisplayName()}' at cell {cellPos}.");
            bool planted = PlantPlacementManager.Instance?.TryPlantSeedFromInventory(selected, cellPos, cellCenter) ?? false;
            if (planted)
            {
                if (showDebug) Debug.Log($"[PlayerTileInteractor] Planted successfully. Removing seed from inventory.");
                RemoveSeedFromInventory(selected);
                inventoryBar.ShowBar(); // Refresh the bar
            }
            else
            {
                if (showDebug) Debug.Log($"[PlayerTileInteractor] Planting failed.");
            }
        }
    }

    private void RemoveSeedFromInventory(InventoryBarItem seed)
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

        // Fallback search by NodeData ID
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