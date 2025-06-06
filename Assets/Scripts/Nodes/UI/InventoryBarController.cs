// FILE: Assets/Scripts/Nodes/UI/InventoryBarController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryBarController : MonoBehaviour
{
    public static InventoryBarController Instance { get; private set; }
    
    [Header("Bar Configuration")]
    [SerializeField] private int slotsPerRow = 10;
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] private float cellMargin = 10f;
    
    [Header("UI References")]
    [SerializeField] private Transform cellContainer;
    [SerializeField] private Button upArrowButton;
    [SerializeField] private Button downArrowButton;
    [SerializeField] private GameObject selectionHighlight; // Keep only this one

    [Header("Cell Visuals")]
    [SerializeField] private Sprite emptyCellSprite;
    [SerializeField] private Color emptyCellColor = Color.white;

    [Header("Integration")]
    [SerializeField] private InventoryGridController inventoryGridController;
    
    private List<NodeCell> barCells = new List<NodeCell>();
    private int currentRow = 0;
    private int selectedSlot = 0;
    private int totalRows = 0;
    
    public InventoryBarItem SelectedItem { get; private set; }
    public event System.Action<InventoryBarItem> OnSelectionChanged;
    
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    
    void Start()
    {
        if (upArrowButton != null) upArrowButton.onClick.AddListener(() => ChangeRow(-1));
        if (downArrowButton != null) downArrowButton.onClick.AddListener(() => ChangeRow(1));
        
        SetupBarCells();
        gameObject.SetActive(false); // Start hidden
    }
    
    void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        HandleNumberKeyInput();
        HandleArrowKeyInput();
    }
    
    public void ShowBar()
    {
        if (inventoryGridController == null)
        {
            Debug.LogError("[InventoryBarController] InventoryGridController reference is missing!");
            return;
        }
        
        RefreshFromInventory();
        gameObject.SetActive(true);
        UpdateBarDisplay();
        UpdateSelection();
    }
    
    public void HideBar()
    {
        gameObject.SetActive(false);
    }
    
    private void SetupBarCells()
    {
        if (cellContainer == null) 
        {
            Debug.LogError("[InventoryBarController] Cell Container not assigned!");
            return;
        }
        
        // Clear existing cells
        foreach (Transform child in cellContainer) Destroy(child.gameObject);
        barCells.Clear();
        
        // Setup grid layout
        GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null) gridLayout = cellContainer.gameObject.AddComponent<GridLayoutGroup>();
        
        gridLayout.cellSize = cellSize;
        gridLayout.spacing = new Vector2(cellMargin, cellMargin);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = slotsPerRow;
        
        // Create bar cells (same as inventory cells)
        for (int i = 0; i < slotsPerRow; i++)
        {
            GameObject cellGO = new GameObject($"BarCell_{i}", typeof(RectTransform));
            cellGO.transform.SetParent(cellContainer, false);
            
            Image cellImage = cellGO.AddComponent<Image>();
            cellImage.sprite = emptyCellSprite;
            cellImage.color = emptyCellColor;
            cellImage.raycastTarget = true;
            
            NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
            cellLogic.Init(i, null, inventoryGridController, cellImage); // Use inventory controller for shared logic
            
            barCells.Add(cellLogic);
        }
    }
    
    private void RefreshFromInventory()
    {
        if (inventoryGridController == null) return;
    
        // FIXED: Calculate total rows based on actual inventory content
        int totalInventorySlots = inventoryGridController.TotalSlots;
        int inventoryColumns = inventoryGridController.inventoryColumns;
    
        // Ensure inventoryColumns is valid
        if (inventoryColumns <= 0)
        {
            Debug.LogError("[InventoryBarController] Invalid inventory columns count!");
            totalRows = 1;
            currentRow = 0;
            return;
        }
    
        totalRows = Mathf.Max(1, Mathf.CeilToInt((float)totalInventorySlots / inventoryColumns));
    
        // Clamp current row to valid range
        currentRow = Mathf.Clamp(currentRow, 0, totalRows - 1);
    
        Debug.Log($"[InventoryBarController] RefreshFromInventory: TotalSlots={totalInventorySlots}, Columns={inventoryColumns}, TotalRows={totalRows}, CurrentRow={currentRow}");
    }
    
    private void UpdateBarDisplay()
    {
        if (inventoryGridController == null) return;
    
        // Clear all bar cells first
        foreach (var cell in barCells)
        {
            cell.RemoveNode();
        }
    
        // FIXED: Calculate proper inventory access
        int columnsPerRow = inventoryGridController.inventoryColumns;
        int startIndex = currentRow * columnsPerRow;
        int maxInventoryIndex = inventoryGridController.TotalSlots - 1;
    
        Debug.Log($"[InventoryBarController] UpdateBarDisplay: CurrentRow={currentRow}, ColumnsPerRow={columnsPerRow}, StartIndex={startIndex}, MaxIndex={maxInventoryIndex}");
    
        // Populate current row from inventory
        for (int i = 0; i < slotsPerRow && i < columnsPerRow; i++)
        {
            int inventoryIndex = startIndex + i;
        
            // FIXED: Bounds check before accessing inventory
            if (inventoryIndex > maxInventoryIndex)
            {
                Debug.Log($"[InventoryBarController] Skipping bar slot {i}, inventory index {inventoryIndex} is beyond max {maxInventoryIndex}");
                continue;
            }
        
            var inventoryCell = inventoryGridController.GetInventoryCellAtIndex(inventoryIndex);
        
            if (inventoryCell != null && inventoryCell.HasNode())
            {
                // Copy the content from inventory cell to bar cell
                CopyInventoryItemToBarCell(inventoryCell, barCells[i]);
            }
        }
    
        // Update arrow buttons
        if (upArrowButton != null) upArrowButton.interactable = currentRow > 0;
        if (downArrowButton != null) upArrowButton.interactable = currentRow < totalRows - 1;
    
        Debug.Log($"[InventoryBarController] UpdateBarDisplay: Row {currentRow}/{totalRows-1}, StartIndex={startIndex}");
    }
    
    private void CopyInventoryItemToBarCell(NodeCell inventoryCell, NodeCell barCell)
    {
        var nodeData = inventoryCell.GetNodeData();
        var nodeView = inventoryCell.GetNodeView();
        var toolDef = inventoryCell.GetToolDefinition();
        
        if (nodeData != null)
        {
            if (toolDef != null)
            {
                // It's a tool - create a visual copy
                CreateToolCopyInBarCell(toolDef, nodeData, barCell);
            }
            else if (nodeView != null)
            {
                // It's a node - create a visual copy
                var nodeDef = nodeView.GetNodeDefinition();
                if (nodeDef != null)
                {
                    CreateNodeCopyInBarCell(nodeDef, nodeData, barCell);
                }
            }
        }
    }
    
    private void CreateToolCopyInBarCell(ToolDefinition toolDef, NodeData nodeData, NodeCell barCell)
    {
        // Create a visual representation of the tool (not draggable)
        GameObject toolDisplay = new GameObject($"ToolDisplay_{toolDef.displayName}", typeof(RectTransform), typeof(Image));
        toolDisplay.transform.SetParent(barCell.transform, false);
        
        Image toolImage = toolDisplay.GetComponent<Image>();
        toolImage.sprite = toolDef.icon;
        toolImage.color = toolDef.iconTint;
        toolImage.raycastTarget = false; // Not interactive in bar
        
        RectTransform toolRect = toolDisplay.GetComponent<RectTransform>();
        toolRect.anchoredPosition = Vector2.zero;
        toolRect.sizeDelta = cellSize * 0.8f;
        
        // Store reference in the cell (but don't make it draggable)
        barCell.AssignDisplayOnly(toolDisplay, nodeData, toolDef);
    }
    
    private void CreateNodeCopyInBarCell(NodeDefinition nodeDef, NodeData nodeData, NodeCell barCell)
    {
        // Create a visual representation of the node (not draggable)
        GameObject nodeDisplay = new GameObject($"NodeDisplay_{nodeDef.displayName}", typeof(RectTransform), typeof(Image));
        nodeDisplay.transform.SetParent(barCell.transform, false);
        
        Image nodeImage = nodeDisplay.GetComponent<Image>();
        nodeImage.sprite = nodeDef.thumbnail;
        nodeImage.color = nodeDef.thumbnailTintColor;
        nodeImage.raycastTarget = false; // Not interactive in bar
        
        RectTransform nodeRect = nodeDisplay.GetComponent<RectTransform>();
        nodeRect.anchoredPosition = Vector2.zero;
        nodeRect.sizeDelta = cellSize * 0.8f;
        
        // Store reference in the cell (but don't make it draggable)
        barCell.AssignDisplayOnly(nodeDisplay, nodeData, null);
    }
    
    private void HandleNumberKeyInput()
    {
        // FIXED: Handle both QWERTY and QWERTZ layouts properly
        // Check for number keys 1-9 and 0 (which maps to slot 9)
        for (int i = 1; i <= 9; i++)
        {
            KeyCode key = (KeyCode)((int)KeyCode.Alpha1 + (i - 1));
            if (Input.GetKeyDown(key))
            {
                int targetSlot = i - 1; // 1-9 maps to slots 0-8
                if (targetSlot < slotsPerRow)
                {
                    SelectSlot(targetSlot);
                }
                return;
            }
        }
        
        // Handle 0 key (maps to slot 9)
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            int targetSlot = 9; // 0 maps to slot 9
            if (targetSlot < slotsPerRow)
            {
                SelectSlot(targetSlot);
            }
        }
    }
    
    private void HandleArrowKeyInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            ChangeRow(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            ChangeRow(1);
        }
    }
    
    private void ChangeRow(int direction)
    {
        int newRow = currentRow + direction;
        if (newRow >= 0 && newRow < totalRows)
        {
            currentRow = newRow;
            Debug.Log($"[InventoryBarController] Changed to row {currentRow}");
            UpdateBarDisplay();
            UpdateSelection();
        }
        else
        {
            Debug.Log($"[InventoryBarController] Cannot change to row {newRow}, out of bounds (0-{totalRows-1})");
        }
    }
    
    private void SelectSlot(int slotIndex)
    {
        selectedSlot = Mathf.Clamp(slotIndex, 0, slotsPerRow - 1);
        Debug.Log($"[InventoryBarController] Selected slot {selectedSlot}");
        UpdateSelection();
    }
    
    private void UpdateSelection()
{
    // Update visual selection
    if (selectionHighlight != null)
    {
        if (selectedSlot < barCells.Count)
        {
            selectionHighlight.transform.SetParent(barCells[selectedSlot].transform, false);
            selectionHighlight.transform.SetAsLastSibling();
            selectionHighlight.SetActive(true);
            
            RectTransform highlightRect = selectionHighlight.GetComponent<RectTransform>();
            if (highlightRect != null)
            {
                highlightRect.anchoredPosition = Vector2.zero;
                highlightRect.sizeDelta = cellSize;
            }
        }
    }
    
    // Update selected item from bar cell
    SelectedItem = null;
    if (selectedSlot < barCells.Count)
    {
        var selectedCell = barCells[selectedSlot];
        if (selectedCell.HasNode())
        {
            var nodeData = selectedCell.GetNodeData();
            var toolDef = selectedCell.GetToolDefinition();
            
            if (toolDef != null)
            {
                SelectedItem = InventoryBarItem.FromTool(toolDef);
                Debug.Log($"[InventoryBarController] Selected tool: {toolDef.displayName}");
            }
            else if (nodeData != null)
            {
                // Find the corresponding item from the ACTUAL inventory (not the bar display)
                int inventoryIndex = currentRow * inventoryGridController.inventoryColumns + selectedSlot;
                
                if (inventoryIndex < inventoryGridController.TotalSlots)
                {
                    var inventoryCell = inventoryGridController.GetInventoryCellAtIndex(inventoryIndex);
                    if (inventoryCell != null && inventoryCell.HasNode())
                    {
                        // Get the ACTUAL data from inventory, not the bar copy
                        var inventoryNodeData = inventoryCell.GetNodeData();
                        var inventoryNodeView = inventoryCell.GetNodeView();
                        var inventoryNodeDef = inventoryNodeView?.GetNodeDefinition();
                        
                        if (inventoryNodeData != null && inventoryNodeDef != null)
                        {
                            // Use the inventory's actual NodeData which should have the proper storedSequence
                            SelectedItem = InventoryBarItem.FromNode(inventoryNodeData, inventoryNodeDef, inventoryNodeView?.gameObject);
                            
                            Debug.Log($"[InventoryBarController] Selected node: {inventoryNodeDef.displayName}, IsSeed: {inventoryNodeData.IsSeed()}");
                            if (inventoryNodeData.IsSeed())
                            {
                                Debug.Log($"[InventoryBarController] Seed's stored sequence has {inventoryNodeData.storedSequence?.nodes?.Count ?? 0} nodes");
                            }
                        }
                    }
                }
            }
        }
    }
    
    Debug.Log($"[InventoryBarController] UpdateSelection: SelectedItem = {SelectedItem?.GetDisplayName() ?? "NULL"}, Type = {SelectedItem?.Type.ToString() ?? "N/A"}");
    OnSelectionChanged?.Invoke(SelectedItem);
}
    
    void OnDestroy()
    {
        if (upArrowButton != null) upArrowButton.onClick.RemoveAllListeners();
        if (downArrowButton != null) downArrowButton.onClick.RemoveAllListeners();
    }
}