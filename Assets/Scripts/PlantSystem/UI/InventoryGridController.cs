// Reworked File: Assets/Scripts/PlantSystem/UI/InventoryGridController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Abracodabra.Genes;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Templates;
using Abracodabra.UI.Genes;

public class InventoryGridController : MonoBehaviour
{
    public static InventoryGridController Instance { get; private set; }

    [Header("Grid Configuration")]
    [SerializeField][Min(1)] private int inventoryRows = 2;
    [SerializeField][Min(1)] private int inventoryColumns = 8;
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] private float cellMargin = 10f;

    [Header("Prefabs & References")]
    [SerializeField] private GameObject geneSlotPrefab; // IMPORTANT: Must have GeneSlotUI component
    [SerializeField] private Transform cellContainer;
    [SerializeField] private GeneLibrary geneLibrary; // To populate initial genes

    private List<GeneSlotUI> inventorySlots = new List<GeneSlotUI>();

    public event System.Action OnInventoryChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (cellContainer == null) Debug.LogError("InventoryGridController: Cell Container not assigned!", this);
        if (geneSlotPrefab == null) Debug.LogError("InventoryGridController: Gene Slot Prefab not assigned!", this);
        if (geneLibrary == null) geneLibrary = GeneLibrary.Instance;

        CreateInventoryCells();
        PopulateInitialInventory();
    }

    private void CreateInventoryCells()
    {
        foreach (Transform child in cellContainer) Destroy(child.gameObject);
        inventorySlots.Clear();

        GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            Debug.LogError("InventoryGridController: Cell Container MUST have a GridLayoutGroup component.", this);
            return;
        }

        gridLayout.cellSize = cellSize;
        gridLayout.spacing = new Vector2(cellMargin, cellMargin);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = inventoryColumns;

        int totalCells = inventoryRows * inventoryColumns;
        for (int i = 0; i < totalCells; i++)
        {
            GameObject cellGO = Instantiate(geneSlotPrefab, cellContainer);
            GeneSlotUI slotUI = cellGO.GetComponent<GeneSlotUI>();
            if (slotUI != null)
            {
                slotUI.slotIndex = i;
                inventorySlots.Add(slotUI);
            }
            else
            {
                Debug.LogError($"The provided Gene Slot Prefab for the inventory is missing the 'GeneSlotUI' component!", geneSlotPrefab);
            }
        }
    }

    private void PopulateInitialInventory()
    {
        if (geneLibrary == null) return;

        // Add starter genes to inventory
        foreach (var gene in geneLibrary.starterGenes)
        {
            if (gene != null)
            {
                AddGeneToInventory(gene);
            }
        }
    }

    public bool AddGeneToInventory(GeneBase gene)
    {
        if (gene == null) return false;

        GeneSlotUI emptySlot = inventorySlots.FirstOrDefault(slot => slot.GetGeneInstance() == null);
        if (emptySlot == null)
        {
            Debug.LogWarning("Inventory is full! Cannot add new gene.");
            return false;
        }

        var runtimeInstance = new RuntimeGeneInstance(gene);
        emptySlot.SetGeneInstance(runtimeInstance);
        
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void RemoveGeneFromInventory(RuntimeGeneInstance instance)
    {
        if (instance == null) return;
        
        GeneSlotUI slot = inventorySlots.FirstOrDefault(s => s.GetGeneInstance() == instance);
        if (slot != null)
        {
            slot.ClearSlot();
            OnInventoryChanged?.Invoke();
        }
    }

    public List<RuntimeGeneInstance> GetAllGenes()
    {
        return inventorySlots
            .Where(s => s.GetGeneInstance() != null)
            .Select(s => s.GetGeneInstance())
            .ToList();
    }
}