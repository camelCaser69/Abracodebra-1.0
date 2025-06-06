// FILE: Assets/Scripts/Tiles/Data/PlayerTileInteractor.cs
using UnityEngine;

public class PlayerTileInteractor : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = false;
    
    private InventoryBarController inventoryBar;

    private void Awake()
    {
        // Will be set when InventoryBarController is initialized
    }
    
    private void Start()
    {
        inventoryBar = InventoryBarController.Instance;
        if (inventoryBar == null && showDebugMessages)
        {
            Debug.LogWarning("[PlayerTileInteractor] InventoryBarController instance not found!");
        }
    }

    void Update()
    {
        // Only handle input during Growth & Threat phase
        if (RunManager.Instance == null || RunManager.Instance.CurrentState != RunState.GrowthAndThreat)
            return;
    
        // Debug mouse position
        if (showDebugMessages && Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;
            Debug.Log($"[PlayerTileInteractor] Mouse clicked at world position: {mouseWorldPos}");
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }
    }
    
    private void HandleLeftClick()
    {
        if (TileInteractionManager.Instance == null)
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
    
        if (selectedItem.Type == InventoryBarItem.ItemType.Tool)
        {
            // Handle tool usage directly
            if (showDebugMessages) Debug.Log($"Using tool: {selectedItem.GetDisplayName()}");
            TileInteractionManager.Instance.ApplyToolAction(selectedItem.ToolDefinition);
        }
        else if (selectedItem.Type == InventoryBarItem.ItemType.Node && selectedItem.IsSeed())
        {
            // Handle seed planting directly
            if (showDebugMessages) Debug.Log($"Planting seed: {selectedItem.GetDisplayName()}");
            HandleSeedPlanting(selectedItem);
        }
        else
        {
            if (showDebugMessages) Debug.Log($"Selected item '{selectedItem.GetDisplayName()}' cannot be used for tile interaction.");
        }
    }
    
    private void HandleSeedPlanting(InventoryBarItem seedItem)
    {
        if (PlantPlacementManager.Instance == null)
        {
            Debug.LogError("PlantPlacementManager not found!");
            return;
        }

        // Add detailed debug info
        if (showDebugMessages)
        {
            Debug.Log($"[PlayerTileInteractor] HandleSeedPlanting called with seed: {seedItem.GetDisplayName()}");
            Debug.Log($"[PlayerTileInteractor] Seed NodeData exists: {seedItem.NodeData != null}");
            Debug.Log($"[PlayerTileInteractor] Seed NodeDefinition exists: {seedItem.NodeDefinition != null}");
            Debug.Log($"[PlayerTileInteractor] Seed ViewGameObject exists: {seedItem.ViewGameObject != null}");
            if (seedItem.NodeData != null)
            {
                Debug.Log($"[PlayerTileInteractor] Seed internal sequence count: {seedItem.NodeData.storedSequence?.nodes?.Count ?? 0}");
            }
        }

        bool success = PlantPlacementManager.Instance.TryPlantSeedFromInventory(seedItem);

        if (success)
        {
            if (showDebugMessages)
                Debug.Log($"Successfully planted seed: {seedItem.GetDisplayName()}");
        
            RemoveSeedFromInventory(seedItem);
        
            // Refresh the inventory bar to show updated inventory
            if (inventoryBar != null)
            {
                inventoryBar.ShowBar();
            }
        }
        else
        {
            if (showDebugMessages)
                Debug.Log($"Failed to plant seed: {seedItem.GetDisplayName()}");
        }
    }
    
    private void RemoveSeedFromInventory(InventoryBarItem seedItem)
{
    if (seedItem?.NodeData == null) return;

    var inventoryController = InventoryGridController.Instance;
    if (inventoryController == null) return;

    // If we have the ViewGameObject reference, use it to find the exact cell
    if (seedItem.ViewGameObject != null)
    {
        for (int i = 0; i < inventoryController.TotalSlots; i++)
        {
            var cell = inventoryController.GetInventoryCellAtIndex(i);
            if (cell != null && cell.HasNode())
            {
                var nodeView = cell.GetNodeView();
                if (nodeView != null && nodeView.gameObject == seedItem.ViewGameObject)
                {
                    // Found the exact match
                    cell.RemoveNode();
                    Debug.Log($"[PlayerTileInteractor] Removed planted seed '{seedItem.GetDisplayName()}' from inventory slot {i}");
                    
                    // Refresh the inventory bar
                    if (inventoryBar != null)
                    {
                        inventoryBar.ShowBar();
                    }
                    return;
                }
            }
        }
    }
    
    // Fallback: Find by NodeData properties
    Debug.Log($"[PlayerTileInteractor] ViewGameObject not found, searching by properties for seed '{seedItem.GetDisplayName()}'");
    
    for (int i = 0; i < inventoryController.TotalSlots; i++)
    {
        var cell = inventoryController.GetInventoryCellAtIndex(i);
        if (cell != null && cell.HasNode())
        {
            var cellNodeData = cell.GetNodeData();
            if (cellNodeData != null && 
                cellNodeData.nodeDisplayName == seedItem.NodeData.nodeDisplayName &&
                cellNodeData.IsSeed())
            {
                // Remove the seed from inventory
                cell.RemoveNode();
                Debug.Log($"[PlayerTileInteractor] Removed planted seed '{seedItem.GetDisplayName()}' from inventory slot {i} (by properties)");
                
                // Refresh the inventory bar
                if (inventoryBar != null)
                {
                    inventoryBar.ShowBar();
                }
                return;
            }
        }
    }

    Debug.LogWarning($"[PlayerTileInteractor] Could not find seed '{seedItem.GetDisplayName()}' in inventory to remove");
}
}