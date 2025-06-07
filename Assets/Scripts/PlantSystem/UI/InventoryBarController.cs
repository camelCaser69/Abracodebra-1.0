using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class InventoryBarController : MonoBehaviour
{
    public static InventoryBarController Instance { get; set; }

    [SerializeField] int slotsPerRow = 10;
    [SerializeField] Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] float cellMargin = 10f;

    [SerializeField] Transform cellContainer;
    [SerializeField] Button upArrowButton;
    [SerializeField] Button downArrowButton;
    [SerializeField] GameObject selectionHighlight;

    [SerializeField] Sprite emptyCellSprite;
    [SerializeField] Color emptyCellColor = Color.white;

    [SerializeField] InventoryGridController inventoryGridController;

    List<NodeCell> barCells = new List<NodeCell>();
    int currentRow = 0;
    int selectedSlot = 0; // Index within the BAR (0 to slotsPerRow-1), -1 for none selected
    int totalRows = 0;

    public InventoryBarItem SelectedItem { get; private set; }
    public event System.Action<InventoryBarItem> OnSelectionChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        selectedSlot = 0; // Default to selecting the first slot, or -1 if nothing should be pre-selected
    }

    void Start()
    {
        if (upArrowButton != null) upArrowButton.onClick.AddListener(() => ChangeRow(-1));
        if (downArrowButton != null) downArrowButton.onClick.AddListener(() => ChangeRow(1));

        if (inventoryGridController == null)
        {
            Debug.LogError("[InventoryBarController] InventoryGridController reference is missing in Start! Bar may not function correctly.");
        }
        if (selectionHighlight == null)
        {
            Debug.LogWarning("[InventoryBarController] Selection Highlight GameObject is not assigned in the Inspector. Highlighting will not work.");
        }
        else
        {
            selectionHighlight.SetActive(false); // Ensure highlight is also initially hidden
        }

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
            Debug.LogError("[InventoryBarController] InventoryGridController reference is missing! Cannot show bar.");
            return;
        }
        // When showing the bar, if nothing was selected (-1), keep it that way or select slot 0.
        // If a slot was previously selected, try to maintain it.
        // For simplicity, let's ensure selectedSlot is valid or default to 0 if it was -1.
        if (selectedSlot < 0 && slotsPerRow > 0) selectedSlot = 0;


        RefreshFromInventory();
        gameObject.SetActive(true);
        UpdateBarDisplay();
        UpdateSelection();
    }

    public void HideBar()
    {
        gameObject.SetActive(false);

        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(false);
        }

        if (UniversalTooltipManager.Instance != null)
            UniversalTooltipManager.Instance.HideTooltip();
    }

    void SetupBarCells()
    {
        if (cellContainer == null)
        {
            Debug.LogError("[InventoryBarController] Cell Container not assigned!");
            return;
        }

        foreach (Transform child in cellContainer) Destroy(child.gameObject);
        barCells.Clear();

        GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null) gridLayout = cellContainer.gameObject.AddComponent<GridLayoutGroup>();

        gridLayout.cellSize = cellSize;
        gridLayout.spacing = new Vector2(cellMargin, cellMargin);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = slotsPerRow;

        for (int i = 0; i < slotsPerRow; i++)
        {
            GameObject cellGO = new GameObject($"BarCell_{i}", typeof(RectTransform));
            cellGO.transform.SetParent(cellContainer, false);

            Image cellImage = cellGO.AddComponent<Image>();
            cellImage.sprite = emptyCellSprite;
            cellImage.color = emptyCellColor;
            cellImage.raycastTarget = true;

            NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
            cellLogic.Init(i, null, inventoryGridController, cellImage);
            barCells.Add(cellLogic);
        }
    }

    void RefreshFromInventory()
    {
        if (inventoryGridController == null) return;

        int totalInventorySlots = inventoryGridController.TotalSlots;
        int inventoryColumns = inventoryGridController.inventoryColumns;

        if (inventoryColumns <= 0)
        {
            Debug.LogError("[InventoryBarController] Invalid inventory columns count from InventoryGridController!");
            totalRows = 1;
            currentRow = 0;
            return;
        }

        totalRows = Mathf.Max(1, Mathf.CeilToInt((float)totalInventorySlots / inventoryColumns));
        currentRow = Mathf.Clamp(currentRow, 0, totalRows - 1);

        if (inventoryGridController != null)
        {
            for (int i = 0; i < inventoryGridController.TotalSlots; i++)
            {
                var cell = inventoryGridController.GetInventoryCellAtIndex(i);
                if (cell != null && cell.HasNode())
                {
                    var nodeData = cell.GetNodeData();
                    if (nodeData != null)
                    {
                        nodeData.CleanForSerialization(0, "InvBarRefresh");
                    }
                }
            }
        }
    }

    void UpdateBarDisplay()
    {
        if (inventoryGridController == null || barCells == null) return;

        foreach (var cell in barCells)
        {
            cell.RemoveNode();
        }

        int mainInventoryColumns = inventoryGridController.inventoryColumns;
         if (mainInventoryColumns <= 0) {
             Debug.LogError("[InventoryBarController] UpdateBarDisplay: InventoryGridController has 0 columns. Cannot display items.");
             return;
        }

        int startIndexInMainInventory = currentRow * mainInventoryColumns;
        int maxInventoryIndex = inventoryGridController.TotalSlots - 1;

        for (int i = 0; i < slotsPerRow; i++)
        {
            if (i >= mainInventoryColumns) break;
            int inventoryIndexToDisplay = startIndexInMainInventory + i;
            if (inventoryIndexToDisplay > maxInventoryIndex) continue;

            if (i < barCells.Count)
            {
                NodeCell inventoryCell = inventoryGridController.GetInventoryCellAtIndex(inventoryIndexToDisplay);
                if (inventoryCell != null && inventoryCell.HasNode())
                {
                    CopyInventoryItemToBarCell(inventoryCell, barCells[i]);
                }
            }
        }

        if (upArrowButton != null) upArrowButton.interactable = currentRow > 0;
        if (downArrowButton != null) downArrowButton.interactable = currentRow < totalRows - 1;
    }


    void CopyInventoryItemToBarCell(NodeCell inventoryCell, NodeCell barCell)
    {
        NodeData nodeData = inventoryCell.GetNodeData();
        NodeView nodeView = inventoryCell.GetNodeView();
        ToolDefinition toolDef = inventoryCell.GetToolDefinition();

        if (toolDef != null)
        {
            CreateToolCopyInBarCell(toolDef, nodeData, barCell);
        }
        else if (nodeData != null && nodeView != null)
        {
            NodeDefinition nodeDef = nodeView.GetNodeDefinition();
            if (nodeDef != null)
            {
                CreateNodeCopyInBarCell(nodeDef, nodeData, barCell);
            }
        }
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

    void UpdateSelection()
    {
        SelectedItem = null; // Reset

        if (selectedSlot == -1) // Explicitly deselected
        {
            // SelectedItem remains null
        }
        else if (selectedSlot >= 0 && selectedSlot < barCells.Count && inventoryGridController != null)
        {
            // This part tries to find the actual item from the main inventory
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

        // Update highlight based on SelectedItem and selectedSlot
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

        // Update tooltip
        if (gameObject.activeInHierarchy && SelectedItem != null && SelectedItem.IsValid() && UniversalTooltipManager.Instance != null && selectedSlot >= 0 && selectedSlot < barCells.Count)
        {
            UniversalTooltipManager.Instance.ShowInventoryItemTooltip(
                SelectedItem,
                barCells[selectedSlot].transform
            );
        }
        else if (UniversalTooltipManager.Instance != null)
        {
            UniversalTooltipManager.Instance.HideTooltip();
        }

        OnSelectionChanged?.Invoke(SelectedItem);
    }

    void OnDestroy()
    {
        if (upArrowButton != null) upArrowButton.onClick.RemoveAllListeners();
        if (downArrowButton != null) downArrowButton.onClick.RemoveAllListeners();
    }
}