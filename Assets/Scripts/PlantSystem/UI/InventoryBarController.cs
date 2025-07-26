// Assets/Scripts/PlantSystem/UI/InventoryBarController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryBarController : MonoBehaviour
{
    public static InventoryBarController Instance { get; set; }

    [Header("Layout")]
    [SerializeField] int slotsPerRow = 10;
    [SerializeField] Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] float cellMargin = 10f;

    [Header("References")]
    [SerializeField] Transform cellContainer;
    [SerializeField] Button upArrowButton;
    [SerializeField] Button downArrowButton;
    [SerializeField] GameObject selectionHighlight;
    [SerializeField] Sprite emptyCellSprite;
    [SerializeField] Color emptyCellColor = Color.white;
    [SerializeField] InventoryGridController inventoryGridController;

    private readonly List<NodeCell> barCells = new List<NodeCell>();
    private int currentRow = 0;
    private int selectedSlot = 0;
    private int totalRows = 0;

    public InventoryBarItem SelectedItem { get; set; }
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

        // <<< NEW >>> Subscribe to the inventory change event
        if (inventoryGridController != null)
        {
            inventoryGridController.OnInventoryChanged += HandleInventoryChanged;
        }

        SetupBarCells();
        gameObject.SetActive(false); // Start hidden
    }

    void OnDestroy()
    {
        if (upArrowButton != null) upArrowButton.onClick.RemoveAllListeners();
        if (downArrowButton != null) downArrowButton.onClick.RemoveAllListeners();

        // <<< NEW >>> Unsubscribe to prevent memory leaks
        if (inventoryGridController != null)
        {
            inventoryGridController.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    // <<< NEW >>> Event handler to refresh the bar when inventory changes
    private void HandleInventoryChanged()
    {
        // Only refresh if the bar is currently active to avoid unnecessary work
        if (gameObject.activeInHierarchy)
        {
            RefreshFromInventory();
            UpdateBarDisplay();
            UpdateSelection(); // Also update selection in case the selected item moved or was removed
        }
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

    void SetupBarCells()
    {
        foreach (Transform child in cellContainer) Destroy(child.gameObject);
        barCells.Clear();

        GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
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

            if (inventoryGridController != null)
            {
                cellImage.color = inventoryGridController.EmptyCellColor;
            }
            else
            {
                cellImage.color = emptyCellColor;
            }

            NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
            cellLogic.Init(i, inventoryGridController, cellImage);
            barCells.Add(cellLogic);
        }
    }

    void RefreshFromInventory()
    {
        if (inventoryGridController == null) return;
        totalRows = Mathf.Max(1, Mathf.CeilToInt((float)inventoryGridController.TotalSlots / inventoryGridController.inventoryColumns));
        currentRow = Mathf.Clamp(currentRow, 0, totalRows - 1);
    }

    void UpdateBarDisplay()
    {
        if (inventoryGridController == null || barCells == null) return;

        foreach (var cell in barCells)
        {
            cell.RemoveNode(); // This will destroy any existing ItemView/DisplayObject

            Image cellImage = cell.GetComponent<Image>();
            if (cellImage != null && inventoryGridController != null)
            {
                cellImage.color = inventoryGridController.EmptyCellColor; // Reset to empty color
            }
        }

        int startIndexInMainInventory = currentRow * inventoryGridController.inventoryColumns;

        for (int i = 0; i < slotsPerRow; i++)
        {
            if (i >= inventoryGridController.inventoryColumns) break; // Don't try to access beyond inventory width
            int inventoryIndexToDisplay = startIndexInMainInventory + i;
            if (inventoryIndexToDisplay >= inventoryGridController.TotalSlots) continue;

            NodeCell inventoryCell = inventoryGridController.GetInventoryCellAtIndex(inventoryIndexToDisplay);
            if (inventoryCell != null && inventoryCell.HasItem())
            {
                CopyInventoryItemToBarCell(inventoryCell, barCells[i]);
            }
        }

        if (upArrowButton != null) upArrowButton.interactable = currentRow > 0;
        if (downArrowButton != null) downArrowButton.interactable = currentRow < totalRows - 1;
    }

    void CopyInventoryItemToBarCell(NodeCell inventoryCell, NodeCell barCell)
    {
        NodeData nodeData = inventoryCell.GetNodeData();
        ToolDefinition toolDef = inventoryCell.GetToolDefinition();
        NodeDefinition nodeDef = inventoryCell.GetNodeDefinition();

        if (InventoryGridController.Instance.InventoryItemPrefab == null)
        {
            Debug.LogError("[InventoryBarController] InventoryItemPrefab is not set on InventoryGridController!");
            return;
        }

        GameObject itemViewGO = Instantiate(InventoryGridController.Instance.InventoryItemPrefab, barCell.transform);
        ItemView itemView = itemViewGO.GetComponent<ItemView>();

        if (itemView == null)
        {
            Debug.LogError("[InventoryBarController] The InventoryItemPrefab is missing the ItemView component!", itemViewGO);
            Destroy(itemViewGO);
            return;
        }

        if (toolDef != null)
        {
            itemView.Initialize(nodeData, toolDef);
        }
        else if (nodeDef != null)
        {
            itemView.Initialize(nodeData, nodeDef, null); // Sequence controller is null for inventory items
        }

        NodeDraggable draggable = itemView.GetComponent<NodeDraggable>();
        if (draggable != null)
        {
            draggable.enabled = false;
        }

        barCell.AssignItemView(itemView, nodeData, toolDef);
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
        if (this.selectedSlot == slotIndexInBar && this.SelectedItem != null)
        {
            this.selectedSlot = -1; // Mark as deselected
        }
        else
        {
            this.selectedSlot = Mathf.Clamp(slotIndexInBar, 0, slotsPerRow - 1);
        }
        UpdateSelection();
    }

    void UpdateSelection()
    {
        SelectedItem = null; // Reset

        if (selectedSlot >= 0 && selectedSlot < barCells.Count && inventoryGridController != null)
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

        OnSelectionChanged?.Invoke(SelectedItem);
    }
}