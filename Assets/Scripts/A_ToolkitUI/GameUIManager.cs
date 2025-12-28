using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using WegoSystem;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.UI.Genes;
using Abracodabra.UI.Tooltips;

public class UIInventoryItem
{
    public Sprite Icon { get; }
    public int StackSize { get; set; } = 1;
    public object OriginalData { get; }
    public PlantGeneRuntimeState SeedRuntimeState { get; }

    public UIInventoryItem(object data)
    {
        OriginalData = data;
        if (data is SeedTemplate seed)
        {
            Icon = seed.icon;
            SeedRuntimeState = seed.CreateRuntimeState();
        }
        else if (data is ToolDefinition tool)
        {
            Icon = tool.icon;
        }
        else if (data is GeneBase gene)
        {
            Icon = gene.icon;
        }
    }
}

public class GameUIManager : MonoBehaviour
{
    [Header("UI Templates")]
    [SerializeField] private VisualTreeAsset inventorySlotTemplate;
    [SerializeField] private VisualTreeAsset geneSlotTemplate;

    [Header("Starting Items")]
    [SerializeField] private StartingInventory startingInventory;

    private List<UIInventoryItem> playerInventory = new List<UIInventoryItem>();
    private List<VisualElement> inventorySlots = new List<VisualElement>();

    #region UI Element References
    private VisualElement planningPanel, hudPanel;
    private VisualElement inventoryGrid;
    private ListView hotbarList;
    private VisualElement hotbarSelector;
    private VisualElement seedEditorPanel, seedSpecSheetPanel;
    private VisualElement seedDropSlotContainer, passiveGenesContainer, activeSequenceContainer;
    private Image seedIcon;
    private Label seedNameText, qualityText, descriptionText;
    private Label maturityTimeText, energyBalanceText, yieldText;
    private VisualElement attributeContainer, sequenceContainer, synergiesContainer, warningsContainer;
    private Label cycleTimeText;
    #endregion

    private int selectedHotbarIndex = 0;
    private int selectedInventoryIndex = -1;
    private UIInventoryItem currentlyEditingSeed;

    void Start()
    {
        if (inventorySlotTemplate == null || geneSlotTemplate == null)
        {
            Debug.LogError("CRITICAL: UI Template assets are not assigned in the GameUIManager Inspector! Please drag your 'InventorySlot.uxml' and 'GeneSlot.uxml' files into the corresponding fields on the '_UIDocument_GameUI' GameObject.", this);
            this.enabled = false;
            return;
        }

        var root = GetComponent<UIDocument>().rootVisualElement;

        #region Queries
        planningPanel = root.Q<VisualElement>("PlanningPanel");
        hudPanel = root.Q<VisualElement>("HUDPanel");
        inventoryGrid = root.Q<VisualElement>("inventory-grid");
        hotbarList = root.Q<ListView>("hotbar-list");
        hotbarSelector = root.Q<VisualElement>("hotbar-selector");
        seedEditorPanel = root.Q<VisualElement>("SeedEditorPanel");
        seedDropSlotContainer = root.Q<VisualElement>("seed-drop-slot-container");
        passiveGenesContainer = root.Q<VisualElement>("passive-genes-container");
        activeSequenceContainer = root.Q<VisualElement>("active-sequence-container");
        seedSpecSheetPanel = root.Q<VisualElement>("SeedSpecSheetPanel");
        seedIcon = seedSpecSheetPanel.Q<Image>("seed-icon");
        seedNameText = seedSpecSheetPanel.Q<Label>("seed-name-text");
        qualityText = seedSpecSheetPanel.Q<Label>("quality-text");
        descriptionText = seedSpecSheetPanel.Q<Label>("description-text");
        maturityTimeText = seedSpecSheetPanel.Q<Label>("maturity-time-text");
        energyBalanceText = seedSpecSheetPanel.Q<Label>("energy-balance-text");
        yieldText = seedSpecSheetPanel.Q<Label>("yield-text");
        attributeContainer = seedSpecSheetPanel.Q<VisualElement>("attribute-breakdown-container");
        sequenceContainer = seedSpecSheetPanel.Q<VisualElement>("sequence-breakdown-container");
        synergiesContainer = seedSpecSheetPanel.Q<VisualElement>("synergies-container");
        warningsContainer = seedSpecSheetPanel.Q<VisualElement>("warnings-container");
        cycleTimeText = seedSpecSheetPanel.Q<Label>("cycle-time-text");
        #endregion

        SetupPlayerInventory();
        PopulateInventoryGrid(); 
        SetupHotbarListView(hotbarList, playerInventory.Take(8).ToList());
        
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnRunStateChanged += HandleRunStateChanged;
            HandleRunStateChanged(RunManager.Instance.CurrentState);
        }
        else
        {
            ShowPlanningUI();
        }
        
        ClearSeedEditor();
        ClearSpecSheet();
        root.schedule.Execute(() => SelectHotbarSlot(0)).StartingIn(10);
    }

    void Update()
    {
        HandleHotbarInput();
    }
    
    #region Inventory Interaction
    private void PopulateInventoryGrid()
    {
        inventoryGrid.Clear();
        inventorySlots.Clear();

        for (int i = 0; i < playerInventory.Count; i++)
        {
            var newSlot = inventorySlotTemplate.Instantiate();
            newSlot.userData = i;
            
            newSlot.RegisterCallback<PointerDownEvent>(evt => 
            {
                int index = (int)(evt.currentTarget as VisualElement).userData;
                SelectInventorySlot(index);
            });
            
            inventorySlots.Add(newSlot);
            inventoryGrid.Add(newSlot);
        }
        RefreshInventoryVisuals();
    }

    private void RefreshInventoryVisuals()
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            var element = inventorySlots[i];
            var item = playerInventory[i];
            
            var icon = element.Q<Image>("icon");
            var stack = element.Q<Label>("stack-size");

            if (item != null)
            {
                icon.sprite = item.Icon;
                icon.style.display = DisplayStyle.Flex;
                stack.text = item.StackSize > 1 ? item.StackSize.ToString() : "";
            }
            else
            {
                icon.style.display = DisplayStyle.None;
                stack.text = "";
            }
        }
    }

    private void SelectInventorySlot(int index)
    {
        if (selectedInventoryIndex >= 0 && selectedInventoryIndex < inventorySlots.Count)
        {
            inventorySlots[selectedInventoryIndex].RemoveFromClassList("slot--selected");
        }

        selectedInventoryIndex = index;

        if (selectedInventoryIndex >= 0 && selectedInventoryIndex < inventorySlots.Count)
        {
            inventorySlots[selectedInventoryIndex].AddToClassList("slot--selected");
            var selectedItem = playerInventory[selectedInventoryIndex];

            UpdateSpecSheet(selectedItem);

            if (selectedItem?.OriginalData is SeedTemplate)
            {
                SetEditingSeed(selectedItem);
            }
            else
            {
                ClearSeedEditor();
            }
        }
        else
        {
            UpdateSpecSheet(null);
            ClearSeedEditor();
        }
    }
    #endregion

    #region Seed Editor Logic
    private void SetEditingSeed(UIInventoryItem seedItem)
    {
        currentlyEditingSeed = seedItem;
        DisplaySeedInEditor(seedItem);
    }

    private void ClearSeedEditor()
    {
        currentlyEditingSeed = null;
        seedDropSlotContainer.Clear();
        passiveGenesContainer.Clear();
        activeSequenceContainer.Clear();

        var emptySeedSlot = geneSlotTemplate.Instantiate();
        var label = emptySeedSlot.Q<Label>("tier-label");
        if(label != null) label.text = "SEED";
        emptySeedSlot.Q("icon").style.display = DisplayStyle.None;
        emptySeedSlot.AddToClassList("gene-slot--seed");
        emptySeedSlot.name = "seed-drop-slot";
        seedDropSlotContainer.Add(emptySeedSlot);
    }

    private void DisplaySeedInEditor(UIInventoryItem seedItem)
    {
        if (seedItem == null || seedItem.OriginalData is not SeedTemplate template)
        {
            ClearSeedEditor();
            return;
        }

        seedDropSlotContainer.Clear();
        passiveGenesContainer.Clear();
        activeSequenceContainer.Clear();

        var seedSlot = geneSlotTemplate.Instantiate();
        BindGeneSlot(seedSlot, seedItem.OriginalData);
        seedDropSlotContainer.Add(seedSlot);

        var runtimeState = seedItem.SeedRuntimeState;

        for (int i = 0; i < template.passiveSlotCount; i++)
        {
            var passiveSlot = geneSlotTemplate.Instantiate();
            var geneInstance = (i < runtimeState.passiveInstances.Count) ? runtimeState.passiveInstances[i] : null;
            BindGeneSlot(passiveSlot, geneInstance?.GetGene());
            passiveGenesContainer.Add(passiveSlot);
        }
        
        for (int i = 0; i < template.activeSequenceLength; i++)
        {
            var sequenceRow = new VisualElement();
            sequenceRow.AddToClassList("sequence-row");
            activeSequenceContainer.Add(sequenceRow);

            var sequenceData = (i < runtimeState.activeSequence.Count) ? runtimeState.activeSequence[i] : null;

            var activeSlot = geneSlotTemplate.Instantiate();
            BindGeneSlot(activeSlot, sequenceData?.activeInstance?.GetGene());
            sequenceRow.Add(activeSlot);
            
            var modifierSlot = geneSlotTemplate.Instantiate();
            var payloadSlot = geneSlotTemplate.Instantiate();
            BindGeneSlot(modifierSlot, sequenceData?.modifierInstances.FirstOrDefault()?.GetGene());
            BindGeneSlot(payloadSlot, sequenceData?.payloadInstances.FirstOrDefault()?.GetGene());
            sequenceRow.Add(modifierSlot);
            sequenceRow.Add(payloadSlot);
        }
    }

    private void BindGeneSlot(VisualElement slot, object data)
    {
        var background = slot.Q("background");
        var icon = slot.Q<Image>("icon");
        var tierLabel = slot.Q<Label>("tier-label");

        background.ClearClassList();
        background.AddToClassList("gene-slot__background");

        if (data == null)
        {
            icon.style.display = DisplayStyle.None;
            tierLabel.text = "";
            return;
        }

        icon.style.display = DisplayStyle.Flex;

        if (data is GeneBase gene)
        {
            icon.sprite = gene.icon;
            tierLabel.text = $"T{gene.tier}";
            background.AddToClassList($"gene-slot--{gene.Category.ToString().ToLower()}");
        }
        else if (data is SeedTemplate seed)
        {
            icon.sprite = seed.icon;
            tierLabel.text = "SEED";
            background.AddToClassList("gene-slot--seed");
        }
    }

    private void UpdateSpecSheet(UIInventoryItem item)
    {
        if (item == null || item.OriginalData is not SeedTemplate seedTemplate)
        {
            ClearSpecSheet();
            return;
        }

        var data = SeedTooltipData.CreateFromSeed(seedTemplate, item.SeedRuntimeState);
        if (data == null)
        {
            ClearSpecSheet();
            return;
        }

        seedIcon.sprite = item.Icon;
        seedNameText.text = data.seedName;
        qualityText.text = SeedQualityCalculator.GetQualityDescription(data.qualityTier);
        qualityText.style.color = SeedQualityCalculator.GetQualityColor(data.qualityTier);
        descriptionText.text = seedTemplate.description;

        maturityTimeText.text = $"Maturity: {data.estimatedMaturityTicks:F0} ticks";
        energyBalanceText.text = $"Energy Balance: {data.energySurplusPerCycle:F1} E/cycle";
        yieldText.text = $"Primary Yield: {data.primaryYieldSummary}";

        attributeContainer.Clear();
        CreateAttributeDisplay("Growth", data.growthSpeedMultiplier);
        CreateAttributeDisplay("Storage", data.energyStorageMultiplier);
        CreateAttributeDisplay("Generation", data.energyGenerationMultiplier);
        CreateAttributeDisplay("Yield", data.fruitYieldMultiplier);
        CreateAttributeDisplay("Defense", data.defenseMultiplier);
        
        cycleTimeText.text = $"Cycle Time: {data.totalCycleTime} ticks";
        sequenceContainer.Clear();
        foreach (var slot in data.sequenceSlots)
        {
            var label = new Label($"A{slot.position}: {slot.actionName} ({slot.modifiedCost:F0}E)");
            sequenceContainer.Add(label);
        }

        synergiesContainer.Clear();
        warningsContainer.Clear();
        foreach(var synergy in data.synergies)
        {
            var label = new Label($"✓ {synergy}");
            label.style.color = new StyleColor(new Color(0.5f, 1f, 0.5f));
            synergiesContainer.Add(label);
        }
        foreach(var warning in data.warnings)
        {
            var label = new Label($"⚠ {warning}");
            label.style.color = new StyleColor(new Color(1f, 0.8f, 0.5f));
            warningsContainer.Add(label);
        }
    }

    private void ClearSpecSheet()
    {
        seedNameText.text = "Select a Seed";
        qualityText.text = "Awaiting Analysis...";
        qualityText.style.color = Color.gray;
        descriptionText.text = "Select an item from the inventory to see its details.";
        seedIcon.sprite = null;
        maturityTimeText.text = "Maturity: --";
        energyBalanceText.text = "Energy Balance: --";
        yieldText.text = "Primary Yield: --";
        attributeContainer.Clear();
        sequenceContainer.Clear();
        synergiesContainer.Clear();
        warningsContainer.Clear();
    }

    private void CreateAttributeDisplay(string label, float value)
    {
        var labelElement = new Label($"{label}: {value:F2}x");
        labelElement.style.fontSize = 13;
        attributeContainer.Add(labelElement);
    }
    #endregion
    
    #region Inventory & Hotbar Logic
    private void SetupPlayerInventory()
    {
        playerInventory.Clear();
        if (startingInventory == null) return;
        
        foreach (var tool in startingInventory.startingTools) if (tool != null) playerInventory.Add(new UIInventoryItem(tool));
        foreach (var seed in startingInventory.startingSeeds) if (seed != null) playerInventory.Add(new UIInventoryItem(seed));
        foreach (var gene in startingInventory.startingGenes) if (gene != null) playerInventory.Add(new UIInventoryItem(gene));

        while (playerInventory.Count < 24)
        {
            playerInventory.Add(null);
        }
    }

    private void SetupHotbarListView(ListView listView, List<UIInventoryItem> itemList)
    {
        if (listView == null) return;
        listView.fixedItemHeight = 74;
        listView.selectionType = SelectionType.None;
        listView.itemsSource = itemList;
        listView.makeItem = () => inventorySlotTemplate.Instantiate();
        listView.bindItem = (element, index) =>
        {
            var icon = element.Q<Image>("icon");
            var stack = element.Q<Label>("stack-size");

            if (itemList[index] != null)
            {
                icon.sprite = itemList[index].Icon;
                icon.style.display = DisplayStyle.Flex;
                stack.text = itemList[index].StackSize > 1 ? itemList[index].StackSize.ToString() : "";
            }
            else
            {
                icon.style.display = DisplayStyle.None;
                stack.text = "";
            }
        };
    }
    
    private void HandleHotbarInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectHotbarSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectHotbarSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectHotbarSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectHotbarSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectHotbarSlot(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SelectHotbarSlot(5);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SelectHotbarSlot(6);
        if (Input.GetKeyDown(KeyCode.Alpha8)) SelectHotbarSlot(7);
    }

    private void SelectHotbarSlot(int index)
    {
        if (index < 0 || hotbarList == null || index >= hotbarList.itemsSource.Count) return;

        selectedHotbarIndex = index;
        
        var selectedSlotElement = hotbarList.Query(className: "slot").AtIndex(selectedHotbarIndex);

        if (selectedSlotElement != null)
        {
            hotbarSelector.style.left = selectedSlotElement.layout.xMin;
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