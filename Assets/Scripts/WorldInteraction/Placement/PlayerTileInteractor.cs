using UnityEngine;
using System.Linq;

public sealed class PlayerTileInteractor : MonoBehaviour
{
    [SerializeField] InventoryBarController inventoryBar;
    [SerializeField] TileInteractionManager tileInteractionManager;
    [SerializeField] Transform playerTransform;
    [SerializeField] bool showDebug = false;

    bool pendingClick;

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

    void HandleLeftClick()
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

            // --- START: MODIFIED BLOCK ---
            // Create a callback that will be executed ONLY on successful planting
            System.Action onSuccess = () => {
                if (showDebug) Debug.Log($"[PlayerTileInteractor] Planting was successful. Removing seed '{selected.GetDisplayName()}' from inventory via callback.");
                RemoveSeedFromInventory(selected);
                inventoryBar.ShowBar(); // Refresh the bar after item is removed
            };

            // Pass the action and the callback to the manager
            PlayerActionManager.Instance.ExecutePlayerAction(
                PlayerActionType.PlantSeed,
                cellPos,
                selected,
                onSuccess
            );
            // --- END: MODIFIED BLOCK ---
        }
    }

    public void RemoveSeedFromInventory(InventoryBarItem seed)
    {
        InventoryGridController grid = InventoryGridController.Instance;
        if (grid == null) return;

        // Try to find by direct GameObject reference first (most reliable)
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

        // Fallback to finding by NodeData ID if view reference fails or is null
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

    bool EnsureManagers()
    {
        if (tileInteractionManager == null || inventoryBar == null) FindSingletons();
        return tileInteractionManager != null && inventoryBar != null;
    }

    void FindSingletons()
    {
        if (inventoryBar == null) inventoryBar = InventoryBarController.Instance ?? FindAnyObjectByType<InventoryBarController>(FindObjectsInactive.Include);
        if (tileInteractionManager == null) tileInteractionManager = TileInteractionManager.Instance ?? FindAnyObjectByType<TileInteractionManager>(FindObjectsInactive.Include);
    }
}