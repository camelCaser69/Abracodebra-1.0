// Reworked File: Assets/Scripts/PlantSystem/UI/InventoryBarController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
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

    private void UpdateBarDisplay()
    {
        // This logic needs to be adapted based on what the inventory holds.
        // For now, let's assume it holds SeedTemplates for planting.
        // A more complex system would handle tools and genes.
        
        // Clear old items
        foreach(var slot in barSlots)
        {
            foreach(Transform child in slot.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // TODO: This part requires a list of items the player can actually use from the bar,
        // which would come from a reworked InventoryGridController.
        // For now, this will remain empty until the inventory logic is fully fleshed out.
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

    private void SelectSlot(int slotIndex)
    {
        if (this.selectedSlot == slotIndex && this.SelectedItem != null)
        {
            this.selectedSlot = -1; // Deselect
        }
        else
        {
            this.selectedSlot = Mathf.Clamp(slotIndex, 0, slotsPerRow - 1);
        }
        UpdateSelection();
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