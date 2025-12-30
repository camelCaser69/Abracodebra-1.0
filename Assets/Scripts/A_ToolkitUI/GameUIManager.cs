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
    
    [Header("Panel Sizing")]
    [Tooltip("Enable dynamic panel resizing based on inventory columns. When disabled, panels split screen equally.")]
    [SerializeField] private bool enableDynamicResizing = true;

    // Shared inventory data
    private List<UIInventoryItem> playerInventory = new List<UIInventoryItem>();
    private int selectedInventoryIndex = -1;

    // UI Controllers
    private UIInventoryGridController inventoryController;
    private UIDragDropController dragDropController;
    private UISeedEditorController seedEditorController;
    private UISpecSheetController specSheetController;
    private UIHotbarController hotbarController;

    // UI Element References
    private VisualElement rootElement;
    private VisualElement planningPanel, hudPanel;

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
        // Find panel and grid elements once
        var inventoryPanel = rootElement.Q<VisualElement>("InventoryPanel");
        var geneEditorPanel = rootElement.Q<VisualElement>("SeedEditorPanel");
        var specSheetPanel = rootElement.Q<VisualElement>("SeedSpecSheetPanel");
        var inventoryGridElement = rootElement.Q<VisualElement>("inventory-grid");
        
        Debug.Log($"[GameUIManager] === FIXING PANEL SIZING (prevent expansion) ===");
        
        // CRITICAL FIX: The key to preventing gene editor expansion is:
        // 1. Use flexBasis = 0 (NOT Auto, NOT explicit width)
        // 2. Use flexGrow = 1 (fills remaining space)
        // 3. Ensure overflow: hidden on all child containers
        
        // Calculate slot dimensions
        int slotWidth = 64;
        int slotMarginLeft = 5;
        int slotMarginRight = 5;
        int totalSpacePerSlot = slotWidth + slotMarginLeft + slotMarginRight; // 74px
        
        // Calculate inventory panel width
        int gridWidth = inventoryColumns * totalSpacePerSlot;
        int panelPaddingLeft = 15;
        int panelPaddingRight = 15;
        int totalPanelPadding = panelPaddingLeft + panelPaddingRight;
        int inventoryPanelWidth = gridWidth + totalPanelPadding;
        
        Debug.Log($"[GameUIManager] Inventory panel width: {inventoryPanelWidth}px ({inventoryColumns} columns)");
        
        // SET INVENTORY PANEL - FIXED WIDTH
        if (inventoryPanel != null)
        {
            inventoryPanel.style.width = inventoryPanelWidth;
            inventoryPanel.style.minWidth = inventoryPanelWidth;
            inventoryPanel.style.maxWidth = inventoryPanelWidth;
            inventoryPanel.style.flexGrow = 0;
            inventoryPanel.style.flexShrink = 0;
            inventoryPanel.style.flexBasis = inventoryPanelWidth;
            Debug.Log($"[GameUIManager] ✓ Inventory: {inventoryPanelWidth}px (FIXED)");
        }
        
        // SET SPEC SHEET PANEL - FIXED WIDTH (400px from CSS)
        if (specSheetPanel != null)
        {
            specSheetPanel.style.width = 400;
            specSheetPanel.style.minWidth = 400;
            specSheetPanel.style.maxWidth = 400;
            specSheetPanel.style.flexGrow = 0;
            specSheetPanel.style.flexShrink = 0;
            specSheetPanel.style.flexBasis = 400;
            Debug.Log($"[GameUIManager] ✓ Spec sheet: 400px (FIXED)");
        }
        
        // SET GENE EDITOR - FLEXIBLE BUT CONSTRAINED
        // KEY FIX: flexBasis = 0 (NOT Auto!) prevents content-based sizing
        if (geneEditorPanel != null)
        {
            geneEditorPanel.style.width = StyleKeyword.Null; // Clear explicit width
            geneEditorPanel.style.minWidth = 300; // Minimum usable width
            geneEditorPanel.style.maxWidth = StyleKeyword.Null; // No maximum
            geneEditorPanel.style.flexGrow = 1; // Take remaining space
            geneEditorPanel.style.flexShrink = 0; // Don't shrink
            geneEditorPanel.style.flexBasis = 0; // KEY: Start from 0, NOT Auto!
            Debug.Log($"[GameUIManager] ✓ Gene editor: flex-grow=1, flex-basis=0 (fills remaining, won't expand)");
        }
        
        // SET GRID WIDTH
        if (inventoryGridElement != null)
        {
            inventoryGridElement.style.width = gridWidth;
            inventoryGridElement.style.minWidth = gridWidth;
            inventoryGridElement.style.maxWidth = gridWidth;
            inventoryGridElement.style.flexShrink = 0;
            inventoryGridElement.style.flexGrow = 0;
            inventoryGridElement.style.marginLeft = StyleKeyword.Auto;
            inventoryGridElement.style.marginRight = StyleKeyword.Auto;
            inventoryGridElement.style.alignSelf = Align.Center;
            Debug.Log($"[GameUIManager] ✓ Grid: {gridWidth}px (centered)");
        }
        
        Debug.Log($"[GameUIManager] === PANEL SIZING COMPLETE ===");
        
        // Continue with controller initialization
        inventoryController = new UIInventoryGridController();
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
        inventoryController.OnSlotHoverEnter += HandleInventoryHover;
        inventoryController.OnSlotHoverExit += HandleHoverExit;

        // Drag-drop events
        dragDropController.OnInventorySwapRequested += HandleInventorySwap;
        dragDropController.OnGeneDropRequested += HandleGeneDrop;
        dragDropController.OnDragStarted += HandleDragStarted;
        dragDropController.OnDragEnded += HandleDragEnded;
        
        // Gene editor events
        seedEditorController.OnGeneSlotPointerDown += HandleGeneSlotPointerDown;
        seedEditorController.OnGeneSlotHoverEnter += HandleGeneHover;
        seedEditorController.OnGeneSlotHoverExit += HandleHoverExit;
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

        while (playerInventory.Count < TotalInventorySlots)
        {
            playerInventory.Add(null);
        }
        
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
        if (dragDropController.IsDragging()) return;

        selectedInventoryIndex = index;
        inventoryController.SetSelectedSlot(index);
        
        var selectedItem = playerInventory[index];
        specSheetController.DisplayItem(selectedItem);

        if (selectedItem?.OriginalData is SeedTemplate)
        {
            inventoryController.SetLockedSeedSlot(index);
            seedEditorController.DisplaySeed(selectedItem);
            
            dragDropController.SetGeneEditorSlots(
                seedEditorController.GetSeedContainer(),
                seedEditorController.GetPassiveContainer(),
                seedEditorController.GetActiveContainer()
            );
        }
    }

    private void HandleSlotPointerDown(int index)
    {
        dragDropController.StartDrag(index);
        dragDropController.SetInventorySlots(inventoryController.GetSlots());
    }
    
    private void HandleGeneSlotPointerDown(GeneBase gene, VisualElement slot)
    {
        if (gene == null) return;
        
        dragDropController.StartDragFromGeneEditor(gene, slot);
        dragDropController.SetInventorySlots(inventoryController.GetSlots());
    }
    
    private void HandleInventoryHover(int index)
    {
        if (dragDropController.IsDragging()) return;
        
        var item = playerInventory[index];
        if (item != null)
        {
            specSheetController.DisplayItem(item);
        }
    }
    
    private void HandleGeneHover(GeneBase gene)
    {
        if (dragDropController.IsDragging()) return;
        
        if (gene != null)
        {
            specSheetController.DisplayGene(gene);
        }
    }
    
    private void HandleHoverExit()
    {
        if (selectedInventoryIndex >= 0 && selectedInventoryIndex < playerInventory.Count)
        {
            var selectedItem = playerInventory[selectedInventoryIndex];
            specSheetController.DisplayItem(selectedItem);
        }
    }
    
    private void HandleDragStarted(GeneCategory? category)
    {
        seedEditorController.HighlightCompatibleSlots(category);
    }
    
    private void HandleDragEnded()
    {
        seedEditorController.ClearSlotHighlighting();
    }

    private void HandleInventorySwap(int fromIndex, int toIndex)
    {
        var temp = playerInventory[fromIndex];
        playerInventory[fromIndex] = playerInventory[toIndex];
        playerInventory[toIndex] = temp;
        
        inventoryController.UpdateIndicesAfterSwap(fromIndex, toIndex);
        inventoryController.RefreshVisuals();
    }

    private void HandleGeneDrop(int dragSourceIndex, VisualElement targetSlot, string slotType)
    {
        var draggedItem = playerInventory[dragSourceIndex];
        if (draggedItem == null) return;
        
        bool validDrop = false;
        
        if (draggedItem.OriginalData is GeneBase gene)
        {
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
            }
            else
            {
                Debug.Log($"Invalid drop: {gene.Category} gene cannot go in {slotType} slot");
            }
        }
        else if (draggedItem.OriginalData is SeedTemplate && slotType == "seed")
        {
            inventoryController.SetLockedSeedSlot(dragSourceIndex);
            seedEditorController.DisplaySeed(draggedItem);
            
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