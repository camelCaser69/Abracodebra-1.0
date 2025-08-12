using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Abracodabra.Genes.Core;
using Abracodabra.UI.Genes;
using WegoSystem;
using System.Reflection;

public class CompleteUISetup : MonoBehaviour
{
    [Header("Setup Options")]
    [Tooltip("If true, this GameObject will be destroyed after generation.")]
    [SerializeField] bool destroyAfterSetup = true;

    [Header("UI Styling")]
    [SerializeField] Color panelBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    [SerializeField] Color slotNormalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [Tooltip("You must assign a TextMeshPro Font Asset here. To create one, right-click a Font file > Create > TextMeshPro > Font Asset.")]
    [SerializeField] TMP_FontAsset defaultFontAsset;

    [ContextMenu("Generate Complete UI")]
    public void GenerateCompleteUI()
    {
        Debug.Log("[CompleteUISetup] Starting complete UI setup...");

        // 1. Setup Core Scene Requirements
        CreateEventSystem();
        Canvas mainCanvas = CreateMainCanvas();
        GameObject prefabContainer = new GameObject("[Generated Prefabs]");
        prefabContainer.SetActive(false);

        // 2. Create UI Panels
        GameObject planningPanel = CreatePanel("PlanningPanel", mainCanvas.transform);
        GameObject growthThreatPanel = CreatePanel("GrowthAndThreatPanel", mainCanvas.transform);
        planningPanel.SetActive(true); // Start with planning panel active
        growthThreatPanel.SetActive(false);

        // 3. Create All Required Prefab Assets (as GameObjects first)
        GameObject itemViewPrefab = CreateItemViewPrefab();
        itemViewPrefab.transform.SetParent(prefabContainer.transform);

        GameObject itemSlotPrefab = CreateItemSlotPrefab(itemViewPrefab);
        itemSlotPrefab.transform.SetParent(prefabContainer.transform);

        GameObject passiveSlotPrefab = CreateGeneSlot("PassiveSlotPrefab", GeneCategory.Passive, itemViewPrefab);
        passiveSlotPrefab.transform.SetParent(prefabContainer.transform);

        GameObject sequenceRowPrefab = CreateSequenceRowPrefab(itemViewPrefab);
        sequenceRowPrefab.transform.SetParent(prefabContainer.transform);

        // 4. Populate Panels with UI Modules
        InventoryGridController inventoryGrid = CreateInventoryGrid(planningPanel.transform, itemSlotPrefab);
        CreateInventoryBar(growthThreatPanel.transform, itemViewPrefab, inventoryGrid);
        GeneSequenceUI geneSequenceUI = CreateGeneSequenceUI(planningPanel.transform, passiveSlotPrefab, sequenceRowPrefab);

        CreateControlButtons(planningPanel.transform, growthThreatPanel.transform);
        GameObject wegoPanel = CreateDebugUI(mainCanvas.transform);

        // 5. Link everything to the UIManager
        LinkToUIManager(planningPanel, growthThreatPanel, geneSequenceUI, wegoPanel);

        Debug.Log("[CompleteUISetup] UI setup complete! You should now create Prefab Assets from the GameObjects inside '[Generated Prefabs]'.");

        if (destroyAfterSetup)
        {
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }
    }

    #region Core Scene & Panel Creation
    void CreateEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    Canvas CreateMainCanvas()
    {
        GameObject existingRoot = GameObject.Find("Generated_UI_Root");
        if (existingRoot != null) DestroyImmediate(existingRoot);

        var canvasObj = new GameObject("Generated_UI_Root");
        canvasObj.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    GameObject CreatePanel(string name, Transform parent)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        SetAnchorsAndOffsets(panel, Vector2.zero, Vector2.one);
        panel.GetComponent<Image>().color = panelBackgroundColor;
        return panel;
    }
    #endregion

    #region Prefab Definitions
    GameObject CreateItemViewPrefab()
    {
        var prefab = new GameObject("InventoryItemViewPrefab", typeof(RectTransform));
        prefab.SetActive(false);
        prefab.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 60);
        CreateItemView(prefab.transform);
        return prefab;
    }

    GameObject CreateItemSlotPrefab(GameObject itemViewPrefab)
    {
        var prefab = CreateGeneSlot("ItemSlotPrefab", GeneCategory.Passive, itemViewPrefab); // Category is a placeholder
        prefab.GetComponent<GeneSlotUI>().acceptedCategory = 0; // Will be overwritten by controller/sequencer
        return prefab;
    }

    GameObject CreateSequenceRowPrefab(GameObject itemViewPrefab)
    {
        var prefab = new GameObject("SequenceRowPrefab", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(SequenceRowUI));
        prefab.SetActive(false);
        prefab.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 70);
        var layout = prefab.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 15;
        layout.childAlignment = TextAnchor.MiddleCenter;

        SequenceRowUI rowUI = prefab.GetComponent<SequenceRowUI>();

        rowUI.modifierSlot = CreateGeneSlot("ModifierSlot", GeneCategory.Modifier, itemViewPrefab, prefab.transform).GetComponent<GeneSlotUI>();
        rowUI.activeSlot = CreateGeneSlot("ActiveSlot", GeneCategory.Active, itemViewPrefab, prefab.transform).GetComponent<GeneSlotUI>();
        rowUI.payloadSlot = CreateGeneSlot("PayloadSlot", GeneCategory.Payload, itemViewPrefab, prefab.transform).GetComponent<GeneSlotUI>();

        return prefab;
    }
    #endregion

    #region UI Module Creation
    InventoryGridController CreateInventoryGrid(Transform parent, GameObject itemSlotPrefab)
    {
        var inventoryGridObj = new GameObject("InventoryGrid", typeof(Image), typeof(InventoryGridController));
        inventoryGridObj.transform.SetParent(parent, false);
        SetAnchorsAndOffsets(inventoryGridObj, new Vector2(0.05f, 0.1f), new Vector2(0.35f, 0.85f));
        inventoryGridObj.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        var gridTitle = CreateText("Title", inventoryGridObj.transform, "INVENTORY", 20, TextAlignmentOptions.Center);
        SetAnchorsAndOffsets(gridTitle, new Vector2(0, 0.9f), new Vector2(1, 1f));

        InventoryGridController gridController = inventoryGridObj.GetComponent<InventoryGridController>();

        var cellContainer = new GameObject("CellContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        cellContainer.transform.SetParent(inventoryGridObj.transform, false);
        SetAnchorsAndOffsets(cellContainer, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.9f));

        var gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        gridLayout.padding = new RectOffset(10, 10, 10, 10);
        gridLayout.cellSize = new Vector2(64, 64);
        gridLayout.spacing = new Vector2(10, 10);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 4;

        SetPrivateField(gridController, "cellContainer", cellContainer.transform);
        SetPrivateField(gridController, "itemSlotPrefab", itemSlotPrefab);
        return gridController;
    }

    void CreateInventoryBar(Transform parent, GameObject itemViewPrefab, InventoryGridController gridController)
    {
        var inventoryBarObj = new GameObject("InventoryBar", typeof(Image), typeof(InventoryBarController));
        inventoryBarObj.transform.SetParent(parent, false);
        SetAnchorsAndOffsets(inventoryBarObj, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.12f));
        inventoryBarObj.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        InventoryBarController barController = inventoryBarObj.GetComponent<InventoryBarController>();

        var barCellContainer = new GameObject("CellContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        barCellContainer.transform.SetParent(inventoryBarObj.transform, false);
        SetAnchorsAndOffsets(barCellContainer, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f));

        var barGridLayout = barCellContainer.GetComponent<GridLayoutGroup>();
        barGridLayout.cellSize = new Vector2(60, 60);
        barGridLayout.spacing = new Vector2(5, 0);
        barGridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        barGridLayout.constraintCount = 1;

        var selectionHighlight = CreateSelectionHighlight(inventoryBarObj.transform);

        SetPrivateField(barController, "cellContainer", barCellContainer.transform);
        SetPrivateField(barController, "selectionHighlight", selectionHighlight);
        SetPrivateField(barController, "inventoryItemViewPrefab", itemViewPrefab);
        SetPrivateField(barController, "inventoryGridController", gridController);
    }

        // Find the CreateGeneSequenceUI method and replace it with this version.
    GeneSequenceUI CreateGeneSequenceUI(Transform parent, GameObject passiveSlotPrefab, GameObject sequenceRowPrefab)
    {
        var geneSequenceUIObj = new GameObject("GeneSequenceUI", typeof(Image), typeof(GeneSequenceUI));
        geneSequenceUIObj.transform.SetParent(parent, false);
        SetAnchorsAndOffsets(geneSequenceUIObj, new Vector2(0.4f, 0.1f), new Vector2(0.95f, 0.85f));
        geneSequenceUIObj.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 0.9f);
        var title = CreateText("Title", geneSequenceUIObj.transform, "SEED EDITOR", 20, TextAlignmentOptions.Center);
        SetAnchorsAndOffsets(title, new Vector2(0, 0.95f), new Vector2(1, 1f));

        GeneSequenceUI sequenceUI = geneSequenceUIObj.GetComponent<GeneSequenceUI>();

        // NEW: Create the dedicated Seed Edit Slot
        var itemViewPrefab = CreateItemViewPrefab(); // We need an ItemView for the slot
        itemViewPrefab.transform.SetParent(geneSequenceUIObj.transform); // Temporarily parent to create slot
        var seedEditSlot = CreateGeneSlot("SeedEditSlot", GeneCategory.Passive, itemViewPrefab, geneSequenceUIObj.transform); // Category doesn't matter, will be overridden
        SetAnchorsAndOffsets(seedEditSlot, new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), new Vector2(-32, -32), new Vector2(32, 32)); // Center it
        var seedSlotUI = seedEditSlot.GetComponent<GeneSlotUI>();
        seedSlotUI.isDraggable = false; // You can't drag the seed away once it's being edited
        DestroyImmediate(itemViewPrefab); // Clean up temp object

        var passiveContainer = new GameObject("PassiveGenesContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        passiveContainer.transform.SetParent(geneSequenceUIObj.transform, false);
        passiveContainer.GetComponent<HorizontalLayoutGroup>().spacing = 10;
        SetAnchorsAndOffsets(passiveContainer, new Vector2(0.05f, 0.7f), new Vector2(0.95f, 0.8f));

        var activeContainer = new GameObject("ActiveSequenceContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        activeContainer.transform.SetParent(geneSequenceUIObj.transform, false);
        var activeLayout = activeContainer.GetComponent<VerticalLayoutGroup>();
        activeLayout.spacing = 10;
        activeLayout.childControlWidth = true;
        activeLayout.childForceExpandWidth = true;
        SetAnchorsAndOffsets(activeContainer, new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.65f));

        CreateSequenceInfoDisplay(geneSequenceUIObj.transform, sequenceUI);
        
        // Link the new slot to the GeneSequenceUI script
        SetPrivateField(sequenceUI, "seedEditSlot", seedSlotUI);
        sequenceUI.passiveGenesContainer = passiveContainer.transform;
        sequenceUI.activeSequenceContainer = activeContainer.transform;
        sequenceUI.sequenceRowPrefab = sequenceRowPrefab;
        sequenceUI.passiveSlotPrefab = passiveSlotPrefab;
        return sequenceUI;
    }

    void CreateSequenceInfoDisplay(Transform parent, GeneSequenceUI sequenceUI)
    {
        // FIX: The InfoPanel now correctly gets a RectTransform.
        var infoPanel = new GameObject("InfoPanel", typeof(RectTransform));
        infoPanel.transform.SetParent(parent, false);
        SetAnchorsAndOffsets(infoPanel, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.2f));

        // FIX: Replaced '⚡' with 'E' to prevent font warnings.
        var energyCostObj = CreateText("EnergyCostText", infoPanel.transform, "Cost: 0 E/cycle", 16, TextAlignmentOptions.Left);
        SetAnchorsAndOffsets(energyCostObj, new Vector2(0, 0.5f), new Vector2(0.4f, 1f));
        
        var rechargeObj = CreateText("RechargeTimeText", infoPanel.transform, "Recharge: 3 ticks", 16, TextAlignmentOptions.Right);
        SetAnchorsAndOffsets(rechargeObj, new Vector2(0.6f, 0.5f), new Vector2(1f, 1f));
        
        var currentEnergyObj = CreateText("CurrentEnergyText", infoPanel.transform, "Energy: --/100", 14, TextAlignmentOptions.Center);
        SetAnchorsAndOffsets(currentEnergyObj, new Vector2(0, 0f), new Vector2(1f, 0.5f));
        
        sequenceUI.energyCostText = energyCostObj.GetComponent<TextMeshProUGUI>();
        sequenceUI.currentEnergyText = currentEnergyObj.GetComponent<TextMeshProUGUI>();
        sequenceUI.rechargeTimeText = rechargeObj.GetComponent<TextMeshProUGUI>();
        sequenceUI.rechargeProgress = null; 
        sequenceUI.validationMessage = null; 
    }

    void CreateControlButtons(Transform planningParent, Transform growthParent)
    {
        CreateButton("EndPlanningButton", planningParent, "Start Growth", new Vector2(0.5f, 0.02f), new Vector2(0.8f, 0.08f));
        CreateButton("ReturnToPlanningButton", growthParent, "End Day", new Vector2(0.8f, 0.9f), new Vector2(0.95f, 0.95f));
    }

    GameObject CreateDebugUI(Transform parent)
    {
        GameObject wegoPanel = CreatePanel("WegoControlPanel", parent);
        wegoPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        SetAnchorsAndOffsets(wegoPanel, new Vector2(0.01f, 0.01f), new Vector2(0.2f, 0.15f));

        var phaseText = CreateText("CurrentPhaseText", wegoPanel.transform, "Phase: Planning", 14, TextAlignmentOptions.Left);
        SetAnchorsAndOffsets(phaseText, new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.9f));
        
        var tickText = CreateText("TickCounterText", wegoPanel.transform, "Tick: 0", 14, TextAlignmentOptions.Left);
        SetAnchorsAndOffsets(tickText, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.6f));
        
        var phaseProgressText = CreateText("PhaseProgressText", wegoPanel.transform, "Phase Ticks: 0", 12, TextAlignmentOptions.Left);
        SetAnchorsAndOffsets(phaseProgressText, new Vector2(0.1f, 0.0f), new Vector2(0.9f, 0.3f));
        
        return wegoPanel;
    }
    #endregion

    #region Step 5: Linking
    void LinkToUIManager(GameObject planningPanel, GameObject growthThreatPanel, GeneSequenceUI geneSequenceUI, GameObject wegoPanel)
    {
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            uiManager = new GameObject("UIManager").AddComponent<UIManager>();
        }
        
        SetPrivateField(uiManager, "planningPanel", planningPanel);
        SetPrivateField(uiManager, "growthAndThreatPanel", growthThreatPanel);
        SetPrivateField(uiManager, "geneSequenceUIPanel", geneSequenceUI.gameObject);

        var endPlanningBtn = planningPanel.transform.Find("EndPlanningButton")?.GetComponent<Button>();
        var newPlanningBtn = growthThreatPanel.transform.Find("ReturnToPlanningButton")?.GetComponent<Button>();
        
        // FIX: Correctly link the buttons that are actually created.
        SetPrivateField(uiManager, "endPlanningPhaseButton", endPlanningBtn);
        SetPrivateField(uiManager, "startNewPlanningPhaseButton", newPlanningBtn);
            
        var currentPhaseText = wegoPanel.transform.Find("CurrentPhaseText")?.GetComponent<TextMeshProUGUI>();
        var tickCounterText = wegoPanel.transform.Find("TickCounterText")?.GetComponent<TextMeshProUGUI>();
        var phaseProgressText = wegoPanel.transform.Find("PhaseProgressText")?.GetComponent<TextMeshProUGUI>();

        SetPrivateField(uiManager, "wegoControlPanel", wegoPanel);
        SetPrivateField(uiManager, "currentPhaseText", currentPhaseText);
        SetPrivateField(uiManager, "tickCounterText", tickCounterText);
        SetPrivateField(uiManager, "phaseProgressText", phaseProgressText);
    }
    #endregion

    #region Generic Helper Methods
    GameObject CreateText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
    {
        var textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);
        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        if (defaultFontAsset != null) tmp.font = defaultFontAsset;
        return textObj;
    }

    GameObject CreateButton(string name, Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax)
    {
        var buttonObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);
        SetAnchorsAndOffsets(buttonObj, anchorMin, anchorMax);
        Image bg = buttonObj.GetComponent<Image>();
        bg.color = new Color(0.2f, 0.3f, 0.4f, 1f);
        Button button = buttonObj.GetComponent<Button>();
        button.targetGraphic = bg;
        GameObject textObj = CreateText("Text", buttonObj.transform, text, 16, TextAlignmentOptions.Center);
        SetAnchorsAndOffsets(textObj, Vector2.zero, Vector2.one, new Vector2(10, 5), new Vector2(-10, -5));
        return buttonObj;
    }

    GameObject CreateItemView(Transform parent)
    {
        var itemView = new GameObject("ItemView", typeof(RectTransform), typeof(Image), typeof(ItemView), typeof(TooltipTrigger));
        itemView.transform.SetParent(parent, false);
        SetAnchorsAndOffsets(itemView, Vector2.zero, Vector2.one);
        
        Image bgImage = itemView.GetComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);
        
        var thumbnail = new GameObject("ThumbnailImage", typeof(RectTransform), typeof(Image));
        thumbnail.transform.SetParent(itemView.transform, false);
        SetAnchorsAndOffsets(thumbnail, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
        thumbnail.GetComponent<Image>().preserveAspect = true;
        
        ItemView itemViewComponent = itemView.GetComponent<ItemView>();
        SetPrivateField(itemViewComponent, "thumbnailImage", thumbnail.GetComponent<Image>());
        SetPrivateField(itemViewComponent, "backgroundImage", bgImage);
        
        return itemView;
    }
    
    GameObject CreateGeneSlot(string name, GeneCategory category, GameObject itemViewPrefab, Transform parent = null)
    {
        var slot = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(GeneSlotUI));
        if (parent != null) slot.transform.SetParent(parent, false);
        
        slot.GetComponent<RectTransform>().sizeDelta = new Vector2(64, 64);
        slot.GetComponent<Image>().color = GetSlotColorForCategory(category);
        
        GeneSlotUI slotUI = slot.GetComponent<GeneSlotUI>();
        slotUI.acceptedCategory = category;

        var emptyIndicator = CreateText("EmptyIndicator", slot.transform, GetEmptyTextForCategory(category), 12, TextAlignmentOptions.Center);
        SetAnchorsAndOffsets(emptyIndicator, Vector2.zero, Vector2.one);
        emptyIndicator.GetComponent<TextMeshProUGUI>().color = new Color(0.6f, 0.6f, 0.6f, 0.5f);

        var itemViewInstance = Instantiate(itemViewPrefab);
        itemViewInstance.transform.SetParent(slot.transform, false);
        itemViewInstance.name = "ItemView";
        itemViewInstance.SetActive(false);

        SetPrivateField(slotUI, "slotBackground", slot.GetComponent<Image>());
        SetPrivateField(slotUI, "emptyIndicator", emptyIndicator);
        SetPrivateField(slotUI, "itemView", itemViewInstance.GetComponent<ItemView>());
        
        return slot;
    }
    
    GameObject CreateSelectionHighlight(Transform parent)
    {
        var highlight = new GameObject("SelectionHighlight", typeof(RectTransform), typeof(Image));
        highlight.transform.SetParent(parent, false);
        highlight.GetComponent<RectTransform>().sizeDelta = new Vector2(68, 68);
        Image highlightImage = highlight.GetComponent<Image>();
        highlightImage.color = new Color(1f, 0.9f, 0.3f, 0.5f);
        highlightImage.raycastTarget = false;
        highlight.SetActive(false);
        return highlight;
    }

    void SetAnchorsAndOffsets(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin = default, Vector2 offsetMax = default)
    {
        if (obj.GetComponent<RectTransform>() is RectTransform rect)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }

    void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) field.SetValue(obj, value);
        else Debug.LogWarning($"[CompleteUISetup] Could not find private field '{fieldName}' on object '{obj}'.");
    }
    
    Color GetSlotColorForCategory(GeneCategory category)
    {
        switch (category)
        {
            case GeneCategory.Active:   return new Color(0.4f, 0.2f, 0.2f, 1f);
            case GeneCategory.Passive:  return new Color(0.2f, 0.2f, 0.4f, 1f);
            case GeneCategory.Modifier: return new Color(0.4f, 0.4f, 0.2f, 1f);
            case GeneCategory.Payload:  return new Color(0.4f, 0.2f, 0.4f, 1f);
            default:                    return slotNormalColor;
        }
    }
    
    string GetEmptyTextForCategory(GeneCategory category)
    {
        switch (category)
        {
            case GeneCategory.Active:   return "ACTIVE";
            case GeneCategory.Passive:  return "PASSIVE";
            case GeneCategory.Modifier: return "MOD";
            case GeneCategory.Payload:  return "LOAD";
            default:                    return "+";
        }
    }
    #endregion
}