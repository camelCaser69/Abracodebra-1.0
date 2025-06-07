using UnityEngine;

[DefaultExecutionOrder(100)] // Ensures LateUpdate runs after TileInteractionManager.Update
public sealed class PlayerTileInteractor : MonoBehaviour
{
    [Header("Scene References (optional)")]
    [Tooltip("InventoryBarController in the scene. If left empty, the script will auto-locate it.")]
    [SerializeField] private InventoryBarController inventoryBar;

    [Tooltip("TileInteractionManager in the scene. If left empty, the script will auto-locate it.")]
    [SerializeField] private TileInteractionManager tileInteractionManager;

    [Tooltip("Transform used to measure distance from player to target tile. Defaults to this transform.")]
    [SerializeField] private Transform playerTransform;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    private bool pendingClick;

    void Awake()
    {
        if (playerTransform == null) playerTransform = transform;
    }

    void Start()
    {
        FindSingletons();
        if (showDebug)
        {
            if (inventoryBar == null)
                Debug.LogWarning("[PTI] InventoryBarController not found in scene.");
            if (tileInteractionManager == null)
                Debug.LogWarning("[PTI] TileInteractionManager not found in scene.");
        }
    }

    void Update()
    {
        if (RunManager.Instance == null ||
            RunManager.Instance.CurrentState != RunState.GrowthAndThreat)
            return;

        if (Input.GetMouseButtonDown(0))
            pendingClick = true;
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
            if (showDebug) Debug.Log("[PTI] Left-click ignored – nothing selected.");
            return;
        }

        Vector3 mouseW = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseW.z = 0f;
        Vector3Int cellPos = tileInteractionManager.WorldToCell(mouseW);
        Vector3 cellCenter = tileInteractionManager.interactionGrid.GetCellCenterWorld(cellPos);

        float dist = Vector2.Distance(playerTransform.position, cellCenter);
        if (dist > tileInteractionManager.hoverRadius)
        {
            if (showDebug) Debug.Log($"[PTI] Cell too far ({dist:F2}>{tileInteractionManager.hoverRadius}).");
            return;
        }

        // TOOL
        if (selected.Type == InventoryBarItem.ItemType.Tool)
        {
            if (showDebug) Debug.Log($"[PTI] Using tool “{selected.GetDisplayName()}”.");
            tileInteractionManager.ApplyToolAction(selected.ToolDefinition);
            return;
        }

        // SEED
        if (selected.Type == InventoryBarItem.ItemType.Node && selected.IsSeed())
        {
            if (showDebug) Debug.Log($"[PTI] Planting seed “{selected.GetDisplayName()}”.");
            bool planted = PlantPlacementManager.Instance != null &&
                           PlantPlacementManager.Instance.TryPlantSeedFromInventory(
                               selected, cellPos, cellCenter);

            if (planted)
            {
                RemoveSeedFromInventory(selected);
                inventoryBar.ShowBar();
            }
            else if (showDebug)
            {
                Debug.Log("[PTI] PlantPlacementManager returned FALSE.");
            }
            return;
        }

        if (showDebug)
            Debug.Log($"[PTI] Item “{selected.GetDisplayName()}” is not usable.");
    }

    void RemoveSeedFromInventory(InventoryBarItem seed)
    {
        InventoryGridController grid = InventoryGridController.Instance;
        if (grid == null) return;

        if (seed.ViewGameObject != null)
        {
            for (int i = 0; i < grid.ActualCellCount; i++)
            {
                NodeCell cell = grid.GetInventoryCellAtIndex(i);
                if (cell != null && cell.GetNodeView()?.gameObject == seed.ViewGameObject)
                {
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
                cell.RemoveNode();
                return;
            }
        }
    }

    bool EnsureManagers()
    {
        if (tileInteractionManager == null || inventoryBar == null)
            FindSingletons();

        return tileInteractionManager != null && inventoryBar != null;
    }

    void FindSingletons()
    {
        if (inventoryBar == null)
        {
            inventoryBar = InventoryBarController.Instance
                           ?? FindAnyObjectByType<InventoryBarController>(FindObjectsInactive.Include);
        }

        if (tileInteractionManager == null)
        {
            tileInteractionManager = TileInteractionManager.Instance
                                     ?? FindAnyObjectByType<TileInteractionManager>(FindObjectsInactive.Include);
        }
    }
}
