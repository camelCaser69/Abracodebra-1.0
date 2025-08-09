// Reworked File: Assets/Scripts/PlantSystem/UI/InventoryGridController.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Abracodabra.Genes; // <<< THIS WAS THE MISSING LINE
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Templates;
using Abracodabra.UI.Genes;

public class InventoryGridController : MonoBehaviour
{
    public static InventoryGridController Instance { get; private set; }

    [SerializeField][Min(1)] private int inventoryRows = 2;
    [SerializeField][Min(1)] private int inventoryColumns = 8;
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] private float cellMargin = 10f;

    [SerializeField] private GameObject itemSlotPrefab; // IMPORTANT: Must have GeneSlotUI component
    [SerializeField] private Transform cellContainer;
    [SerializeField] private GeneLibrary geneLibrary;
    [SerializeField] private ToolSwitcher toolSwitcher;

    private List<GeneSlotUI> inventorySlots = new List<GeneSlotUI>();

    public event System.Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (cellContainer == null) Debug.LogError("InventoryGridController: Cell Container not assigned!", this);
        if (itemSlotPrefab == null) Debug.LogError("InventoryGridController: Item Slot Prefab not assigned!", this);
        if (geneLibrary == null) geneLibrary = GeneLibrary.Instance;
        if (toolSwitcher == null) toolSwitcher = ToolSwitcher.Instance;

        CreateInventoryCells();
        PopulateInitialInventory();
    }

    private void CreateInventoryCells()
{
    if (itemSlotPrefab == null || cellContainer == null)
    {
        return;
    }

    // Clear any existing slots just in case
    foreach (Transform child in cellContainer)
    {
        Destroy(child.gameObject);
    }
    inventorySlots.Clear();

    // Set up GridLayoutGroup
    var gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
    if (gridLayout == null)
    {
        gridLayout = cellContainer.gameObject.AddComponent<GridLayoutGroup>();
    }
    gridLayout.cellSize = cellSize;
    gridLayout.spacing = new Vector2(cellMargin, cellMargin);
    gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
    gridLayout.constraintCount = inventoryColumns;

    // Create slots
    for (int i = 0; i < inventoryRows * inventoryColumns; i++)
    {
        GameObject slotObj = Instantiate(itemSlotPrefab, cellContainer);
        slotObj.name = $"ItemSlot_{i}";
        GeneSlotUI slot = slotObj.GetComponent<GeneSlotUI>();
        if (slot != null)
        {
            slot.slotIndex = i;
            inventorySlots.Add(slot);
        }
        else
        {
            Debug.LogError($"[InventoryGridController] The 'itemSlotPrefab' is missing the required GeneSlotUI component!", itemSlotPrefab);
            Destroy(slotObj); // Clean up the bad instance
        }
    }
}

    private void PopulateInitialInventory()
    {
        if (geneLibrary != null)
        {
            foreach (var gene in geneLibrary.starterGenes)
            {
                if (gene != null)
                {
                    AddItemToInventory(InventoryBarItem.FromGene(new RuntimeGeneInstance(gene)));
                }
            }
        }

        // Now also add tools
        if (toolSwitcher != null && toolSwitcher.toolDefinitions != null)
        {
            foreach (var toolDef in toolSwitcher.toolDefinitions)
            {
                if (toolDef != null && toolDef.autoAddToInventory)
                {
                     AddItemToInventory(InventoryBarItem.FromTool(toolDef));
                }
            }
        }
    }
    
    public bool AddItemToInventory(InventoryBarItem item)
    {
        if (item == null || !item.IsValid()) return false;

        GeneSlotUI emptySlot = inventorySlots.FirstOrDefault(slot => slot.CurrentItem == null);
        if (emptySlot == null)
        {
            Debug.LogWarning("Inventory is full! Cannot add new item.");
            return false;
        }

        emptySlot.SetItem(item);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void RemoveItemFromInventory(InventoryBarItem item)
    {
        if (item == null) return;

        GeneSlotUI slot = inventorySlots.FirstOrDefault(s => s.CurrentItem == item);
        if (slot != null)
        {
            slot.ClearSlot();
            OnInventoryChanged?.Invoke();
        }
    }

    public List<InventoryBarItem> GetAllItems()
    {
        return inventorySlots
            .Where(s => s.CurrentItem != null && s.CurrentItem.IsValid())
            .Select(s => s.CurrentItem)
            .ToList();
    }
}