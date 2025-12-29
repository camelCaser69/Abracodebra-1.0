using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using WegoSystem;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Core;
using Abracodabra.UI.Toolkit;

/// <summary>
/// Main UI Manager - Coordinates all UI controllers and manages shared state
/// </summary>
public class GameUIManager : MonoBehaviour
{
    [Header("UI Templates")]
    [SerializeField] private VisualTreeAsset inventorySlotTemplate;
    [SerializeField] private VisualTreeAsset geneSlotTemplate;

    [Header("Starting Items")]
    [SerializeField] private StartingInventory startingInventory;

    [Header("Inventory Configuration")]
    [Tooltip("Number of rows in the inventory grid")]
    [SerializeField] private int inventoryRows = 4;
    
    [Tooltip("Number of columns in the inventory grid")]
    [SerializeField] private int inventoryColumns = 6;

    // Shared inventory data
    private List<UIInventoryItem> playerInventory = new List<UIInventoryItem>();

    // UI Controllers
    private UIInventoryGridController inventoryController;
    private UIDragDropController dragDropController;
    private UISeedEditorController seedEditorController;
    private UISpecSheetController specSheetController;
    private UIHotbarController hotbarController;

    // UI Element References
    private VisualElement rootElement;
    private VisualElement planningPanel, hudPanel;

    // FIX #2: Calculate total inventory size from rows * columns
    private int TotalInventorySlots => inventoryRows * inventoryColumns;

    void Start()
    {
        if (inventorySlotTemplate == null || geneSlotTemplate == null)
        {
            Debug.LogError("CRITICAL: UI Template assets are not assigned in the GameUIManager Inspector! Please drag your 'InventorySlot.uxml' and 'GeneSlot.uxml' files into the corresponding fields on the '_UIDocument_GameUI' GameObject.", this);
            this.enabled = false;
            return;
        }

        rootElement = GetComponent<UIDocument>().rootVisualElement;

        // Query UI panels
        planningPanel = rootElement.Q<VisualElement>("PlanningPanel");
        hudPanel = rootElement.Q<VisualElement>("HUDPanel");

        // Initialize inventory data
        SetupPlayerInventory();

        // Initialize all controllers
        InitializeControllers();

        // Subscribe to controller events
        SubscribeToEvents();

        // Initial setup
        inventoryController.PopulateGrid();
        hotbarController.SetupHotbar(playerInventory.Take(8).ToList());
        seedEditorController.Clear();
        specSheetController.Clear();

        // Handle run state
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnRunStateChanged += HandleRunStateChanged;
            HandleRunStateChanged(RunManager.Instance.CurrentState);
        }
        else
        {
            ShowPlanningUI();
        }

        // Select first hotbar slot after a frame
        rootElement.schedule.Execute(() => hotbarController.SelectSlot(0)).StartingIn(10);
    }

    void Update()
    {
        hotbarController.HandleInput();
    }

    #region Initialization
    private void InitializeControllers()
    {
        // Inventory Grid Controller
        inventoryController = new UIInventoryGridController();
        inventoryController.Initialize(
            rootElement.Q<VisualElement>("inventory-grid"),
            inventorySlotTemplate,
            playerInventory
        );

        // Drag Drop Controller
        dragDropController = new UIDragDropController();
        dragDropController.Initialize(rootElement, playerInventory);

        // Seed Editor Controller
        seedEditorController = new UISeedEditorController();
        seedEditorController.Initialize(
            rootElement.Q<VisualElement>("seed-drop-slot-container"),
            rootElement.Q<VisualElement>("passive-genes-container"),
            rootElement.Q<VisualElement>("active-sequence-container"),
            geneSlotTemplate
        );

        // Spec Sheet Controller
        specSheetController = new UISpecSheetController();
        specSheetController.Initialize(rootElement.Q<VisualElement>("SeedSpecSheetPanel"));

        // Hotbar Controller
        hotbarController = new UIHotbarController();
        hotbarController.Initialize(
            rootElement.Q<ListView>("hotbar-list"),
            rootElement.Q<VisualElement>("hotbar-selector"),
            inventorySlotTemplate
        );
    }

    private void SubscribeToEvents()
    {
        // Inventory events
        inventoryController.OnSlotClicked += HandleSlotClicked;
        inventoryController.OnSlotPointerDown += HandleSlotPointerDown;

        // Drag-drop events
        dragDropController.OnInventorySwapRequested += HandleInventorySwap;
        dragDropController.OnGeneDropRequested += HandleGeneDrop;
        
        // FIX #4: Gene editor events for dragging genes back to inventory
        seedEditorController.OnGeneSlotPointerDown += HandleGeneSlotPointerDown;
    }

    private void SetupPlayerInventory()
    {
        playerInventory.Clear();
        if (startingInventory == null) return;
        
        foreach (var tool in startingInventory.startingTools) 
            if (tool != null) playerInventory.Add(new UIInventoryItem(tool));
        
        foreach (var seed in startingInventory.startingSeeds) 
            if (seed != null) playerInventory.Add(new UIInventoryItem(seed));
        
        foreach (var gene in startingInventory.startingGenes) 
            if (gene != null) playerInventory.Add(new UIInventoryItem(gene));

        // FIX #2: Fill remaining slots based on configured grid size
        while (playerInventory.Count < TotalInventorySlots)
        {
            playerInventory.Add(null);
        }
        
        // Warn if we have more items than slots
        if (playerInventory.Count > TotalInventorySlots)
        {
            Debug.LogWarning($"[GameUIManager] Starting inventory has {playerInventory.Count} items but only {TotalInventorySlots} slots configured ({inventoryRows}x{inventoryColumns}). Extra items will be truncated.");
            playerInventory = playerInventory.Take(TotalInventorySlots).ToList();
        }
    }
    #endregion

    #region Event Handlers
    private void HandleSlotClicked(int index)
    {
        // Don't select while dragging
        if (dragDropController.IsDragging()) return;

        // Update selection
        inventoryController.SetSelectedSlot(index);
        
        var selectedItem = playerInventory[index];

        // Update spec sheet for any item type
        specSheetController.DisplayItem(selectedItem);

        // If it's a seed, lock it for editing
        if (selectedItem?.OriginalData is SeedTemplate)
        {
            inventoryController.SetLockedSeedSlot(index);
            seedEditorController.DisplaySeed(selectedItem);
            
            // Update drag-drop references to gene editor slots
            dragDropController.SetGeneEditorSlots(
                seedEditorController.GetSeedContainer(),
                seedEditorController.GetPassiveContainer(),
                seedEditorController.GetActiveContainer()
            );
        }
        // If not a seed, keep gene editor as-is (don't clear it)
    }

    private void HandleSlotPointerDown(int index)
    {
        // Start drag operation
        dragDropController.StartDrag(index);
        
        // Update drag-drop controller with current inventory slots
        dragDropController.SetInventorySlots(inventoryController.GetSlots());
    }
    
    // FIX #4: Handle dragging genes from gene editor
    private void HandleGeneSlotPointerDown(GeneBase gene, VisualElement slot)
    {
        if (gene == null) return; // Can't drag empty slots
        
        // Start drag from gene editor
        dragDropController.StartDragFromGeneEditor(gene, slot);
        
        // Update drag-drop controller with current inventory slots
        dragDropController.SetInventorySlots(inventoryController.GetSlots());
    }

    private void HandleInventorySwap(int fromIndex, int toIndex)
    {
        // Swap items in data
        var temp = playerInventory[fromIndex];
        playerInventory[fromIndex] = playerInventory[toIndex];
        playerInventory[toIndex] = temp;
        
        // Update indices
        inventoryController.UpdateIndicesAfterSwap(fromIndex, toIndex);
        
        // Refresh visuals
        inventoryController.RefreshVisuals();
    }

    private void HandleGeneDrop(int dragSourceIndex, VisualElement targetSlot, string slotType)
    {
        var draggedItem = playerInventory[dragSourceIndex];
        if (draggedItem == null) return;
        
        bool validDrop = false;
        
        if (draggedItem.OriginalData is GeneBase gene)
        {
            // Validate gene category matches slot type
            validDrop = slotType switch
            {
                "passive" => gene.Category == GeneCategory.Passive,
                "active" => gene.Category == GeneCategory.Active,
                "modifier" => gene.Category == GeneCategory.Modifier,
                "payload" => gene.Category == GeneCategory.Payload,
                _ => false
            };
            
            if (validDrop)
            {
                Debug.Log($"Inserting {gene.geneName} into {slotType} slot");
                seedEditorController.UpdateGeneSlot(targetSlot, gene);
                // TODO: Actually modify the seed's runtime state here
            }
            else
            {
                Debug.Log($"Invalid drop: {gene.Category} gene cannot go in {slotType} slot");
            }
        }
        else if (draggedItem.OriginalData is SeedTemplate && slotType == "seed")
        {
            // Dropping a seed into the seed slot - lock it for editing
            inventoryController.SetLockedSeedSlot(dragSourceIndex);
            seedEditorController.DisplaySeed(draggedItem);
            
            // Update drag-drop references
            dragDropController.SetGeneEditorSlots(
                seedEditorController.GetSeedContainer(),
                seedEditorController.GetPassiveContainer(),
                seedEditorController.GetActiveContainer()
            );
            
            validDrop = true;
        }
    }
    #endregion

    #region Panel Switching
    private void HandleRunStateChanged(RunState newState)
    {
        if (newState == RunState.Planning) ShowPlanningUI();
        else ShowHUD();
    }

    private void ShowPlanningUI()
    {
        planningPanel.style.display = DisplayStyle.Flex;
        hudPanel.style.display = DisplayStyle.None;
    }

    private void ShowHUD()
    {
        planningPanel.style.display = DisplayStyle.None;
        hudPanel.style.display = DisplayStyle.Flex;
    }
    #endregion
}
