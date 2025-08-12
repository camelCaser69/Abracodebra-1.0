using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
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
    [SerializeField] private GameObject inventoryItemViewPrefab;

    private List<GameObject> barSlots = new List<GameObject>();
    private int selectedSlot = 0;

    public InventoryBarItem SelectedItem { get; private set; }
    public event System.Action<InventoryBarItem> OnSelectionChanged;

    public class InventoryBarItemComponent : MonoBehaviour { public InventoryBarItem item; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        selectedSlot = 0;
    }

    void Start()
    {
        if (inventoryGridController != null) inventoryGridController.OnInventoryChanged += HandleInventoryChanged;
        else Debug.LogError($"[{nameof(InventoryBarController)}] InventoryGridController not assigned!", this);

        if (inventoryItemViewPrefab == null) Debug.LogError($"[{nameof(InventoryBarController)}] InventoryItemViewPrefab not assigned!", this);

        SetupBarCells();
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (inventoryGridController != null) inventoryGridController.OnInventoryChanged -= HandleInventoryChanged;
    }

    private void UpdateBarDisplay()
    {
        foreach (var slot in barSlots)
        {
            foreach (Transform child in slot.transform) Destroy(child.gameObject);
        }

        if (inventoryGridController == null || inventoryItemViewPrefab == null) return;
        var allItems = inventoryGridController.GetAllItems();
        int itemsToDisplay = Mathf.Min(allItems.Count, slotsPerRow);

        for (int i = 0; i < itemsToDisplay; i++)
        {
            if (i >= barSlots.Count) break;
            var item = allItems[i];
            if (item == null || !item.IsValid()) continue;

            GameObject itemViewGO = Instantiate(inventoryItemViewPrefab, barSlots[i].transform);
            var itemView = itemViewGO.GetComponentInChildren<ItemView>();
            if (itemView == null)
            {
                Debug.LogError($"The assigned InventoryItemViewPrefab is missing the ItemView component on itself or its children!", inventoryItemViewPrefab);
                Destroy(itemViewGO);
                continue;
            }
            itemViewGO.SetActive(true);

            switch(item.Type)
            {
                case InventoryBarItem.ItemType.Gene: itemView.InitializeAsGene(item.GeneInstance); break;
                case InventoryBarItem.ItemType.Seed: itemView.InitializeAsSeed(item.SeedTemplate); break;
                case InventoryBarItem.ItemType.Tool: itemView.InitializeAsTool(item.ToolDefinition); break;
            }
            var barItemComponent = itemViewGO.AddComponent<InventoryBarItemComponent>();
            barItemComponent.item = item;
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
        for (int i = 0; i < slotsPerRow; i++) 
        { 
            GameObject cellGO = new GameObject($"BarCell_{i}", typeof(RectTransform)); 
            cellGO.transform.SetParent(cellContainer, false); 
            barSlots.Add(cellGO); 
        } 
    }
    
    private void HandleNumberKeyInput() 
    { 
        for (int i = 1; i <= 9; i++) { if (Input.GetKeyDown(KeyCode.Alpha0 + i)) { SelectSlot(i - 1); return; } } 
        if (Input.GetKeyDown(KeyCode.Alpha0)) { SelectSlot(9); } 
    }
    
    private void SelectSlot(int slotIndex) 
    { 
        if (slotIndex < 0 || slotIndex >= slotsPerRow) return; 
        SelectedItem = null; 
        selectedSlot = slotIndex; 
        if (slotIndex < barSlots.Count) 
        { 
            var slot = barSlots[slotIndex]; 
            var itemComponent = slot.GetComponentInChildren<InventoryBarItemComponent>(); 
            if (itemComponent != null) { SelectedItem = itemComponent.item; } 
        } 
        UpdateSelection(); 
    }
    
    private void UpdateSelection() 
    { 
        if (selectionHighlight != null) 
        { 
            bool itemIsValid = SelectedItem != null && SelectedItem.IsValid(); 
            selectionHighlight.SetActive(itemIsValid); 
            if (itemIsValid && selectedSlot < barSlots.Count) 
            {
                selectionHighlight.transform.SetParent(barSlots[selectedSlot].transform, false);
                selectionHighlight.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            } 
        } 
        OnSelectionChanged?.Invoke(SelectedItem); 
    }

    public void SelectSlotByIndex(int slotIndex)
    {
        int targetSlot = Mathf.Clamp(slotIndex, 0, slotsPerRow - 1);
        SelectSlot(targetSlot);
    }
}