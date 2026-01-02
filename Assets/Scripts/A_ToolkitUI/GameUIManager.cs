using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using WegoSystem;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Core;
using Abracodabra.UI.Toolkit;
using Abracodabra.UI.Genes;

namespace Abracodabra.UI.Toolkit
{
/// <summary>
/// Main UI Manager - Fully functional gene editor with proper data persistence.
/// Now integrates with InventoryService for game system compatibility.
/// 
/// HUD includes: hunger bar, tick counter, selected item tooltip
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

    // Shared inventory data - this is THE authoritative inventory list
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
    private Button startDayButton;

    // HUD Elements
    private VisualElement hungerBarFill;
    private Label hungerText;
    private Label tickText;
    private VisualElement hudTooltipPanel;
    private Image hudTooltipIcon;
    private Label hudTooltipName;
    private Label hudTooltipType;
    private Label hudTooltipDescription;

    // System references
    private PlayerHungerSystem playerHungerSystem;

    private int TotalInventorySlots => inventoryRows * inventoryColumns;

    void Start()
    {
        if (inventorySlotTemplate == null || geneSlotTemplate == null)
        {
            Debug.LogError("CRITICAL: UI Template assets are not assigned in the GameUIManager Inspector!");
            this.enabled = false;
            return;
        }

        rootElement = GetComponent<UIDocument>().rootVisualElement;

        // Query UI panels
        planningPanel = rootElement.Q<VisualElement>("PlanningPanel");
        hudPanel = rootElement.Q<VisualElement>("HUDPanel");

        // Initialize inventory data
        SetupPlayerInventory();

        // Register inventory with the service (BEFORE initializing controllers)
        InventoryService.Register(playerInventory, inventoryColumns, inventoryRows);

        // Initialize all controllers
        InitializeControllers();

        // Initialize HUD elements
        InitializeHUD();

        // Subscribe to controller events
        SubscribeToEvents();

        // Subscribe to inventory service changes
        InventoryService.OnInventoryChanged += HandleInventoryServiceChanged;

        // Create Start Day button
        CreateStartDayButton();

        // Initial setup
        inventoryController.PopulateGrid();
        RefreshHotbar();
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

    void OnDestroy()
    {
        // Unregister from inventory service
        InventoryService.OnInventoryChanged -= HandleInventoryServiceChanged;
        InventoryService.Unregister();

        // Unsubscribe from events
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnRunStateChanged -= HandleRunStateChanged;
        }

        if (playerHungerSystem != null)
        {
            playerHungerSystem.OnHungerChanged -= UpdateHungerDisplay;
        }

        if (TickManager.Instance != null)
        {
            TickManager.Instance.OnTickAdvanced -= UpdateTickDisplay;
        }

        HotbarSelectionService.OnSelectionChanged -= HandleHotbarSelectionChanged;
    }

    #region Initialization
    private void InitializeControllers()
    {
        // Find panel and grid elements
        var inventoryPanel = rootElement.Q<VisualElement>("InventoryPanel");
        var geneEditorPanel = rootElement.Q<VisualElement>("SeedEditorPanel");
        var specSheetPanel = rootElement.Q<VisualElement>("SeedSpecSheetPanel");
        var inventoryGridElement = rootElement.Q<VisualElement>("inventory-grid");

        Debug.Log($"[GameUIManager] === PANEL SIZING: FIXED MODE ===");

        // Calculate dimensions
        int slotWidth = 64;
        int slotMarginLeft = 5;
        int slotMarginRight = 5;
        int totalSpacePerSlot = slotWidth + slotMarginLeft + slotMarginRight;

        int gridWidth = inventoryColumns * totalSpacePerSlot;
        int panelPaddingLeft = 15;
        int panelPaddingRight = 15;
        int totalPanelPadding = panelPaddingLeft + panelPaddingRight;
        int inventoryPanelWidth = gridWidth + totalPanelPadding;

        // SET INVENTORY PANEL
        if (inventoryPanel != null)
        {
            inventoryPanel.style.width = inventoryPanelWidth;
            inventoryPanel.style.minWidth = inventoryPanelWidth;
            inventoryPanel.style.maxWidth = inventoryPanelWidth;
            inventoryPanel.style.flexGrow = 0;
            inventoryPanel.style.flexShrink = 0;
            inventoryPanel.style.flexBasis = inventoryPanelWidth;
        }

        // SET SPEC SHEET PANEL
        if (specSheetPanel != null)
        {
            specSheetPanel.style.width = 400;
            specSheetPanel.style.minWidth = 400;
            specSheetPanel.style.maxWidth = 400;
            specSheetPanel.style.flexGrow = 0;
            specSheetPanel.style.flexShrink = 0;
            specSheetPanel.style.flexBasis = 400;
        }

        // SET GENE EDITOR - FLEXIBLE BUT CONSTRAINED
        if (geneEditorPanel != null)
        {
            geneEditorPanel.style.width = StyleKeyword.Null;
            geneEditorPanel.style.minWidth = 300;
            geneEditorPanel.style.maxWidth = StyleKeyword.Null;
            geneEditorPanel.style.flexGrow = 1;
            geneEditorPanel.style.flexShrink = 0;
            geneEditorPanel.style.flexBasis = 0;
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
        }

        // Initialize controllers
        inventoryController = new UIInventoryGridController();
        inventoryController.Initialize(inventoryGridElement, inventorySlotTemplate, playerInventory);

        dragDropController = new UIDragDropController();
        dragDropController.Initialize(rootElement, playerInventory);

        seedEditorController = new UISeedEditorController();
        seedEditorController.Initialize(
            rootElement.Q<VisualElement>("seed-drop-slot-container"),
            rootElement.Q<VisualElement>("passive-genes-container"),
            rootElement.Q<VisualElement>("active-sequence-container"),
            geneSlotTemplate
        );

        specSheetController = new UISpecSheetController();
        specSheetController.Initialize(rootElement.Q<VisualElement>("SeedSpecSheetPanel"));

        hotbarController = new UIHotbarController();
        hotbarController.Initialize(
            rootElement.Q<ListView>("hotbar-list"),
            rootElement.Q<VisualElement>("hotbar-selector"),
            inventorySlotTemplate
        );
    }

    private void InitializeHUD()
    {
        // Get HUD elements
        hungerBarFill = rootElement.Q<VisualElement>("hunger-bar-fill");
        hungerText = rootElement.Q<Label>("hunger-text");
        tickText = rootElement.Q<Label>("tick-text");

        // Tooltip elements
        hudTooltipPanel = rootElement.Q<VisualElement>("hud-tooltip-panel");
        hudTooltipIcon = rootElement.Q<Image>("hud-tooltip-icon");
        hudTooltipName = rootElement.Q<Label>("hud-tooltip-name");
        hudTooltipType = rootElement.Q<Label>("hud-tooltip-type");
        hudTooltipDescription = rootElement.Q<Label>("hud-tooltip-description");

        // Find player hunger system
        playerHungerSystem = FindFirstObjectByType<PlayerHungerSystem>();
        if (playerHungerSystem != null)
        {
            playerHungerSystem.OnHungerChanged += UpdateHungerDisplay;
            UpdateHungerDisplay(playerHungerSystem.CurrentHunger, playerHungerSystem.MaxHunger);
        }
        else
        {
            Debug.LogWarning("[GameUIManager] PlayerHungerSystem not found - hunger display won't update");
        }

        // Subscribe to tick manager
        if (TickManager.Instance != null)
        {
            TickManager.Instance.OnTickAdvanced += UpdateTickDisplay;
            UpdateTickDisplay(TickManager.Instance.CurrentTick);
        }

        // Subscribe to hotbar selection changes for tooltip
        HotbarSelectionService.OnSelectionChanged += HandleHotbarSelectionChanged;

        Debug.Log("[GameUIManager] HUD initialized");
    }

    private void CreateStartDayButton()
    {
        var buttonContainer = new VisualElement();
        buttonContainer.style.position = Position.Absolute;
        buttonContainer.style.bottom = 120;
        buttonContainer.style.left = Length.Percent(50);
        buttonContainer.style.translate = new Translate(Length.Percent(-50), 0);
        buttonContainer.style.alignItems = Align.Center;
        buttonContainer.style.justifyContent = Justify.Center;

        startDayButton = new Button();
        startDayButton.text = "▶ START DAY";
        startDayButton.AddToClassList("start-day-button");

        startDayButton.style.fontSize = 24;
        startDayButton.style.paddingTop = 15;
        startDayButton.style.paddingBottom = 15;
        startDayButton.style.paddingLeft = 40;
        startDayButton.style.paddingRight = 40;
        startDayButton.style.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
        startDayButton.style.color = Color.white;
        startDayButton.style.borderTopLeftRadius = 10;
        startDayButton.style.borderTopRightRadius = 10;
        startDayButton.style.borderBottomLeftRadius = 10;
        startDayButton.style.borderBottomRightRadius = 10;
        startDayButton.style.borderLeftWidth = 0;
        startDayButton.style.borderRightWidth = 0;
        startDayButton.style.borderTopWidth = 0;
        startDayButton.style.borderBottomWidth = 0;
        startDayButton.style.unityFontStyleAndWeight = FontStyle.Bold;

        startDayButton.clicked += OnStartDayClicked;

        buttonContainer.Add(startDayButton);
        planningPanel.Add(buttonContainer);

        Debug.Log("[GameUIManager] Start Day button created");
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
        seedEditorController.OnSeedColorChanged += HandleSeedColorChanged;
        seedEditorController.OnGeneRemovedFromEditor += HandleGeneRemovedFromEditor;
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
            Debug.LogWarning($"[GameUIManager] Starting inventory has {playerInventory.Count} items but only {TotalInventorySlots} slots. Truncating.");
            playerInventory = playerInventory.Take(TotalInventorySlots).ToList();
        }
    }

    /// <summary>
    /// Refresh the hotbar with current inventory first row.
    /// Preserves empty slots (nulls) exactly as they appear in inventory.
    /// </summary>
    private void RefreshHotbar()
    {
        // Get exactly the first row, including nulls
        var hotbarItems = new List<UIInventoryItem>();
        for (int i = 0; i < inventoryColumns && i < playerInventory.Count; i++)
        {
            hotbarItems.Add(playerInventory[i]); // Add even if null!
        }
        hotbarController.SetupHotbar(hotbarItems);
    }
    #endregion

    #region Inventory Service Handler
    /// <summary>
    /// Called when inventory is modified via InventoryService (e.g., planting, harvesting, eating)
    /// </summary>
    private void HandleInventoryServiceChanged()
    {
        Debug.Log("[GameUIManager] Inventory changed via service - refreshing UI");
        
        // Refresh inventory grid
        inventoryController.RefreshVisuals();
        
        // Refresh hotbar (preserving empty slots)
        RefreshHotbar();
        
        // Re-select current slot to update tooltip
        if (hotbarController != null)
        {
            hotbarController.SelectSlot(HotbarSelectionService.SelectedIndex);
        }
    }
    #endregion

    #region HUD Updates
    private void UpdateHungerDisplay(float currentHunger, float maxHunger)
    {
        if (hungerBarFill != null)
        {
            float percentage = maxHunger > 0 ? (currentHunger / maxHunger) * 100f : 0f;
            hungerBarFill.style.width = Length.Percent(percentage);

            if (percentage <= 25f)
            {
                hungerBarFill.style.backgroundColor = new Color(0.85f, 0.25f, 0.25f);
            }
            else if (percentage <= 50f)
            {
                hungerBarFill.style.backgroundColor = new Color(0.9f, 0.6f, 0.2f);
            }
            else
            {
                hungerBarFill.style.backgroundColor = new Color(0.86f, 0.47f, 0.2f);
            }
        }

        if (hungerText != null)
        {
            hungerText.text = $"{Mathf.CeilToInt(currentHunger)}/{Mathf.CeilToInt(maxHunger)}";
        }
    }

    private void UpdateTickDisplay(int currentTick)
    {
        if (tickText != null)
        {
            tickText.text = $"Tick: {currentTick}";
        }
    }

    private void HandleHotbarSelectionChanged(InventoryBarItem selectedItem)
    {
        UpdateHUDTooltip(selectedItem);
    }

    private void UpdateHUDTooltip(InventoryBarItem selectedItem)
    {
        if (selectedItem == null || hudTooltipPanel == null)
        {
            if (hudTooltipPanel != null)
            {
                hudTooltipPanel.style.display = DisplayStyle.None;
            }
            return;
        }

        hudTooltipPanel.style.display = DisplayStyle.Flex;

        switch (selectedItem.Type)
        {
            case InventoryBarItem.ItemType.Seed:
                var seed = selectedItem.SeedTemplate;
                if (seed != null)
                {
                    if (hudTooltipIcon != null) hudTooltipIcon.sprite = seed.icon;
                    if (hudTooltipName != null) hudTooltipName.text = seed.templateName;
                    if (hudTooltipType != null) hudTooltipType.text = "Seed";
                    if (hudTooltipDescription != null) hudTooltipDescription.text = seed.description;
                }
                break;

            case InventoryBarItem.ItemType.Tool:
                var tool = selectedItem.ToolDefinition;
                if (tool != null)
                {
                    if (hudTooltipIcon != null) hudTooltipIcon.sprite = tool.icon;
                    if (hudTooltipName != null) hudTooltipName.text = tool.displayName;
                    if (hudTooltipType != null) hudTooltipType.text = $"Tool - {tool.toolType}";
                    if (hudTooltipDescription != null) hudTooltipDescription.text = tool.GetTooltipDescription();
                }
                break;

            case InventoryBarItem.ItemType.Gene:
                var gene = selectedItem.GeneInstance?.GetGene();
                if (gene != null)
                {
                    if (hudTooltipIcon != null) hudTooltipIcon.sprite = gene.icon;
                    if (hudTooltipName != null) hudTooltipName.text = gene.geneName;
                    if (hudTooltipType != null) hudTooltipType.text = $"Gene - {gene.Category}";
                    if (hudTooltipDescription != null) hudTooltipDescription.text = gene.description;
                }
                break;

            case InventoryBarItem.ItemType.Resource:
                var resource = selectedItem.ItemInstance;
                if (resource?.definition != null)
                {
                    if (hudTooltipIcon != null) hudTooltipIcon.sprite = resource.definition.icon;
                    if (hudTooltipName != null) hudTooltipName.text = resource.definition.itemName;
                    if (hudTooltipType != null) hudTooltipType.text = "Resource";
                    if (hudTooltipDescription != null) hudTooltipDescription.text = resource.definition.description;
                }
                break;

            default:
                hudTooltipPanel.style.display = DisplayStyle.None;
                break;
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

    private void HandleSeedColorChanged(Color newColor)
    {
        inventoryController.RefreshVisuals();
        hotbarController.RefreshHotbar();
    }

    private void HandleGeneRemovedFromEditor(GeneBase gene, int slotIndex, string slotType)
    {
        int emptySlot = playerInventory.FindIndex(item => item == null);

        if (emptySlot >= 0)
        {
            playerInventory[emptySlot] = new UIInventoryItem(gene);
            inventoryController.RefreshVisuals();
            RefreshHotbar();
            Debug.Log($"[GameUIManager] Returned {gene.geneName} to inventory slot {emptySlot}");
        }
        else
        {
            Debug.LogWarning($"[GameUIManager] No empty inventory slot to return {gene.geneName}!");
        }
    }

    private void HandleInventorySwap(int fromIndex, int toIndex)
    {
        var temp = playerInventory[fromIndex];
        playerInventory[fromIndex] = playerInventory[toIndex];
        playerInventory[toIndex] = temp;

        inventoryController.UpdateIndicesAfterSwap(fromIndex, toIndex);
        inventoryController.RefreshVisuals();
        
        // Refresh hotbar if either index is in first row
        if (fromIndex < inventoryColumns || toIndex < inventoryColumns)
        {
            RefreshHotbar();
        }
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
                int slotIndex = GetSlotIndexFromElement(targetSlot, slotType);

                Debug.Log($"[GameUIManager] Attempting to add {gene.geneName} to {slotType} slot {slotIndex}");

                bool added = seedEditorController.AddGeneToSlot(gene, slotIndex, slotType);

                if (added)
                {
                    playerInventory[dragSourceIndex] = null;
                    inventoryController.RefreshVisuals();
                    RefreshHotbar();
                    Debug.Log($"[GameUIManager] ✓ Added {gene.geneName} to editor, removed from inventory");
                }
                else
                {
                    Debug.LogWarning($"[GameUIManager] ✗ Failed to add {gene.geneName} to slot");
                }
            }
            else
            {
                Debug.Log($"[GameUIManager] Invalid drop: {gene.Category} gene cannot go in {slotType} slot");
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

    private int GetSlotIndexFromElement(VisualElement element, string slotType)
    {
        if (slotType == "passive")
        {
            var container = rootElement.Q<VisualElement>("passive-genes-container");
            if (container != null)
            {
                int index = 0;
                foreach (var child in container.Children())
                {
                    if (child.Contains(element)) return index;
                    index++;
                }
            }
        }
        else if (slotType == "active" || slotType == "modifier" || slotType == "payload")
        {
            var container = rootElement.Q<VisualElement>("active-sequence-container");
            if (container != null)
            {
                int rowIndex = 0;
                foreach (var row in container.Children())
                {
                    if (row.ClassListContains("active-sequence-header")) continue;

                    if (row.Contains(element))
                    {
                        return rowIndex;
                    }
                    rowIndex++;
                }
            }
        }

        return 0;
    }

    private void OnStartDayClicked()
    {
        Debug.Log("[GameUIManager] START DAY clicked - transitioning to Growth & Threat phase");

        if (RunManager.Instance != null)
        {
            RunManager.Instance.StartGrowthAndThreatPhase();
        }
        else
        {
            Debug.LogError("[GameUIManager] RunManager.Instance is null! Cannot start day.");
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

        rootElement.style.backgroundColor = new Color(20f / 255f, 20f / 255f, 25f / 255f);

        if (startDayButton != null)
        {
            startDayButton.style.display = DisplayStyle.Flex;
        }
    }

    private void ShowHUD()
    {
        planningPanel.style.display = DisplayStyle.None;
        hudPanel.style.display = DisplayStyle.Flex;

        rootElement.style.backgroundColor = new Color(0, 0, 0, 0);

        if (hudPanel != null)
        {
            hudPanel.style.backgroundColor = new Color(0, 0, 0, 0);
        }

        if (startDayButton != null)
        {
            startDayButton.style.display = DisplayStyle.None;
        }

        // Update HUD displays
        if (playerHungerSystem != null)
        {
            UpdateHungerDisplay(playerHungerSystem.CurrentHunger, playerHungerSystem.MaxHunger);
        }

        if (TickManager.Instance != null)
        {
            UpdateTickDisplay(TickManager.Instance.CurrentTick);
        }

        // Update tooltip for current selection
        UpdateHUDTooltip(HotbarSelectionService.SelectedItem);

        Debug.Log("[GameUIManager] HUD shown - UI background set to transparent");
    }
    #endregion
}
} // namespace Abracodabra.UI.Toolkit
