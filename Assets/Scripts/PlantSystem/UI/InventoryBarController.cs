// Reworked File: Assets/Scripts/PlantSystem/UI/InventoryBarController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Templates;
using Abracodabra.UI.Genes;

public class InventoryBarController : MonoBehaviour
{
    public static InventoryBarController Instance { get; private set; }

    [SerializeField] private int slotsPerRow = 10;
    [SerializeField] private InventoryGridController inventoryGridController;
    [SerializeField] private Transform cellContainer;
    [SerializeField] private GameObject selectionHighlight;
    [SerializeField] private GameObject inventoryItemViewPrefab; // This prefab must have an ItemView component

    private List<GameObject> barSlots = new List<GameObject>();
    private int selectedSlot = 0;

    public InventoryBarItem SelectedItem { get; private set; }
    public event System.Action<InventoryBarItem> OnSelectionChanged;

    // This component will be added to the item visual to hold its data
    public class InventoryBarItemComponent : MonoBehaviour
    {
        public InventoryBarItem item;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        selectedSlot = 0;
    }

    private void Start()
    {
        if (inventoryGridController != null)
        {
            inventoryGridController.OnInventoryChanged += HandleInventoryChanged;
        }
        else
        {
            Debug.LogError($"[{nameof(InventoryBarController)}] InventoryGridController not assigned!", this);
        }
        
        if (inventoryItemViewPrefab == null)
        {
            Debug.LogError($"[{nameof(InventoryBarController)}] InventoryItemViewPrefab not assigned! Bar cannot display items.", this);
        }
        
        SetupBarCells();
        gameObject.SetActive(false); // Start hidden
    }

    private void OnDestroy()
    {
        if (inventoryGridController != null)
        {
            inventoryGridController.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    // In file: Assets/Scripts/PlantSystem/UI/InventoryBarController.cs

    private void UpdateBarDisplay()
    {
        // Clear all old item visuals from the slots
        foreach (var slot in barSlots)
        {
            foreach (Transform child in slot.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // Ensure we have the necessary references to proceed
        if (inventoryGridController == null || inventoryItemViewPrefab == null) return;

        var allItems = inventoryGridController.GetAllItems();
        int itemsToDisplay = Mathf.Min(allItems.Count, slotsPerRow);

        for (int i = 0; i < itemsToDisplay; i++)
        {
            if (i >= barSlots.Count) break;

            var item = allItems[i];
            if (item == null || !item.IsValid()) continue;

            // 1. Instantiate the visual prefab and parent it to the correct bar slot
            GameObject itemViewGO = Instantiate(inventoryItemViewPrefab, barSlots[i].transform);
            
            // 2. Get the ItemView component from the newly created object
            var itemView = itemViewGO.GetComponent<ItemView>();
            if (itemView == null)
            {
                Debug.LogError($"The assigned InventoryItemViewPrefab is missing the ItemView component!", inventoryItemViewPrefab);
                Destroy(itemViewGO);
                continue;
            }

            // 3. Initialize the ItemView with the correct data based on the item's type
            switch(item.Type)
            {
                case InventoryBarItem.ItemType.Gene:
                    itemView.InitializeAsGene(item.GeneInstance);
                    break;
                case InventoryBarItem.ItemType.Seed:
                    itemView.InitializeAsSeed(item.SeedTemplate);
                    break;
                case InventoryBarItem.ItemType.Tool:
                    itemView.InitializeAsTool(item.ToolDefinition);
                    break;
            }
            
            // 4. Attach our data-holding component so we can retrieve the item later on click
            var barItemComponent = itemViewGO.AddComponent<InventoryBarItemComponent>();
            barItemComponent.item = item;
        }
    }

    // ... (The rest of the methods are unchanged from the previous response and are correct)
    // HandleInventoryChanged, Update, ShowBar, HideBar, RefreshBar, SetupBarCells, HandleNumberKeyInput, SelectSlot, UpdateSelection
    private void HandleInventoryChanged() { if (gameObject.activeInHierarchy) { UpdateBarDisplay(); UpdateSelection(); } }
    private void Update() { if (!gameObject.activeInHierarchy) return; HandleNumberKeyInput(); }
    public void ShowBar() { RefreshBar(); gameObject.SetActive(true); }
    public void HideBar() { gameObject.SetActive(false); if (selectionHighlight != null) selectionHighlight.SetActive(false); UniversalTooltipManager.Instance?.HideTooltip(); }
    private void RefreshBar() { if (selectedSlot < 0 && slotsPerRow > 0) selectedSlot = 0; UpdateBarDisplay(); UpdateSelection(); }
    private void SetupBarCells() { foreach (Transform child in cellContainer) Destroy(child.gameObject); barSlots.Clear(); GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>(); gridLayout.constraintCount = slotsPerRow; for (int i = 0; i < slotsPerRow; i++) { GameObject cellGO = new GameObject($"BarCell_{i}", typeof(RectTransform)); cellGO.transform.SetParent(cellContainer, false); barSlots.Add(cellGO); } }
    private void HandleNumberKeyInput() { for (int i = 1; i <= 9; i++) { if (Input.GetKeyDown(KeyCode.Alpha0 + i)) { SelectSlot(i - 1); return; } } if (Input.GetKeyDown(KeyCode.Alpha0)) { SelectSlot(9); } }
    private void SelectSlot(int slotIndex) { if (slotIndex < 0 || slotIndex >= slotsPerRow) return; SelectedItem = null; selectedSlot = slotIndex; if (slotIndex < barSlots.Count) { var slot = barSlots[slotIndex]; var itemComponent = slot.GetComponentInChildren<InventoryBarItemComponent>(); if (itemComponent != null) { SelectedItem = itemComponent.item; } } UpdateSelection(); }
    private void UpdateSelection() { if (selectionHighlight != null) { bool itemIsValid = SelectedItem != null && SelectedItem.IsValid(); selectionHighlight.SetActive(itemIsValid); if (itemIsValid && selectedSlot < barSlots.Count) { selectionHighlight.transform.SetParent(barSlots[selectedSlot].transform, false); selectionHighlight.GetComponent<RectTransform>().anchoredPosition = Vector2.zero; } } OnSelectionChanged?.Invoke(SelectedItem); }
}