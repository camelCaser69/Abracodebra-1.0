// FILE: Assets/Scripts/Tiles/Data/PlayerTileInteractor.cs
using UnityEngine;

public class PlayerTileInteractor : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = false;
    
    private InventoryBarController inventoryBar;
    private TileInteractionManager tileInteractionManager; 
    private Transform playerTransform; 

    private void Awake()
    {
        playerTransform = transform; 
    }
    
    private void Start()
    {
        inventoryBar = InventoryBarController.Instance;
        if (inventoryBar == null && showDebugMessages)
        {
            Debug.LogWarning("[PlayerTileInteractor] InventoryBarController instance not found!");
        }
        tileInteractionManager = TileInteractionManager.Instance;
        if (tileInteractionManager == null && showDebugMessages)
        {
            Debug.LogWarning("[PlayerTileInteractor] TileInteractionManager instance not found!");
        }
    }

    void Update()
    {
        if (RunManager.Instance == null || RunManager.Instance.CurrentState != RunState.GrowthAndThreat)
            return;
        
        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }
    }
    
    private void HandleLeftClick()
    {
        if (tileInteractionManager == null)
        {
            Debug.LogError("No TileInteractionManager in scene!");
            return;
        }
    
        if (inventoryBar == null)
        {
            inventoryBar = InventoryBarController.Instance; 
            if (inventoryBar == null)
            {
                if (showDebugMessages) Debug.LogWarning("No InventoryBarController available!");
                return;
            }
        }
    
        var selectedItem = inventoryBar.SelectedItem;
    
        if (showDebugMessages) 
        {
            Debug.Log($"[PlayerTileInteractor] HandleLeftClick - SelectedItem: {selectedItem?.GetDisplayName() ?? "NULL"}");
            if (selectedItem != null)
            {
                Debug.Log($"[PlayerTileInteractor] Item Type: {selectedItem.Type}, IsSeed: {selectedItem.IsSeed()}, IsValid: {selectedItem.IsValid()}");
            }
        }
    
        if (selectedItem == null || !selectedItem.IsValid())
        {
            if (showDebugMessages) Debug.Log("No valid item selected in inventory bar.");
            return;
        }

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector3Int gridPosition = tileInteractionManager.WorldToCell(mouseWorldPos);
        Vector3 cellCenterWorld = tileInteractionManager.interactionGrid.GetCellCenterWorld(gridPosition);

        float distanceToCell = Vector2.Distance(playerTransform.position, cellCenterWorld);
        float interactionRadius = tileInteractionManager.hoverRadius; 

        if (distanceToCell > interactionRadius)
        {
            if (showDebugMessages) Debug.Log($"[PlayerTileInteractor] Clicked cell {gridPosition} is too far ({distanceToCell:F2} > {interactionRadius:F2}). Action aborted.");
            return;
        }
        if (showDebugMessages) Debug.Log($"[PlayerTileInteractor] Clicked cell {gridPosition} (World: {cellCenterWorld}) is within range ({distanceToCell:F2} <= {interactionRadius:F2}).");
    
        if (selectedItem.Type == InventoryBarItem.ItemType.Tool)
        {
            if (showDebugMessages) Debug.Log($"Using tool: {selectedItem.GetDisplayName()} on tile {gridPosition}");
            tileInteractionManager.ApplyToolAction(selectedItem.ToolDefinition);
        }
        else if (selectedItem.Type == InventoryBarItem.ItemType.Node && selectedItem.IsSeed())
        {
            if (showDebugMessages) Debug.Log($"Attempting to plant seed: {selectedItem.GetDisplayName()} on tile {gridPosition}");
            HandleSeedPlanting(selectedItem, gridPosition, cellCenterWorld); 
        }
        else
        {
            if (showDebugMessages) Debug.Log($"Selected item '{selectedItem.GetDisplayName()}' cannot be used for tile interaction at {gridPosition}.");
        }
    }
    
    private void HandleSeedPlanting(InventoryBarItem seedItem, Vector3Int gridPosition, Vector3 worldPosition)
    {
        if (PlantPlacementManager.Instance == null)
        {
            Debug.LogError("PlantPlacementManager not found!");
            return;
        }

        if (showDebugMessages)
        {
            Debug.Log($"[PlayerTileInteractor] HandleSeedPlanting for seed '{seedItem.GetDisplayName()}' at grid: {gridPosition}, world: {worldPosition}");
            if (seedItem.NodeData != null)
            {
                Debug.Log($"  Seed ID: {seedItem.NodeData.nodeId}, Stored Seq Count: {seedItem.NodeData.storedSequence?.nodes?.Count ?? 0}");
                 if (seedItem.NodeData.storedSequence?.nodes != null)
                 {
                     foreach(var nodeInSeq in seedItem.NodeData.storedSequence.nodes)
                     {
                         if (nodeInSeq != null) Debug.Log($"    - Seq Node: {nodeInSeq.nodeDisplayName}, Effects: {nodeInSeq.effects?.Count ?? 0}");
                     }
                 }
            }
        }

        bool success = PlantPlacementManager.Instance.TryPlantSeedFromInventory(seedItem, gridPosition, worldPosition);

        if (success)
        {
            if (showDebugMessages) Debug.Log($"Successfully planted seed: {seedItem.GetDisplayName()}");
            RemoveSeedFromInventory(seedItem);
            if (inventoryBar != null) inventoryBar.ShowBar(); 
        }
        else
        {
            if (showDebugMessages) Debug.Log($"Failed to plant seed: {seedItem.GetDisplayName()} at {gridPosition}. Check PlantPlacementManager logs.");
        }
    }
    
    private void RemoveSeedFromInventory(InventoryBarItem seedItem)
    {
        if (seedItem?.NodeData == null)
        {
            if (showDebugMessages) Debug.LogWarning("[PlayerTileInteractor] RemoveSeedFromInventory: seedItem or NodeData is null.");
            return;
        }
    
        var inventoryController = InventoryGridController.Instance;
        if (inventoryController == null)
        {
            if (showDebugMessages) Debug.LogWarning("[PlayerTileInteractor] RemoveSeedFromInventory: InventoryGridController is null.");
            return;
        }
    
        if (seedItem.ViewGameObject != null)
        {
            for (int i = 0; i < inventoryController.ActualCellCount; i++) 
            {
                var cell = inventoryController.GetInventoryCellAtIndex(i);
                if (cell != null && cell.HasNode())
                {
                    var nodeView = cell.GetNodeView();
                    if (nodeView != null && nodeView.gameObject == seedItem.ViewGameObject)
                    {
                        if (showDebugMessages) Debug.Log($"[PlayerTileInteractor] Removing seed '{seedItem.GetDisplayName()}' from inventory slot {i} (matched by ViewGameObject: {seedItem.ViewGameObject.name}).");
                        cell.RemoveNode(); 
                        return;
                    }
                }
            }
            if (showDebugMessages) Debug.LogWarning($"[PlayerTileInteractor] Could not find seed '{seedItem.GetDisplayName()}' by ViewGameObject. Falling back to NodeData ID search.");
        }
    
        for (int i = 0; i < inventoryController.ActualCellCount; i++) 
        {
            var cell = inventoryController.GetInventoryCellAtIndex(i);
            if (cell != null && cell.HasNode())
            {
                var cellNodeData = cell.GetNodeData();
                if (cellNodeData != null && cellNodeData.nodeId == seedItem.NodeData.nodeId && cellNodeData.IsSeed())
                {
                    if (showDebugMessages) Debug.Log($"[PlayerTileInteractor] Removing seed '{seedItem.GetDisplayName()}' (ID: {seedItem.NodeData.nodeId}) from inventory slot {i} (matched by NodeData ID).");
                    cell.RemoveNode();
                    return;
                }
            }
        }
    
        if (showDebugMessages) Debug.LogWarning($"[PlayerTileInteractor] Could not find seed '{seedItem.GetDisplayName()}' (ID: {seedItem.NodeData.nodeId}) in inventory to remove after planting.");
    }
}