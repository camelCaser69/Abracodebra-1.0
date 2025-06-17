using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryBarController : MonoBehaviour
{
    public static InventoryBarController Instance { get; private set; }

    [Header("Layout")]
    [SerializeField] private int slotsPerRow = 10;
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] private float cellMargin = 10f;
    
    [Header("UI References")]
    [SerializeField] private Transform cellContainer;
    [SerializeField] private Button upArrowButton;
    [SerializeField] private Button downArrowButton;
    [SerializeField] private GameObject selectionHighlight;

    [Header("Visuals")]
    [SerializeField] private Sprite emptyCellSprite;
    [SerializeField] private Color emptyCellColor = Color.white;
    
    [Header("Dependencies")]
    [SerializeField] private InventoryGridController inventoryGridController;

    private readonly List<NodeCell> barCells = new List<NodeCell>();
    private int currentRow = 0;
    private int selectedSlot = 0;
    private int totalRows = 0;

    public InventoryBarItem SelectedItem { get; private set; }
    public event System.Action<InventoryBarItem> OnSelectionChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        selectedSlot = 0;
    }

    void Start()
    {
        if (upArrowButton != null) upArrowButton.onClick.AddListener(() => ChangeRow(-1));
        if (downArrowButton != null) downArrowButton.onClick.AddListener(() => ChangeRow(1));
        
        SetupBarCells();
        gameObject.SetActive(false); // Start hidden
    }

    void OnDestroy()
    {
        if (upArrowButton != null) upArrowButton.onClick.RemoveAllListeners();
        if (downArrowButton != null) downArrowButton.onClick.RemoveAllListeners();
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        HandleNumberKeyInput();
        HandleArrowKeyInput();
    }

    public void ShowBar()
    {
        if (inventoryGridController == null) return;
        if (selectedSlot < 0 && slotsPerRow > 0) selectedSlot = 0;
        RefreshFromInventory();
        gameObject.SetActive(true);
        UpdateBarDisplay();
        UpdateSelection();
    }

    public void HideBar()
    {
        gameObject.SetActive(false);
        if (selectionHighlight != null) selectionHighlight.SetActive(false);
        UniversalTooltipManager.Instance?.HideTooltip();
    }
    
    void SetupBarCells() {
        foreach (Transform child in cellContainer) Destroy(child.gameObject);
        barCells.Clear();

        GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        gridLayout.cellSize = cellSize;
        gridLayout.spacing = new Vector2(cellMargin, cellMargin);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = slotsPerRow;

        for (int i = 0; i < slotsPerRow; i++) {
            GameObject cellGO = new GameObject($"BarCell_{i}", typeof(RectTransform));
            cellGO.transform.SetParent(cellContainer, false);
            Image cellImage = cellGO.AddComponent<Image>();
            cellImage.sprite = emptyCellSprite;
        
            // Use the inventory's empty cell color for consistency
            if (inventoryGridController != null) {
                cellImage.color = inventoryGridController.EmptyCellColor;
            } else {
                cellImage.color = emptyCellColor;
            }
        
            NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
            cellLogic.Init(i, inventoryGridController, cellImage);
            barCells.Add(cellLogic);
        }
    }
    
    private void RefreshFromInventory()
    {
        if (inventoryGridController == null) return;
        totalRows = Mathf.Max(1, Mathf.CeilToInt((float)inventoryGridController.TotalSlots / inventoryGridController.inventoryColumns));
        currentRow = Mathf.Clamp(currentRow, 0, totalRows - 1);
    }
    
    void UpdateBarDisplay() {
        if (inventoryGridController == null || barCells == null) return;
    
        // Clear all bar cells first and reset their colors
        foreach (var cell in barCells) { 
            cell.RemoveNode(); 
        
            // Reset to empty cell color
            Image cellImage = cell.GetComponent<Image>();
            if (cellImage != null && inventoryGridController != null) {
                cellImage.color = inventoryGridController.EmptyCellColor;
            }
        }

        int startIndexInMainInventory = currentRow * inventoryGridController.inventoryColumns;

        for (int i = 0; i < slotsPerRow; i++) {
            if (i >= inventoryGridController.inventoryColumns) break;
            int inventoryIndexToDisplay = startIndexInMainInventory + i;
            if (inventoryIndexToDisplay >= inventoryGridController.TotalSlots) continue;

            NodeCell inventoryCell = inventoryGridController.GetInventoryCellAtIndex(inventoryIndexToDisplay);
            if (inventoryCell != null && inventoryCell.HasItem()) {
                CopyInventoryItemToBarCell(inventoryCell, barCells[i]);
            }
        }

        if (upArrowButton != null) upArrowButton.interactable = currentRow > 0;
        if (downArrowButton != null) downArrowButton.interactable = currentRow < totalRows - 1;
    }
    
    void CopyInventoryItemToBarCell(NodeCell inventoryCell, NodeCell barCell) {
        NodeData nodeData = inventoryCell.GetNodeData();
        ToolDefinition toolDef = inventoryCell.GetToolDefinition();
        NodeDefinition nodeDef = inventoryCell.GetNodeDefinition();

        GameObject display = new GameObject("DisplayItem", typeof(RectTransform), typeof(Image));
        display.transform.SetParent(barCell.transform, false);
        Image displayImage = display.GetComponent<Image>();
        displayImage.raycastTarget = false;

        if (toolDef != null) {
            displayImage.sprite = toolDef.icon;
            displayImage.color = toolDef.iconTint;
        }
        else if (nodeDef != null) {
            displayImage.sprite = nodeDef.thumbnail;
            displayImage.color = nodeDef.thumbnailTintColor;
        }

        display.transform.localScale = Vector3.one * InventoryGridController.Instance.NodeGlobalImageScale;
        display.GetComponent<RectTransform>().sizeDelta = cellSize * 0.8f;

        // Apply the cell background tint
        if (barCell.GetComponent<Image>() != null && InventoryColorManager.Instance != null) {
            Color cellColor = InventoryColorManager.Instance.GetCellColorForItem(nodeData, nodeDef, toolDef);
            barCell.GetComponent<Image>().color = cellColor;
        }

        barCell.AssignDisplayOnly(display, nodeData, toolDef);
    }

    void CreateToolCopyInBarCell(ToolDefinition toolDef, NodeData toolWrapperNodeData, NodeCell barCell)
    {
        GameObject toolDisplay = new GameObject($"ToolDisplay_{toolDef.displayName}", typeof(RectTransform), typeof(Image));
        toolDisplay.transform.SetParent(barCell.transform, false);

        Image toolImage = toolDisplay.GetComponent<Image>();
        toolImage.sprite = toolDef.icon;
        toolImage.color = toolDef.iconTint;
        toolImage.raycastTarget = false;

        float globalScale = 1f;
        if (InventoryGridController.Instance != null)
        {
            globalScale = InventoryGridController.Instance.NodeGlobalImageScale;
        }
        toolDisplay.transform.localScale = new Vector3(globalScale, globalScale, 1f);

        RectTransform toolRect = toolDisplay.GetComponent<RectTransform>();
        toolRect.anchoredPosition = Vector2.zero;
        toolRect.sizeDelta = cellSize * 0.8f;

        barCell.AssignDisplayOnly(toolDisplay, toolWrapperNodeData, toolDef);
    }

    void CreateNodeCopyInBarCell(NodeDefinition nodeDef, NodeData originalNodeData, NodeCell barCell)
    {
        GameObject nodeDisplay = new GameObject($"NodeDisplay_{nodeDef.displayName}", typeof(RectTransform), typeof(Image));
        nodeDisplay.transform.SetParent(barCell.transform, false);

        Image nodeImage = nodeDisplay.GetComponent<Image>();
        nodeImage.sprite = nodeDef.thumbnail;
        nodeImage.color = nodeDef.thumbnailTintColor;
        nodeImage.raycastTarget = false;

        float globalScale = 1f;
        if (InventoryGridController.Instance != null)
        {
            globalScale = InventoryGridController.Instance.NodeGlobalImageScale;
        }
        nodeDisplay.transform.localScale = new Vector3(globalScale, globalScale, 1f);

        RectTransform nodeRect = nodeDisplay.GetComponent<RectTransform>();
        nodeRect.anchoredPosition = Vector2.zero;
        nodeRect.sizeDelta = cellSize * 0.8f;

        barCell.AssignDisplayOnly(nodeDisplay, originalNodeData, null);
    }


    void HandleNumberKeyInput()
    {
        for (int i = 1; i <= 9; i++)
        {
            KeyCode key = (KeyCode)((int)KeyCode.Alpha1 + (i - 1));
            if (Input.GetKeyDown(key))
            {
                int targetSlotIndexInBar = i - 1; // 1-9 maps to slots 0-8
                if (targetSlotIndexInBar < slotsPerRow)
                {
                    SelectSlot(targetSlotIndexInBar);
                }
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            int targetSlotIndexInBar = 9; // 0 maps to slot 9
            if (targetSlotIndexInBar < slotsPerRow)
            {
                SelectSlot(targetSlotIndexInBar);
            }
        }
    }

    void HandleArrowKeyInput()
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

    void ChangeRow(int direction)
    {
        int newRow = currentRow + direction;
        if (newRow >= 0 && newRow < totalRows)
        {
            currentRow = newRow;
            UpdateBarDisplay();
            UpdateSelection();
        }
    }

    void SelectSlot(int slotIndexInBar)
    {
        // If the pressed key corresponds to the already selected slot and an item is indeed selected
        if (this.selectedSlot == slotIndexInBar && this.SelectedItem != null)
        {
            this.selectedSlot = -1; // Mark as deselected
        }
        else
        {
            // Otherwise, select the new slot
            this.selectedSlot = Mathf.Clamp(slotIndexInBar, 0, slotsPerRow - 1);
        }
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        SelectedItem = null; // Reset

        if (selectedSlot == -1) // Explicitly deselected
        {
            // Do nothing, SelectedItem is already null
        }
        else if (selectedSlot >= 0 && selectedSlot < barCells.Count && inventoryGridController != null)
        {
            int mainInventoryColumns = inventoryGridController.inventoryColumns;
            if (mainInventoryColumns > 0)
            {
                int inventoryIndex = currentRow * mainInventoryColumns + selectedSlot;
                if (inventoryIndex >= 0 && inventoryIndex < inventoryGridController.TotalSlots)
                {
                    InventoryBarItem itemFromMainInventory = inventoryGridController.GetItemAtIndex(inventoryIndex);
                    if (itemFromMainInventory != null && itemFromMainInventory.IsValid())
                    {
                        SelectedItem = itemFromMainInventory;
                    }
                }
            }
        }

        // Update the visual selection highlight
        if (selectionHighlight != null)
        {
            if (SelectedItem != null && SelectedItem.IsValid() && selectedSlot >= 0 && selectedSlot < barCells.Count)
            {
                if (gameObject.activeInHierarchy)
                {
                    NodeCell targetBarCellForHighlight = barCells[selectedSlot];
                    RectTransform cellRect = targetBarCellForHighlight.GetComponent<RectTransform>();

                    if (cellRect != null)
                    {
                        selectionHighlight.SetActive(true);
                        selectionHighlight.transform.SetParent(cellRect.transform, false);
                        RectTransform highlightRect = selectionHighlight.GetComponent<RectTransform>();
                        if (highlightRect != null)
                        {
                            highlightRect.anchoredPosition = Vector2.zero;
                            highlightRect.sizeDelta = cellRect.sizeDelta;
                        }
                    }
                    else { selectionHighlight.SetActive(false); }
                }
                else { selectionHighlight.SetActive(false); }
            }
            else // No valid item selected or slot is -1
            {
                selectionHighlight.SetActive(false);
            }
        }

        // --- MODIFICATION START ---
        // Update the tooltip based on the new selection
        if (gameObject.activeInHierarchy && SelectedItem != null && SelectedItem.IsValid() && UniversalTooltipManager.Instance != null && selectedSlot >= 0 && selectedSlot < barCells.Count)
        {
            ITooltipDataProvider provider = (SelectedItem.Type == InventoryBarItem.ItemType.Node)
                ? (ITooltipDataProvider)SelectedItem.NodeDefinition
                : SelectedItem.ToolDefinition;
            
            object sourceData = (SelectedItem.Type == InventoryBarItem.ItemType.Node)
                ? SelectedItem.NodeData
                : null;

            UniversalTooltipManager.Instance.ShowTooltip(provider, barCells[selectedSlot].transform, sourceData);
        }
        else if (UniversalTooltipManager.Instance != null)
        {
            UniversalTooltipManager.Instance.HideTooltip();
        }
        // --- MODIFICATION END ---
        
        OnSelectionChanged?.Invoke(SelectedItem);
    }

}