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
    private int selectedInventoryIndex = -1; // FIX #4: Track selected item for hover fallback

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
        // DIAGNOSTIC: Show what we're searching for
        Debug.Log($"[GameUIManager] === ELEMENT SEARCH DIAGNOSTICS ===");
        
        // CORRECT: We need to find the INVENTORY panel, not the gene editor!
        // The inventory is in: <VisualElement name="InventoryPanel" class="main-panel">
        var inventoryPanel = rootElement.Q<VisualElement>("InventoryPanel");
        
        if (inventoryPanel != null)
        {
            Debug.Log($"[GameUIManager] ✓ Found INVENTORY panel by name: InventoryPanel");
        }
        else
        {
            // Fallback: try by class
            inventoryPanel = rootElement.Q(className: "main-panel");
            Debug.Log($"[GameUIManager] Found inventory panel by class: {(inventoryPanel != null ? "YES" : "NO")}");
        }
        
        Debug.Log($"[GameUIManager] === STARTING WIDTH CALCULATION ===");
        
        // STEP-BY-STEP CALCULATION:
        // 1. Each slot is 64px wide with 5px margin on each side
        int slotWidth = 64;
        int slotMarginLeft = 5;
        int slotMarginRight = 5;
        int totalSpacePerSlot = slotWidth + slotMarginLeft + slotMarginRight; // 74px
        
        Debug.Log($"[GameUIManager] Slot calculation: {slotWidth}px width + {slotMarginLeft}px + {slotMarginRight}px margin = {totalSpacePerSlot}px per slot");
        
        // 2. Grid width = number of columns × space per slot
        int gridWidth = inventoryColumns * totalSpacePerSlot;
        Debug.Log($"[GameUIManager] Grid width: {inventoryColumns} columns × {totalSpacePerSlot}px = {gridWidth}px");
        
        // 3. Panel has 15px padding on left and right sides
        int panelPaddingLeft = 15;
        int panelPaddingRight = 15;
        int totalPanelPadding = panelPaddingLeft + panelPaddingRight;
        
        // 4. Panel width = grid width + left padding + right padding
        int panelWidth = gridWidth + totalPanelPadding;
        Debug.Log($"[GameUIManager] Panel width: {gridWidth}px grid + {totalPanelPadding}px padding = {panelWidth}px TOTAL");
        Debug.Log($"[GameUIManager] More columns = WIDER inventory panel | Less columns = NARROWER inventory panel");
        
        if (inventoryPanel != null)
        {
            inventoryPanel.style.width = panelWidth;
            inventoryPanel.style.minWidth = panelWidth;
            inventoryPanel.style.maxWidth = panelWidth;
            inventoryPanel.style.flexGrow = 0; // Don't grow
            inventoryPanel.style.flexShrink = 0; // Don't shrink
            
            // DEBUG: Add visual red border to confirm which element we're sizing
            inventoryPanel.style.borderLeftWidth = 5;
            inventoryPanel.style.borderLeftColor = Color.red;
            inventoryPanel.style.borderRightWidth = 5;
            inventoryPanel.style.borderRightColor = Color.red;
            
            Debug.Log($"[GameUIManager] ✓ Set INVENTORY panel width to {panelWidth}px");
            Debug.Log($"[GameUIManager] ✓ Added RED BORDERS to inventory panel");
        }
        else
        {
            Debug.LogError("[GameUIManager] ✗ Could not find inventory panel element!");
        }
        
        // Inventory Grid Controller
        inventoryController = new UIInventoryGridController();
        var inventoryGridElement = rootElement.Q<VisualElement>("inventory-grid");
        
        if (inventoryGridElement != null)
        {
            // Set exact grid width
            inventoryGridElement.style.width = gridWidth;
            inventoryGridElement.style.minWidth = gridWidth;
            inventoryGridElement.style.maxWidth = gridWidth;
            inventoryGridElement.style.flexShrink = 0;
            inventoryGridElement.style.flexGrow = 0;
            inventoryGridElement.style.alignSelf = Align.FlexStart;
            Debug.Log($"[GameUIManager] ✓ Set grid width to {gridWidth}px");
        }
        else
        {
            Debug.LogError("[GameUIManager] ✗ Could not find inventory-grid element!");
        }
        
        Debug.Log($"[GameUIManager] === COMPLETE ===");
        Debug.Log($"[GameUIManager] RED borders mark the INVENTORY panel (should match column count)");
        
        inventoryController.Initialize(
            inventoryGridElement,
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
        inventoryController.OnSlotHoverEnter += HandleInventoryHover; // FIX #4
        inventoryController.OnSlotHoverExit += HandleHoverExit; // FIX #4

        // Drag-drop events
        dragDropController.OnInventorySwapRequested += HandleInventorySwap;
        dragDropController.OnGeneDropRequested += HandleGeneDrop;
        dragDropController.OnDragStarted += HandleDragStarted; // FIX #2
        dragDropController.OnDragEnded += HandleDragEnded; // FIX #2
        
        // Gene editor events
        seedEditorController.OnGeneSlotPointerDown += HandleGeneSlotPointerDown;
        seedEditorController.OnGeneSlotHoverEnter += HandleGeneHover; // FIX #4
        seedEditorController.OnGeneSlotHoverExit += HandleHoverExit; // FIX #4
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

        // FIX #4: Track selected index
        selectedInventoryIndex = index;

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
    
    // FIX #4: Handle hovering over inventory items
    private void HandleInventoryHover(int index)
    {
        if (dragDropController.IsDragging()) return; // Don't show tooltip while dragging
        
        var item = playerInventory[index];
        if (item != null)
        {
            specSheetController.DisplayItem(item);
        }
    }
    
    // FIX #4: Handle hovering over gene editor genes
    private void HandleGeneHover(GeneBase gene)
    {
        if (dragDropController.IsDragging()) return; // Don't show tooltip while dragging
        
        if (gene != null)
        {
            specSheetController.DisplayGene(gene);
        }
    }
    
    // FIX #4: Clear spec sheet when not hovering over anything
    private void HandleHoverExit()
    {
        // Don't clear if we have a selected item
        if (selectedInventoryIndex >= 0 && selectedInventoryIndex < playerInventory.Count)
        {
            var selectedItem = playerInventory[selectedInventoryIndex];
            specSheetController.DisplayItem(selectedItem);
        }
    }
    
    // FIX #2: Handle drag started - highlight compatible slots
    private void HandleDragStarted(GeneCategory? category)
    {
        seedEditorController.HighlightCompatibleSlots(category);
    }
    
    // FIX #2: Handle drag ended - clear highlighting
    private void HandleDragEnded()
    {
        seedEditorController.ClearSlotHighlighting();
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
