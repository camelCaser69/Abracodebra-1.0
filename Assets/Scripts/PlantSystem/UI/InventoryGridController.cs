using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Templates;
using Abracodabra.UI.Genes;
using UnityEngine.UI;

public class InventoryGridController : MonoBehaviour
{
    public static InventoryGridController Instance { get; private set; }

    [Header("Grid Layout")]
    [SerializeField][Min(1)] private int inventoryRows = 4;
    [SerializeField][Min(1)] private int inventoryColumns = 4;
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] private float cellMargin = 10f;

    [Header("Component References")]
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private Transform cellContainer;
    [SerializeField] private StartingInventory startingInventory;

    private List<GeneSlotUI> inventorySlots = new List<GeneSlotUI>();

    public event System.Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (cellContainer == null) Debug.LogError("InventoryGridController: Cell Container not assigned!", this);
        if (itemSlotPrefab == null) Debug.LogError("InventoryGridController: Item Slot Prefab not assigned!", this);

        if (startingInventory == null)
        {
            Debug.LogError("InventoryGridController: Starting Inventory asset is not assigned! The player will have no items.", this);
        }

        CreateInventoryCells();
        PopulateInitialInventory();
    }

    private void CreateInventoryCells()
    {
        if (itemSlotPrefab == null || cellContainer == null) return;

        foreach (Transform child in cellContainer)
        {
            Destroy(child.gameObject);
        }
        inventorySlots.Clear();

        var gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = cellContainer.gameObject.AddComponent<GridLayoutGroup>();
        }
        gridLayout.cellSize = cellSize;
        gridLayout.spacing = new Vector2(cellMargin, cellMargin);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = inventoryColumns;

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
                Destroy(slotObj);
            }
        }
    }

    private void PopulateInitialInventory()
    {
        if (startingInventory == null) return;

        foreach (var tool in startingInventory.startingTools)
        {
            if (tool != null) AddItemToInventory(InventoryBarItem.FromTool(tool));
        }

        foreach (var seed in startingInventory.startingSeeds)
        {
            if (seed != null) AddItemToInventory(InventoryBarItem.FromSeed(seed));
        }

        foreach (var gene in startingInventory.startingGenes)
        {
            if (gene != null) AddItemToInventory(InventoryBarItem.FromGene(new RuntimeGeneInstance(gene)));
        }
    }

    public bool AddItemToInventory(InventoryBarItem item)
    {
        if (item == null || !item.IsValid()) return false;

        GeneSlotUI emptySlot = inventorySlots.FirstOrDefault(slot => slot.CurrentItem == null);
        if (emptySlot == null)
        {
            Debug.LogWarning($"Inventory is full! Cannot add item: {item.GetDisplayName()}", this);
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

    /// <summary>
    /// Gets the items from the first N slots of the inventory, including nulls for empty slots.
    /// </summary>
    /// <param name="count">The number of slots to get.</param>
    /// <returns>A list of InventoryBarItems, which may contain null entries.</returns>
    public List<InventoryBarItem> GetFirstNItems(int count)
    {
        var items = new List<InventoryBarItem>();
        for (int i = 0; i < count; i++)
        {
            if (i < inventorySlots.Count)
            {
                items.Add(inventorySlots[i].CurrentItem);
            }
            else
            {
                // If the inventory grid itself is smaller than the bar, add nulls
                items.Add(null);
            }
        }
        return items;
    }
}