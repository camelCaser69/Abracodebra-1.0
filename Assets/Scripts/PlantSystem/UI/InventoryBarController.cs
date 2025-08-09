// Reworked File: Assets/Scripts/PlantSystem/UI/InventoryBarController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Templates;
using Abracodabra.UI.Genes; // For ItemView

public class InventoryBarController : MonoBehaviour
{
    public static InventoryBarController Instance { get; private set; }

    [SerializeField] private int slotsPerRow = 10;
    [SerializeField] private InventoryGridController inventoryGridController;
    [SerializeField] private Transform cellContainer;
    [SerializeField] private GameObject selectionHighlight;
    [SerializeField] private GameObject inventoryItemViewPrefab; // Prefab with just ItemView and visuals

    private List<GameObject> barSlots = new List<GameObject>();
    private int selectedSlot = 0;

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
        if (inventoryGridController != null)
        {
            inventoryGridController.OnInventoryChanged += HandleInventoryChanged;
        }
        SetupBarCells();
        gameObject.SetActive(false); // Start hidden
    }
    
    void OnDestroy()
    {
        if (inventoryGridController != null)
        {
            inventoryGridController.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged()
    {
        if (gameObject.activeInHierarchy)
        {
            UpdateBarDisplay();
            UpdateSelection();
        }
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        HandleNumberKeyInput();
    }

    public void ShowBar()
    {
        RefreshBar();
        gameObject.SetActive(true);
    }

    public void HideBar()
    {
        gameObject.SetActive(false);
        if (selectionHighlight != null) selectionHighlight.SetActive(false);
        UniversalTooltipManager.Instance?.HideTooltip();
    }

    private void RefreshBar()
    {
        if (selectedSlot < 0 && slotsPerRow > 0) selectedSlot = 0;
        UpdateBarDisplay();
        UpdateSelection();
    }
    
    private void SetupBarCells()
    {
        foreach (Transform child in cellContainer) Destroy(child.gameObject);
        barSlots.Clear();

        GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        gridLayout.constraintCount = slotsPerRow;

        for (int i = 0; i < slotsPerRow; i++)
        {
            GameObject cellGO = new GameObject($"BarCell_{i}", typeof(RectTransform), typeof(Image));
            cellGO.transform.SetParent(cellContainer, false);
            barSlots.Add(cellGO);
        }
    }

    void UpdateBarDisplay()
    {
        // Clear existing items in bar slots
        foreach (var slot in barSlots)
        {
            foreach (Transform child in slot.transform)
            {
                Destroy(child.gameObject);
            }
        }

        if (inventoryGridController == null) return;

        // Get items from inventory grid (first row)
        var allGenes = inventoryGridController.GetAllGenes();
    
        for (int i = 0; i < Mathf.Min(allGenes.Count, slotsPerRow); i++)
        {
            if (i >= barSlots.Count) break;
        
            var gene = allGenes[i];
            if (gene == null) continue;
        
            // Create visual representation in bar slot
            GameObject itemVisual = new GameObject("ItemVisual");
            itemVisual.transform.SetParent(barSlots[i].transform, false);
        
            var itemImage = itemVisual.AddComponent<Image>();
            var geneBase = gene.GetGene();
            if (geneBase != null && geneBase.icon != null)
            {
                itemImage.sprite = geneBase.icon;
                itemImage.color = geneBase.geneColor;
            }
        
            // Store reference for selection
            var barItem = itemVisual.AddComponent<InventoryBarItemComponent>();
            barItem.runtimeInstance = gene;
            barItem.slotIndex = i;
        }
    }
    
    private void HandleNumberKeyInput()
    {
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                SelectSlot(i - 1);
                return;
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SelectSlot(9);
        }
    }

    void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotsPerRow) return;
    
        // Find item in slot
        if (slotIndex < barSlots.Count)
        {
            var slot = barSlots[slotIndex];
            var itemComponent = slot.GetComponentInChildren<InventoryBarItemComponent>();
        
            if (itemComponent != null && itemComponent.runtimeInstance != null)
            {
                // Create InventoryBarItem for selection
                SelectedItem = InventoryBarItem.FromGene(itemComponent.runtimeInstance, slot);
                selectedSlot = slotIndex;
            }
            else
            {
                SelectedItem = null;
                selectedSlot = -1;
            }
        }
    
        UpdateSelection();
    }
    
    public class InventoryBarItemComponent : MonoBehaviour
    {
        public RuntimeGeneInstance runtimeInstance;
        public int slotIndex;
    }

    private void UpdateSelection()
    {
        // This will be properly implemented once the inventory holds usable items like SeedTemplates or Tools.
        SelectedItem = null; // Placeholder
        
        if (selectionHighlight != null)
        {
            // Placeholder logic
            selectionHighlight.SetActive(SelectedItem != null);
            if (SelectedItem != null)
            {
                selectionHighlight.transform.SetParent(barSlots[selectedSlot].transform, false);
            }
        }
        
        OnSelectionChanged?.Invoke(SelectedItem);
    }
}