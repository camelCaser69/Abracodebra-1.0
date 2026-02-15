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
using Abracodabra.Ecosystem.Feeding;

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

        Label tickText;

        VisualElement waveProgressBarFill;
        int waveStartTick = 0;
        int waveEndTick = 0;

        VisualElement playerHungerBarFill;
        Label playerHungerText;

        VisualElement dorisHungerFooter;
        VisualElement dorisHungerBarFill;
        Label dorisHungerText;
        Label dorisStateText;

        // Simplified hotbar tooltip - just a label
        Label hotbarTooltipLabel;

        PlayerHungerSystem playerHungerSystem;

        MonoBehaviour dorisHungerSystemRef;
        System.Reflection.EventInfo dorisHungerChangedEvent;
        System.Reflection.EventInfo dorisStateChangedEvent;

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

            if (ToolSwitcher.Instance != null)
            {
                ToolSwitcher.Instance.OnUsesChanged += HandleToolUsesChanged;
            }

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
            // Block hotbar input while feeding popup is open
            if (FoodSelectionPopup.IsBlockingInput)
                return;

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
                playerHungerSystem.OnHungerChanged -= UpdatePlayerHungerDisplay;
            }

            if (TickManager.Instance != null)
            {
                TickManager.Instance.OnTickAdvanced -= UpdateTickDisplay;
            }

            if (ToolSwitcher.Instance != null)
            {
                ToolSwitcher.Instance.OnUsesChanged -= HandleToolUsesChanged;
            }

            HotbarSelectionService.OnSelectionChanged -= HandleHotbarSelectionChanged;

            UnsubscribeFromDoris();
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
            tickText = rootElement.Q<Label>("tick-text");

            waveProgressBarFill = rootElement.Q<VisualElement>("wave-progress-bar-fill");

            playerHungerBarFill = rootElement.Q<VisualElement>("player-hunger-bar-fill");
            playerHungerText = rootElement.Q<Label>("player-hunger-text");

            dorisHungerFooter = rootElement.Q<VisualElement>("doris-hunger-footer");
            dorisHungerBarFill = rootElement.Q<VisualElement>("doris-hunger-bar-fill");
            dorisHungerText = rootElement.Q<Label>("doris-hunger-text");

            // Simple hotbar tooltip label
            hotbarTooltipLabel = rootElement.Q<Label>("hotbar-tooltip-label");

            playerHungerSystem = FindFirstObjectByType<PlayerHungerSystem>();
            if (playerHungerSystem != null)
            {
                playerHungerSystem.OnHungerChanged += UpdatePlayerHungerDisplay;
                UpdatePlayerHungerDisplay(playerHungerSystem.CurrentHunger, playerHungerSystem.MaxHunger);
                Debug.Log("[GameUIManager] Subscribed to PlayerHungerSystem");
            }
            else
            {
                Debug.LogWarning("[GameUIManager] PlayerHungerSystem not found - player hunger display won't update");
            }

            TryFindAndSubscribeToDoris();

            if (TickManager.Instance != null)
            {
                TickManager.Instance.OnTickAdvanced += UpdateTickDisplay;
                UpdateTickDisplay(TickManager.Instance.CurrentTick);
            }

            HotbarSelectionService.OnSelectionChanged += HandleHotbarSelectionChanged;

            Debug.Log("[GameUIManager] HUD initialized");
        }

        void TryFindAndSubscribeToDoris()
        {
            if (dorisHungerFooter != null)
            {
                dorisHungerFooter.style.display = DisplayStyle.None;
            }

            var dorisType = System.Type.GetType("Abracodabra.Ecosystem.DorisHungerSystem, Assembly-CSharp");
            if (dorisType == null)
            {
                Debug.Log("[GameUIManager] DorisHungerSystem type not found - Doris UI disabled");
                return;
            }

            dorisHungerSystemRef = FindFirstObjectByType(dorisType) as MonoBehaviour;
            if (dorisHungerSystemRef == null)
            {
                Debug.Log("[GameUIManager] DorisHungerSystem instance not found in scene - Doris UI disabled");
                return;
            }

            try
            {
                dorisHungerChangedEvent = dorisType.GetEvent("OnHungerChanged");
                dorisStateChangedEvent = dorisType.GetEvent("OnStateChanged");

                if (dorisHungerChangedEvent != null)
                {
                    var handler = Delegate.CreateDelegate(
                        dorisHungerChangedEvent.EventHandlerType,
                        this,
                        typeof(GameUIManager).GetMethod("OnDorisHungerChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    );
                    dorisHungerChangedEvent.AddEventHandler(dorisHungerSystemRef, handler);
                }

                if (dorisHungerFooter != null)
                {
                    dorisHungerFooter.style.display = DisplayStyle.Flex;
                }

                var currentHungerProp = dorisType.GetProperty("CurrentHunger");
                var maxHungerProp = dorisType.GetProperty("MaxHunger");
                var currentStateProp = dorisType.GetProperty("CurrentState");

                if (currentHungerProp != null && maxHungerProp != null)
                {
                    float current = (float)currentHungerProp.GetValue(dorisHungerSystemRef);
                    float max = (float)maxHungerProp.GetValue(dorisHungerSystemRef);
                    UpdateDorisHungerDisplay(current, max);
                }

                if (currentStateProp != null)
                {
                    int state = (int)currentStateProp.GetValue(dorisHungerSystemRef);
                    UpdateDorisStateDisplayByInt(state);
                }

                Debug.Log("[GameUIManager] Successfully subscribed to DorisHungerSystem");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[GameUIManager] Failed to subscribe to Doris events: {e.Message}");
            }
        }

        void UnsubscribeFromDoris()
        {
            if (dorisHungerSystemRef != null && dorisHungerChangedEvent != null)
            {
                try
                {
                    var handler = Delegate.CreateDelegate(
                        dorisHungerChangedEvent.EventHandlerType,
                        this,
                        typeof(GameUIManager).GetMethod("OnDorisHungerChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    );
                    dorisHungerChangedEvent.RemoveEventHandler(dorisHungerSystemRef, handler);
                }
                catch { }
            }
        }

        void OnDorisHungerChanged(float currentHunger, float maxHunger)
        {
            UpdateDorisHungerDisplay(currentHunger, maxHunger);

            if (dorisHungerSystemRef != null)
            {
                var dorisType = dorisHungerSystemRef.GetType();
                var currentStateProp = dorisType.GetProperty("CurrentState");
                if (currentStateProp != null)
                {
                    int state = (int)currentStateProp.GetValue(dorisHungerSystemRef);
                    UpdateDorisStateDisplayByInt(state);
                }
            }
        }

        public void RefreshDorisReference()
        {
            UnsubscribeFromDoris();
            TryFindAndSubscribeToDoris();
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
            dragDropController.OnGeneEditorInternalMove += HandleGeneEditorInternalMove;

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
                hotbarItems.Add(playerInventory[i]);
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

        void HandleToolUsesChanged(int remainingUses)
        {
            inventoryController.RefreshVisuals();
            RefreshHotbar();

            Debug.Log($"[GameUIManager] Tool uses changed: {remainingUses} remaining - UI refreshed");
        }

        void UpdatePlayerHungerDisplay(float currentHunger, float maxHunger)
        {
            if (playerHungerBarFill != null)
            {
                float percentage = maxHunger > 0 ? (currentHunger / maxHunger) * 100f : 0f;
                playerHungerBarFill.style.width = Length.Percent(percentage);

                if (percentage >= 80f)
                {
                    playerHungerBarFill.style.backgroundColor = new Color(0.9f, 0.25f, 0.25f);
                }
                else if (percentage >= 50f)
                {
                    playerHungerBarFill.style.backgroundColor = new Color(0.95f, 0.7f, 0.2f);
                }
                else
                {
                    playerHungerBarFill.style.backgroundColor = new Color(0.4f, 0.75f, 0.4f);
                }
            }

            if (playerHungerText != null)
            {
                playerHungerText.text = $"{Mathf.CeilToInt(currentHunger)}/{Mathf.CeilToInt(maxHunger)}";
            }
        }

        void UpdateDorisHungerDisplay(float currentHunger, float maxHunger)
        {
            if (dorisHungerBarFill != null)
            {
                float percentage = maxHunger > 0 ? (currentHunger / maxHunger) * 100f : 0f;
                dorisHungerBarFill.style.width = Length.Percent(percentage);

                if (percentage >= 80f)
                {
                    dorisHungerBarFill.style.backgroundColor = new Color(0.9f, 0.25f, 0.25f);
                }
                else if (percentage >= 50f)
                {
                    dorisHungerBarFill.style.backgroundColor = new Color(0.95f, 0.6f, 0.2f);
                }
                else
                {
                    dorisHungerBarFill.style.backgroundColor = new Color(0.4f, 0.75f, 0.4f);
                }
            }

            if (dorisHungerText != null)
            {
                dorisHungerText.text = $"{Mathf.CeilToInt(currentHunger)}/{Mathf.CeilToInt(maxHunger)}";
            }
        }

        void UpdateDorisStateDisplayByInt(int stateValue)
        {
            if (dorisStateText == null) return;

            dorisStateText.RemoveFromClassList("doris-state-satisfied");
            dorisStateText.RemoveFromClassList("doris-state-hungry");
            dorisStateText.RemoveFromClassList("doris-state-starving");

            switch (stateValue)
            {
                case 0: // Satisfied
                    dorisStateText.text = "😊 Satisfied";
                    dorisStateText.AddToClassList("doris-state-satisfied");
                    break;
                case 1: // Hungry
                    dorisStateText.text = "😐 Hungry";
                    dorisStateText.AddToClassList("doris-state-hungry");
                    break;
                case 2: // Starving
                    dorisStateText.text = "😡 STARVING!";
                    dorisStateText.AddToClassList("doris-state-starving");
                    break;
            }
        }

        void UpdateTickDisplay(int currentTick)
        {
            int maxTicks = 100; // Default fallback
            int effectiveCurrentTick = currentTick;

            if (WaveManager.Instance != null && WaveManager.Instance.IsWaveActive)
            {
                var waveManagerType = WaveManager.Instance.GetType();
                var startTickField = waveManagerType.GetField("waveStartTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var endTickField = waveManagerType.GetField("waveEndTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (startTickField != null && endTickField != null)
                {
                    waveStartTick = (int)startTickField.GetValue(WaveManager.Instance);
                    waveEndTick = (int)endTickField.GetValue(WaveManager.Instance);
                    maxTicks = waveEndTick - waveStartTick;
                    effectiveCurrentTick = currentTick - waveStartTick;
                }
            }
            else if (TickManager.Instance?.Config != null)
            {
                maxTicks = TickManager.Instance.Config.ticksPerDay;
                effectiveCurrentTick = currentTick % maxTicks;
            }

            if (tickText != null)
            {
                tickText.text = $"Tick: {effectiveCurrentTick}/{maxTicks}";
            }

            if (waveProgressBarFill != null && maxTicks > 0)
            {
                float progressPercent = (float)effectiveCurrentTick / maxTicks * 100f;
                waveProgressBarFill.style.width = Length.Percent(Mathf.Clamp(progressPercent, 0f, 100f));
            }
        }

        void HandleHotbarSelectionChanged(UIInventoryItem selectedItem)
        {
            UpdateHotbarTooltip(selectedItem);
        }

        /// <summary>
        /// Simplified hotbar tooltip - just shows item name as white text centered above the monolith
        /// </summary>
        void UpdateHotbarTooltip(UIInventoryItem selectedItem)
        {
            if (hotbarTooltipLabel == null) return;

            if (selectedItem == null || !selectedItem.IsValid())
            {
                hotbarTooltipLabel.style.display = DisplayStyle.None;
                return;
            }

            string itemName = selectedItem.GetDisplayName();
            if (string.IsNullOrEmpty(itemName))
            {
                hotbarTooltipLabel.style.display = DisplayStyle.None;
                return;
            }

            hotbarTooltipLabel.text = itemName;
            hotbarTooltipLabel.style.display = DisplayStyle.Flex;
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

        void HandleGeneDroppedToInventory(GeneBase gene, int inventoryIndex, int editorSlotIndex, string editorSlotType)
        {
            seedEditorController.RemoveGeneFromSlot(editorSlotIndex, editorSlotType);

            var targetItem = playerInventory[inventoryIndex];

            if (targetItem == null)
            {
                playerInventory[inventoryIndex] = new UIInventoryItem(gene);
                Debug.Log($"[GameUIManager] Dropped {gene.geneName} to empty inventory slot {inventoryIndex}");
            }
            else if (targetItem.OriginalData is GeneBase targetGene && CanGeneGoInSlot(targetGene, editorSlotType))
            {
                playerInventory[inventoryIndex] = new UIInventoryItem(gene);
                seedEditorController.AddGeneToSlot(targetGene, editorSlotIndex, editorSlotType);
                Debug.Log($"[GameUIManager] Swapped {gene.geneName} with {targetGene.geneName}");
            }
            else
            {
                int emptySlot = playerInventory.FindIndex(item => item == null);
                if (emptySlot >= 0)
                {
                    playerInventory[emptySlot] = new UIInventoryItem(gene);
                    Debug.Log($"[GameUIManager] Target occupied - placed {gene.geneName} in empty slot {emptySlot}");
                }
                else
                {
                    seedEditorController.AddGeneToSlot(gene, editorSlotIndex, editorSlotType);
                    Debug.LogWarning($"[GameUIManager] No space in inventory - returned {gene.geneName} to editor");
                }
            }

            inventoryController.RefreshVisuals();
            RefreshHotbar();
        }

        void HandleGeneEditorInternalMove(GeneBase gene, int fromIndex, string fromType, int toIndex, string toType)
        {
            if (!CanGeneGoInSlot(gene, toType))
            {
                Debug.Log($"[GameUIManager] Cannot move {gene.Category} gene to {toType} slot");
                return;
            }

            var destinationGene = seedEditorController.GetGeneAtSlot(toIndex, toType);

            seedEditorController.RemoveGeneFromSlot(fromIndex, fromType);

            if (destinationGene != null)
            {
                if (CanGeneGoInSlot(destinationGene, fromType))
                {
                    seedEditorController.AddGeneToSlot(gene, toIndex, toType);
                    seedEditorController.AddGeneToSlot(destinationGene, fromIndex, fromType);
                    Debug.Log($"[GameUIManager] Swapped {gene.geneName} with {destinationGene.geneName}");
                }
                else
                {
                    seedEditorController.AddGeneToSlot(gene, toIndex, toType);

                    int emptySlot = playerInventory.FindIndex(item => item == null);
                    if (emptySlot >= 0)
                    {
                        playerInventory[emptySlot] = new UIInventoryItem(destinationGene);
                        inventoryController.RefreshVisuals();
                        RefreshHotbar();
                        Debug.Log($"[GameUIManager] Moved {gene.geneName} to {toType} slot, {destinationGene.geneName} to inventory");
                    }
                    else
                    {
                        seedEditorController.RemoveGeneFromSlot(toIndex, toType);
                        seedEditorController.AddGeneToSlot(destinationGene, toIndex, toType);
                        seedEditorController.AddGeneToSlot(gene, fromIndex, fromType);
                        Debug.LogWarning($"[GameUIManager] No inventory space - reverted move");
                    }
                }
            }
            else
            {
                seedEditorController.AddGeneToSlot(gene, toIndex, toType);
                Debug.Log($"[GameUIManager] Moved {gene.geneName} from {fromType}[{fromIndex}] to {toType}[{toIndex}]");
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
                "seed" => false,
                _ => false
            };
        }

        void HandleInventorySwap(int from, int to)
        {
            if (from < 0 || from >= playerInventory.Count || to < 0 || to >= playerInventory.Count) return;

            (playerInventory[from], playerInventory[to]) = (playerInventory[to], playerInventory[from]);

            inventoryController.RefreshVisuals();
            RefreshHotbar();

            Debug.Log($"[GameUIManager] Swapped inventory slots {from} and {to}");
        }

        void HandleGeneDrop(int inventoryIndex, VisualElement targetSlot, string slotType)
        {
            var item = playerInventory[inventoryIndex];
            if (item == null) return;

            GeneBase gene = item.OriginalData as GeneBase;
            if (gene == null) return;

            if (!CanGeneGoInSlot(gene, slotType))
            {
                Debug.Log($"[GameUIManager] Cannot place {gene.Category} gene in {slotType} slot");
                return;
            }

            int slotIndex = GetSlotIndexFromElement(targetSlot, slotType);
            if (slotIndex < 0) return;

            var existingGene = seedEditorController.GetGeneAtSlot(slotIndex, slotType);

            seedEditorController.AddGeneToSlot(gene, slotIndex, slotType);

            if (existingGene != null)
            {
                playerInventory[inventoryIndex] = new UIInventoryItem(existingGene);
            }
            else
            {
                playerInventory[inventoryIndex] = null;
            }

            inventoryController.RefreshVisuals();
            RefreshHotbar();
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
            Debug.Log($"[GameUIManager] Run state changed to: {newState}");

            switch (newState)
            {
                case RunState.Planning:
                    ShowPlanningUI();
                    break;

                case RunState.GrowthAndThreat:
                    ShowHUDUI();
                    break;

                case RunState.GameOver:
                    ShowHUDUI();
                    break;
            }
        }

        void ShowPlanningUI()
        {
            if (planningPanel != null) planningPanel.style.display = DisplayStyle.Flex;
            if (hudPanel != null) hudPanel.style.display = DisplayStyle.None;
        }

        void ShowHUDUI()
        {
            if (planningPanel != null) planningPanel.style.display = DisplayStyle.None;
            if (hudPanel != null) hudPanel.style.display = DisplayStyle.Flex;
        }
    }
}