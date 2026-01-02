using System;
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
    public class GameUIManager : MonoBehaviour
    {
        [Header("UI Templates")]
        [SerializeField] VisualTreeAsset inventorySlotTemplate;
        [SerializeField] VisualTreeAsset geneSlotTemplate;

        [Header("Starting Items")]
        [SerializeField] StartingInventory startingInventory;

        [Header("Inventory Configuration")]
        [Tooltip("Number of rows in the inventory grid")]
        [SerializeField] int inventoryRows = 4;

        [Tooltip("Number of columns in the inventory grid")]
        [SerializeField] int inventoryColumns = 6;

        List<UIInventoryItem> playerInventory = new List<UIInventoryItem>();
        int selectedInventoryIndex = -1;

        UIInventoryGridController inventoryController;
        UIDragDropController dragDropController;
        UISeedEditorController seedEditorController;
        UISpecSheetController specSheetController;
        UIHotbarController hotbarController;

        VisualElement rootElement;
        VisualElement planningPanel, hudPanel;
        Button startDayButton;

        VisualElement hungerBarFill;
        Label hungerText;
        Label tickText;
        VisualElement hudTooltipPanel;
        Image hudTooltipIcon;
        Label hudTooltipName;
        Label hudTooltipType;
        Label hudTooltipDescription;

        PlayerHungerSystem playerHungerSystem;

        int TotalInventorySlots => inventoryRows * inventoryColumns;

        void Start()
        {
            if (inventorySlotTemplate == null || geneSlotTemplate == null)
            {
                Debug.LogError("CRITICAL: UI Template assets are not assigned in the GameUIManager Inspector!");
                this.enabled = false;
                return;
            }

            rootElement = GetComponent<UIDocument>().rootVisualElement;

            planningPanel = rootElement.Q<VisualElement>("PlanningPanel");
            hudPanel = rootElement.Q<VisualElement>("HUDPanel");

            SetupPlayerInventory();

            InventoryService.Register(playerInventory, inventoryColumns, inventoryRows);

            InitializeControllers();

            InitializeHUD();

            SubscribeToEvents();

            InventoryService.OnInventoryChanged += HandleInventoryServiceChanged;

            CreateStartDayButton();

            inventoryController.PopulateGrid();
            RefreshHotbar();
            seedEditorController.Clear();
            specSheetController.Clear();

            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnRunStateChanged += HandleRunStateChanged;
                HandleRunStateChanged(RunManager.Instance.CurrentState);
            }
            else
            {
                ShowPlanningUI();
            }

            rootElement.schedule.Execute(() => hotbarController.SelectSlot(0)).StartingIn(10);
        }

        void Update()
        {
            hotbarController.HandleInput();
        }

        void OnDestroy()
        {
            InventoryService.OnInventoryChanged -= HandleInventoryServiceChanged;
            InventoryService.Unregister();

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

        void InitializeControllers()
        {
            var inventoryPanel = rootElement.Q<VisualElement>("InventoryPanel");
            var geneEditorPanel = rootElement.Q<VisualElement>("SeedEditorPanel");
            var specSheetPanel = rootElement.Q<VisualElement>("SeedSpecSheetPanel");
            var inventoryGridElement = rootElement.Q<VisualElement>("inventory-grid");

            Debug.Log($"[GameUIManager] === PANEL SIZING: FIXED MODE ===");

            int slotWidth = 64;
            int slotMarginLeft = 5;
            int slotMarginRight = 5;
            int totalSpacePerSlot = slotWidth + slotMarginLeft + slotMarginRight;

            int gridWidth = inventoryColumns * totalSpacePerSlot;
            int panelPaddingLeft = 15;
            int panelPaddingRight = 15;
            int totalPanelPadding = panelPaddingLeft + panelPaddingRight;
            int inventoryPanelWidth = gridWidth + totalPanelPadding;

            if (inventoryPanel != null)
            {
                inventoryPanel.style.width = inventoryPanelWidth;
                inventoryPanel.style.minWidth = inventoryPanelWidth;
                inventoryPanel.style.maxWidth = inventoryPanelWidth;
                inventoryPanel.style.flexGrow = 0;
                inventoryPanel.style.flexShrink = 0;
                inventoryPanel.style.flexBasis = inventoryPanelWidth;
            }

            if (specSheetPanel != null)
            {
                specSheetPanel.style.width = 400;
                specSheetPanel.style.minWidth = 400;
                specSheetPanel.style.maxWidth = 400;
                specSheetPanel.style.flexGrow = 0;
                specSheetPanel.style.flexShrink = 0;
                specSheetPanel.style.flexBasis = 400;
            }

            if (geneEditorPanel != null)
            {
                geneEditorPanel.style.width = StyleKeyword.Null;
                geneEditorPanel.style.minWidth = 300;
                geneEditorPanel.style.maxWidth = StyleKeyword.Null;
                geneEditorPanel.style.flexGrow = 1;
                geneEditorPanel.style.flexShrink = 0;
                geneEditorPanel.style.flexBasis = 0;
            }

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

        void InitializeHUD()
        {
            hungerBarFill = rootElement.Q<VisualElement>("hunger-bar-fill");
            hungerText = rootElement.Q<Label>("hunger-text");
            tickText = rootElement.Q<Label>("tick-text");

            hudTooltipPanel = rootElement.Q<VisualElement>("hud-tooltip-panel");
            hudTooltipIcon = rootElement.Q<Image>("hud-tooltip-icon");
            hudTooltipName = rootElement.Q<Label>("hud-tooltip-name");
            hudTooltipType = rootElement.Q<Label>("hud-tooltip-type");
            hudTooltipDescription = rootElement.Q<Label>("hud-tooltip-description");

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

            if (TickManager.Instance != null)
            {
                TickManager.Instance.OnTickAdvanced += UpdateTickDisplay;
                UpdateTickDisplay(TickManager.Instance.CurrentTick);
            }

            HotbarSelectionService.OnSelectionChanged += HandleHotbarSelectionChanged;

            Debug.Log("[GameUIManager] HUD initialized");
        }

        void CreateStartDayButton()
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

        void SubscribeToEvents()
        {
            inventoryController.OnSlotClicked += HandleSlotClicked;
            inventoryController.OnSlotPointerDown += HandleSlotPointerDown;
            inventoryController.OnSlotHoverEnter += HandleInventoryHover;
            inventoryController.OnSlotHoverExit += HandleHoverExit;

            dragDropController.OnInventorySwapRequested += HandleInventorySwap;
            dragDropController.OnGeneDropRequested += HandleGeneDrop;
            dragDropController.OnDragStarted += HandleDragStarted;
            dragDropController.OnDragEnded += HandleDragEnded;
            dragDropController.OnGeneDroppedToInventory += HandleGeneDroppedToInventory;

            seedEditorController.OnGeneSlotPointerDown += HandleGeneSlotPointerDown;
            seedEditorController.OnGeneSlotHoverEnter += HandleGeneHover;
            seedEditorController.OnGeneSlotHoverExit += HandleHoverExit;
            seedEditorController.OnSeedColorChanged += HandleSeedColorChanged;
            seedEditorController.OnGeneRemovedFromEditor += HandleGeneRemovedFromEditor;
        }

        void SetupPlayerInventory()
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

        void RefreshHotbar()
        {
            var hotbarItems = new List<UIInventoryItem>();
            for (int i = 0; i < inventoryColumns && i < playerInventory.Count; i++)
            {
                hotbarItems.Add(playerInventory[i]); // Add even if null!
            }
            hotbarController.SetupHotbar(hotbarItems);
        }

        void HandleInventoryServiceChanged()
        {
            Debug.Log("[GameUIManager] Inventory changed via service - refreshing UI");

            inventoryController.RefreshVisuals();

            RefreshHotbar();

            if (hotbarController != null)
            {
                hotbarController.SelectSlot(HotbarSelectionService.SelectedIndex);
            }
        }

        void UpdateHungerDisplay(float currentHunger, float maxHunger)
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

        void UpdateTickDisplay(int currentTick)
        {
            if (tickText != null)
            {
                tickText.text = $"Tick: {currentTick}";
            }
        }

        void HandleHotbarSelectionChanged(InventoryBarItem selectedItem)
        {
            UpdateHUDTooltip(selectedItem);
        }

        void UpdateHUDTooltip(InventoryBarItem selectedItem)
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

        void HandleSlotClicked(int index)
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

        void HandleSlotPointerDown(int index)
        {
            dragDropController.StartDrag(index);
            dragDropController.SetInventorySlots(inventoryController.GetSlots());
        }

        // Updated to accept slot metadata
        void HandleGeneSlotPointerDown(GeneBase gene, VisualElement slot, int slotIndex, string slotType)
        {
            if (gene == null) return;

            dragDropController.StartDragFromGeneEditor(gene, slot, slotIndex, slotType);
            dragDropController.SetInventorySlots(inventoryController.GetSlots());
        }

        void HandleInventoryHover(int index)
        {
            if (dragDropController.IsDragging()) return;

            var item = playerInventory[index];
            if (item != null)
            {
                specSheetController.DisplayItem(item);
            }
        }

        void HandleGeneHover(GeneBase gene)
        {
            if (dragDropController.IsDragging()) return;

            if (gene != null)
            {
                specSheetController.DisplayGene(gene);
            }
        }

        void HandleHoverExit()
        {
            if (selectedInventoryIndex >= 0 && selectedInventoryIndex < playerInventory.Count)
            {
                var selectedItem = playerInventory[selectedInventoryIndex];
                specSheetController.DisplayItem(selectedItem);
            }
        }

        void HandleDragStarted(GeneCategory? category)
        {
            seedEditorController.HighlightCompatibleSlots(category);
        }

        void HandleDragEnded()
        {
            seedEditorController.ClearSlotHighlighting();
        }

        void HandleSeedColorChanged(Color newColor)
        {
            inventoryController.RefreshVisuals();
            hotbarController.RefreshHotbar();
        }

        void HandleGeneRemovedFromEditor(GeneBase gene, int slotIndex, string slotType)
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

        // New handler for when a gene is dropped from editor to inventory
        void HandleGeneDroppedToInventory(GeneBase gene, int inventoryIndex, int editorSlotIndex, string editorSlotType)
        {
            if (gene == null) return;

            // First, remove the gene from the editor's runtime state
            var removedGene = seedEditorController.RemoveGeneFromSlot(editorSlotIndex, editorSlotType);
            
            if (removedGene != null)
            {
                // Check if the inventory slot is empty or can be swapped
                var existingItem = playerInventory[inventoryIndex];
                
                if (existingItem == null)
                {
                    // Empty slot - just add the gene
                    playerInventory[inventoryIndex] = new UIInventoryItem(gene);
                    Debug.Log($"[GameUIManager] ✓ Dropped {gene.geneName} from editor to inventory slot {inventoryIndex}");
                }
                else if (existingItem.OriginalData is GeneBase existingGene)
                {
                    // Target slot has a gene - try to swap if compatible
                    if (CanGeneGoInSlot(existingGene, editorSlotType))
                    {
                        // Swap: put existing gene in the editor slot
                        seedEditorController.AddGeneToSlot(existingGene, editorSlotIndex, editorSlotType);
                        playerInventory[inventoryIndex] = new UIInventoryItem(gene);
                        Debug.Log($"[GameUIManager] ✓ Swapped {gene.geneName} with {existingGene.geneName}");
                    }
                    else
                    {
                        // Can't swap - find an empty slot instead
                        int emptySlot = playerInventory.FindIndex(item => item == null);
                        if (emptySlot >= 0)
                        {
                            playerInventory[emptySlot] = new UIInventoryItem(gene);
                            Debug.Log($"[GameUIManager] ✓ Dropped {gene.geneName} to empty slot {emptySlot} (target was incompatible for swap)");
                        }
                        else
                        {
                            // No space - put it back in the editor
                            seedEditorController.AddGeneToSlot(gene, editorSlotIndex, editorSlotType);
                            Debug.LogWarning($"[GameUIManager] ✗ No space for {gene.geneName}, returned to editor");
                        }
                    }
                }
                else
                {
                    // Target slot has a non-gene item - find an empty slot instead
                    int emptySlot = playerInventory.FindIndex(item => item == null);
                    if (emptySlot >= 0)
                    {
                        playerInventory[emptySlot] = new UIInventoryItem(gene);
                        Debug.Log($"[GameUIManager] ✓ Dropped {gene.geneName} to empty slot {emptySlot} (target had non-gene item)");
                    }
                    else
                    {
                        // No space - put it back in the editor
                        seedEditorController.AddGeneToSlot(gene, editorSlotIndex, editorSlotType);
                        Debug.LogWarning($"[GameUIManager] ✗ No space for {gene.geneName}, returned to editor");
                    }
                }

                inventoryController.RefreshVisuals();
                RefreshHotbar();
            }
        }

        bool CanGeneGoInSlot(GeneBase gene, string slotType)
        {
            return slotType switch
            {
                "passive" => gene.Category == GeneCategory.Passive,
                "active" => gene.Category == GeneCategory.Active,
                "modifier" => gene.Category == GeneCategory.Modifier,
                "payload" => gene.Category == GeneCategory.Payload,
                _ => false
            };
        }

        void HandleInventorySwap(int fromIndex, int toIndex)
        {
            var temp = playerInventory[fromIndex];
            playerInventory[fromIndex] = playerInventory[toIndex];
            playerInventory[toIndex] = temp;

            inventoryController.UpdateIndicesAfterSwap(fromIndex, toIndex);
            inventoryController.RefreshVisuals();

            if (fromIndex < inventoryColumns || toIndex < inventoryColumns)
            {
                RefreshHotbar();
            }
        }

        void HandleGeneDrop(int dragSourceIndex, VisualElement targetSlot, string slotType)
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

        int GetSlotIndexFromElement(VisualElement element, string slotType)
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

        void OnStartDayClicked()
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

        void HandleRunStateChanged(RunState newState)
        {
            if (newState == RunState.Planning) ShowPlanningUI();
            else ShowHUD();
        }

        void ShowPlanningUI()
        {
            planningPanel.style.display = DisplayStyle.Flex;
            hudPanel.style.display = DisplayStyle.None;

            rootElement.style.backgroundColor = new Color(20f / 255f, 20f / 255f, 25f / 255f);

            if (startDayButton != null)
            {
                startDayButton.style.display = DisplayStyle.Flex;
            }
        }

        void ShowHUD()
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

            if (playerHungerSystem != null)
            {
                UpdateHungerDisplay(playerHungerSystem.CurrentHunger, playerHungerSystem.MaxHunger);
            }

            if (TickManager.Instance != null)
            {
                UpdateTickDisplay(TickManager.Instance.CurrentTick);
            }

            UpdateHUDTooltip(HotbarSelectionService.SelectedItem);

            Debug.Log("[GameUIManager] HUD shown - UI background set to transparent");
        }
    }
}
