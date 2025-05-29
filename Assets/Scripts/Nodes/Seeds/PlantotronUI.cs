// FILE: Assets/Scripts/Nodes/Seeds/PlantotronUI.cs (UPDATED with Drop Zones)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections;

public class PlantotronUI : MonoBehaviour
{
    [Header("Main UI Panel")]
    public GameObject mainPanel;
    
    [Header("4-Panel Layout")]
    public Transform detailsPanel;
    public Transform genesPanel;
    public Transform seedsPanel;
    public Transform sequencePanel;
    
    [Header("Scrollview Containers")]
    public Transform geneListContainer;
    public Transform seedListContainer;
    public Transform sequenceContainer;
    
    [Header("Prefabs")]
    public GameObject geneItemPrefab;
    public GameObject seedItemPrefab;
    public GameObject sequenceItemPrefab;
    public GameObject dropZonePrefab; // <<< NEW: Drop zone prefab
    
    [Header("Details Panel Components")]
    public TMP_Text detailsTitleText;
    public TMP_Text detailsDescriptionText;
    public ScrollRect detailsScrollRect;
    
    [Header("Control Buttons")]
    public Button closeButton;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Current state
    private SeedInstance currentSelectedSeed;
    private PlayerGeneticsInventory inventory;
    
    // UI item lists
    private List<GameObject> currentGeneItems = new List<GameObject>();
    private List<GameObject> currentSeedItems = new List<GameObject>();
    private List<GameObject> currentSequenceItems = new List<GameObject>();
    private List<PlantotronSequenceDropZone> currentDropZones = new List<PlantotronSequenceDropZone>(); // <<< NEW
    
    // UI state tracking
    private bool isUIOpen = false;
    
    void Awake()
    {
        // Setup close button
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseUI);
        
        // Validate required components
        ValidateComponents();
    }
    
    void Start()
    {
        // Get inventory reference
        inventory = PlayerGeneticsInventory.Instance;
        if (inventory == null)
        {
            Debug.LogError("[PlantotronUI] PlayerGeneticsInventory not found!", this);
            return;
        }
    
        // DEBUG: Check inventory contents
        Debug.Log($"[PlantotronUI] Inventory found with {inventory.AvailableSeeds?.Count ?? 0} seeds");
        if (inventory.AvailableSeeds != null)
        {
            foreach (var seed in inventory.AvailableSeeds)
            {
                Debug.Log($"[PlantotronUI] Seed in inventory: {seed?.seedName ?? "null"}, genes: {seed?.currentGenes?.Count ?? 0}");
            }
        }
    
        // Subscribe to inventory events
        inventory.OnInventoryChanged += RefreshAllPanels;
        inventory.OnGeneCountChanged += OnGeneCountChanged;
        
        // CRITICAL FIX: Ensure UI starts closed properly
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
            isUIOpen = false;
            if (showDebugLogs)
                Debug.Log("[PlantotronUI] UI properly initialized as closed");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshAllPanels;
            inventory.OnGeneCountChanged -= OnGeneCountChanged;
        }
        
        // Clean up button listeners
        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();
    }
    
    private void ValidateComponents()
    {
        bool isValid = true;
        
        if (geneItemPrefab == null) { Debug.LogError("[PlantotronUI] Gene Item Prefab not assigned!", this); isValid = false; }
        if (seedItemPrefab == null) { Debug.LogError("[PlantotronUI] Seed Item Prefab not assigned!", this); isValid = false; }
        if (sequenceItemPrefab == null) { Debug.LogError("[PlantotronUI] Sequence Item Prefab not assigned!", this); isValid = false; }
        if (dropZonePrefab == null) { Debug.LogError("[PlantotronUI] Drop Zone Prefab not assigned!", this); isValid = false; } // <<< NEW
        if (geneListContainer == null) { Debug.LogError("[PlantotronUI] Gene List Container not assigned!", this); isValid = false; }
        if (seedListContainer == null) { Debug.LogError("[PlantotronUI] Seed List Container not assigned!", this); isValid = false; }
        if (sequenceContainer == null) { Debug.LogError("[PlantotronUI] Sequence Container not assigned!", this); isValid = false; }
        if (mainPanel == null) { Debug.LogError("[PlantotronUI] Main Panel not assigned!", this); isValid = false; }
        
        if (!isValid)
        {
            Debug.LogError("[PlantotronUI] Missing required components! UI will not function properly.", this);
            enabled = false;
        }
    }
    
    // --- Main UI Control ---
    
    public void OpenUI()
    {
        if (showDebugLogs)
            Debug.Log("[PlantotronUI] OpenUI called");
        
        if (isUIOpen)
        {
            if (showDebugLogs)
                Debug.Log("[PlantotronUI] UI already open, skipping");
            return;
        }
        
        StartCoroutine(OpenUICoroutine());
    }
    
    private IEnumerator OpenUICoroutine()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
            isUIOpen = true;
        
            if (showDebugLogs)
                Debug.Log("[PlantotronUI] Main panel activated");
        
            yield return null;
        
            // ENSURE ALL CONTAINERS ARE ENABLED
            EnsureContainersEnabled();
        
            Canvas canvas = mainPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = false;
                canvas.enabled = true;
            }
        
            yield return null;
        
            RefreshAllPanels();
            ClearDetails();
        
            if (geneListContainer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(geneListContainer.GetComponent<RectTransform>());
            if (seedListContainer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(seedListContainer.GetComponent<RectTransform>());
            if (sequenceContainer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(sequenceContainer.GetComponent<RectTransform>());
        
            if (showDebugLogs)
                Debug.Log("[PlantotronUI] UI fully opened and refreshed");
        }
    }
    
    public void CloseUI()
    {
        if (showDebugLogs)
            Debug.Log("[PlantotronUI] CloseUI called");
        
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
            isUIOpen = false;
        }
            
        currentSelectedSeed = null;
        
        if (showDebugLogs)
            Debug.Log("[PlantotronUI] UI Closed");
    }
    
    // --- Panel Refresh Methods ---
    
    private void RefreshAllPanels()
    {
        if (!isUIOpen)
        {
            if (showDebugLogs)
                Debug.Log("[PlantotronUI] RefreshAllPanels called but UI not open, skipping");
            return;
        }
        
        if (showDebugLogs)
            Debug.Log("[PlantotronUI] Refreshing all panels");
        
        RefreshGenesPanel();
        RefreshSeedsPanel();
        RefreshSequencePanel();
    }
    
    private void RefreshGenesPanel()
    {
        if (inventory == null || geneListContainer == null) return;
        
        ClearContainer(geneListContainer, currentGeneItems);
        
        foreach (var geneCount in inventory.AvailableGenes)
        {
            if (geneCount.gene != null && geneCount.count > 0)
            {
                GameObject geneItem = CreateGeneItem(geneCount);
                if (geneItem != null)
                    currentGeneItems.Add(geneItem);
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"[PlantotronUI] Refreshed genes panel: {currentGeneItems.Count} gene types");
    }
    
    private void RefreshSeedsPanel()
    {
        if (inventory == null || seedListContainer == null) return;
        
        ClearContainer(seedListContainer, currentSeedItems);
        
        foreach (var seed in inventory.AvailableSeeds)
        {
            if (seed != null)
            {
                GameObject seedItem = CreateSeedItem(seed);
                if (seedItem != null)
                    currentSeedItems.Add(seedItem);
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"[PlantotronUI] Refreshed seeds panel: {currentSeedItems.Count} seeds");
    }
    
    // Add this new method to ensure containers are enabled
    private void EnsureContainersEnabled()
    {
        // Enable main containers
        if (geneListContainer != null && !geneListContainer.gameObject.activeSelf)
        {
            geneListContainer.gameObject.SetActive(true);
            if (showDebugLogs)
                Debug.Log("[PlantotronUI] Enabled geneListContainer");
        }
    
        if (seedListContainer != null && !seedListContainer.gameObject.activeSelf)
        {
            seedListContainer.gameObject.SetActive(true);
            if (showDebugLogs)
                Debug.Log("[PlantotronUI] Enabled seedListContainer");
        }
    
        if (sequenceContainer != null && !sequenceContainer.gameObject.activeSelf)
        {
            sequenceContainer.gameObject.SetActive(true);
            if (showDebugLogs)
                Debug.Log("[PlantotronUI] Enabled sequenceContainer");
        }
    
        // Also enable parent panels if they exist
        if (genesPanel != null && !genesPanel.gameObject.activeSelf)
        {
            genesPanel.gameObject.SetActive(true);
            if (showDebugLogs)
                Debug.Log("[PlantotronUI] Enabled genesPanel");
        }
    
        if (seedsPanel != null && !seedsPanel.gameObject.activeSelf)
        {
            seedsPanel.gameObject.SetActive(true);
            if (showDebugLogs)
                Debug.Log("[PlantotronUI] Enabled seedsPanel");
        }
    
        if (sequencePanel != null && !sequencePanel.gameObject.activeSelf)
        {
            sequencePanel.gameObject.SetActive(true);
            if (showDebugLogs)
                Debug.Log("[PlantotronUI] Enabled sequencePanel");
        }
    }
    
    private void RefreshSequencePanel()
{
    Debug.Log("[PlantotronUI] RefreshSequencePanel called");
    
    if (sequenceContainer == null)
    {
        Debug.LogError("[PlantotronUI] sequenceContainer is null!");
        return;
    }
    
    // ENSURE SEQUENCE CONTAINER IS ENABLED
    if (!sequenceContainer.gameObject.activeSelf)
    {
        sequenceContainer.gameObject.SetActive(true);
        Debug.Log("[PlantotronUI] Enabled sequenceContainer in RefreshSequencePanel");
    }
    
    Debug.Log("[PlantotronUI] Clearing containers and drop zones...");
    
    // Clear existing items and drop zones
    ClearContainer(sequenceContainer, currentSequenceItems);
    ClearDropZones();
    
    if (currentSelectedSeed == null)
    {
        Debug.Log("[PlantotronUI] No seed selected, creating minimal drop zones");
        CreateDropZones(0);
        return;
    }
    
    if (currentSelectedSeed.currentGenes == null)
    {
        Debug.Log("[PlantotronUI] Selected seed has null genes list, creating minimal drop zones");
        CreateDropZones(0);
        return;
    }
    
    int geneCount = currentSelectedSeed.currentGenes.Count;
    Debug.Log($"[PlantotronUI] Selected seed has {geneCount} genes");
    
    // Create gene items ONLY (no drop zones yet - they're created on demand during drag)
    for (int i = 0; i < geneCount; i++)
    {
        if (currentSelectedSeed.currentGenes[i] != null)
        {
            Debug.Log($"[PlantotronUI] Creating sequence item {i}: {currentSelectedSeed.currentGenes[i].displayName}");
            
            GameObject sequenceItem = CreateSequenceItem(currentSelectedSeed.currentGenes[i], i);
            if (sequenceItem != null)
            {
                currentSequenceItems.Add(sequenceItem);
                Debug.Log($"[PlantotronUI] Created sequence item {i}");
            }
            else
            {
                Debug.LogError($"[PlantotronUI] Failed to create sequence item {i}");
            }
        }
        else
        {
            Debug.LogWarning($"[PlantotronUI] Gene at index {i} is null");
        }
    }
    
    Debug.Log($"[PlantotronUI] RefreshSequencePanel complete: {currentSequenceItems.Count} genes");
    
    // Force layout rebuild
    LayoutRebuilder.ForceRebuildLayoutImmediate(sequenceContainer.GetComponent<RectTransform>());
}

    
    // <<< NEW: Drop Zone Management >>>
    private void CreateDropZones(int geneCount)
    {
        Debug.Log($"[PlantotronUI] CreateDropZones called with geneCount: {geneCount}");
    
        if (dropZonePrefab == null)
        {
            Debug.LogError("[PlantotronUI] dropZonePrefab is null!");
            return;
        }
    
        if (sequenceContainer == null)
        {
            Debug.LogError("[PlantotronUI] sequenceContainer is null!");
            return;
        }
    
        // Create one more drop zone than genes (for insertion at end)
        int dropZoneCount = geneCount + 1;
        Debug.Log($"[PlantotronUI] Creating {dropZoneCount} drop zones");
    
        for (int i = 0; i < dropZoneCount; i++)
        {
            Debug.Log($"[PlantotronUI] Creating drop zone {i}");
        
            GameObject dropZoneGO = Instantiate(dropZonePrefab, sequenceContainer);
            if (dropZoneGO == null)
            {
                Debug.LogError($"[PlantotronUI] Failed to instantiate drop zone {i}");
                continue;
            }
        
            PlantotronSequenceDropZone dropZone = dropZoneGO.GetComponent<PlantotronSequenceDropZone>();
        
            if (dropZone != null)
            {
                dropZone.Initialize(this, i);
                currentDropZones.Add(dropZone);
            
                // Position at even indices (0, 2, 4, etc.)
                dropZoneGO.transform.SetSiblingIndex(i * 2);
            
                Debug.Log($"[PlantotronUI] Successfully created drop zone {i} at sibling index {i * 2}");
            }
            else
            {
                Debug.LogError("[PlantotronUI] Drop Zone Prefab missing PlantotronSequenceDropZone component!", dropZonePrefab);
                Destroy(dropZoneGO);
            }
        }
    
        Debug.Log($"[PlantotronUI] CreateDropZones complete: {currentDropZones.Count} zones created");
    }
    
    private void CreateDropZonesForDrag()
    {
        if (dropZonePrefab == null || sequenceContainer == null) return;
    
        // Clear any existing drop zones first
        ClearDropZones();
    
        int geneCount = currentSelectedSeed?.currentGenes?.Count ?? 0;
        int dropZoneCount = geneCount + 1; // One more than genes for insertion at end
    
        Debug.Log($"[PlantotronUI] Creating {dropZoneCount} drop zones for drag operation");
    
        for (int i = 0; i < dropZoneCount; i++)
        {
            GameObject dropZoneGO = Instantiate(dropZonePrefab, sequenceContainer);
            PlantotronSequenceDropZone dropZone = dropZoneGO.GetComponent<PlantotronSequenceDropZone>();
        
            if (dropZone != null)
            {
                dropZone.Initialize(this, i);
                currentDropZones.Add(dropZone);
            
                // Position drop zones between gene items
                // Index 0: before first gene
                // Index 1: between gene 0 and 1
                // Index 2: between gene 1 and 2, etc.
                int siblingIndex = i * 2; // 0, 2, 4, 6...
                dropZoneGO.transform.SetSiblingIndex(siblingIndex);
            
                // Shift existing gene items to odd positions
                if (i < currentSequenceItems.Count && currentSequenceItems[i] != null)
                {
                    currentSequenceItems[i].transform.SetSiblingIndex((i * 2) + 1);
                }
            
                Debug.Log($"[PlantotronUI] Created drop zone {i} at sibling index {siblingIndex}");
            }
            else
            {
                Debug.LogError("[PlantotronUI] Drop Zone Prefab missing PlantotronSequenceDropZone component!", dropZonePrefab);
                Destroy(dropZoneGO);
            }
        }
    }
    
    private void ClearDropZones()
    {
        foreach (var dropZone in currentDropZones)
        {
            if (dropZone != null && dropZone.gameObject != null)
                DestroyImmediate(dropZone.gameObject);
        }
        currentDropZones.Clear();
    }
    // <<< END Drop Zone Management >>>
    
    // --- Item Creation Methods ---
    
    private GameObject CreateGeneItem(PlayerGeneticsInventory.GeneCount geneCount)
    {
        if (geneItemPrefab == null || geneListContainer == null || geneCount.gene == null)
            return null;
            
        GameObject item = Instantiate(geneItemPrefab, geneListContainer);
        
        PlantotronGeneItem geneItemComponent = item.GetComponent<PlantotronGeneItem>();
        if (geneItemComponent == null)
            geneItemComponent = item.AddComponent<PlantotronGeneItem>();
            
        geneItemComponent.Initialize(geneCount, this);
        
        return item;
    }
    
    private GameObject CreateSeedItem(SeedInstance seed)
    {
        if (seedItemPrefab == null || seedListContainer == null || seed == null)
            return null;
            
        GameObject item = Instantiate(seedItemPrefab, seedListContainer);
        
        PlantotronSeedItem seedItemComponent = item.GetComponent<PlantotronSeedItem>();
        if (seedItemComponent == null)
            seedItemComponent = item.AddComponent<PlantotronSeedItem>();
            
        seedItemComponent.Initialize(seed, this);
        
        return item;
    }
    
    private GameObject CreateSequenceItem(NodeDefinition gene, int index)
    {
        if (sequenceItemPrefab == null || sequenceContainer == null || gene == null)
            return null;
            
        GameObject item = Instantiate(sequenceItemPrefab, sequenceContainer);
        
        PlantotronSequenceItem sequenceItemComponent = item.GetComponent<PlantotronSequenceItem>();
        if (sequenceItemComponent == null)
            sequenceItemComponent = item.AddComponent<PlantotronSequenceItem>();
            
        sequenceItemComponent.Initialize(gene, index, this);
        
        return item;
    }
    
    // --- Details Panel ---
    
    public void ShowGeneDetails(NodeDefinition gene)
    {
        if (gene == null || detailsTitleText == null || detailsDescriptionText == null)
            return;
            
        detailsTitleText.text = gene.displayName;
        
        string details = !string.IsNullOrEmpty(gene.description) ? gene.description : "No description available.";
        
        if (gene.effects != null && gene.effects.Count > 0)
        {
            details += "\n\n<b>Effects:</b>";
            foreach (var effect in gene.effects)
            {
                if (effect != null)
                {
                    details += $"\n• {effect.effectType}: {effect.primaryValue}";
                    if (effect.secondaryValue != 0)
                        details += $" / {effect.secondaryValue}";
                    if (effect.isPassive)
                        details += " (Passive)";
                }
            }
        }
        
        int count = inventory?.GetGeneCount(gene) ?? 0;
        details += $"\n\n<b>Available:</b> {count}";
        
        detailsDescriptionText.text = details;
        
        if (detailsScrollRect != null)
            detailsScrollRect.verticalNormalizedPosition = 1f;
    }
    
    public void ShowSeedDetails(SeedInstance seed)
    {
        if (seed == null || detailsTitleText == null || detailsDescriptionText == null)
            return;
            
        detailsTitleText.text = seed.seedName;
        
        string details = "";
        
        if (seed.baseSeedDefinition != null && !string.IsNullOrEmpty(seed.baseSeedDefinition.description))
        {
            details += seed.baseSeedDefinition.description + "\n\n";
        }
        
        details += $"<b>Status:</b> {(seed.isModified ? "Modified" : "Vanilla")}\n";
        details += $"<b>Genes:</b> {(seed.currentGenes?.Count ?? 0)}\n";
        details += $"<b>Plantable:</b> {(seed.IsValidForPlanting() ? "Yes" : "No")}\n\n";
        
        if (seed.currentGenes != null && seed.currentGenes.Count > 0)
        {
            details += "<b>Genetic Sequence:</b>\n";
            for (int i = 0; i < seed.currentGenes.Count; i++)
            {
                if (seed.currentGenes[i] != null)
                {
                    details += $"{i + 1}. {seed.currentGenes[i].displayName}\n";
                }
            }
        }
        else
        {
            details += "<b>No genes present</b>";
        }
        
        detailsDescriptionText.text = details;
        
        if (detailsScrollRect != null)
            detailsScrollRect.verticalNormalizedPosition = 1f;
    }
    
    public void ClearDetails()
    {
        if (detailsTitleText != null)
            detailsTitleText.text = "Details";
        if (detailsDescriptionText != null)
            detailsDescriptionText.text = "Click on a gene or seed to see details.";
    }
    
    // --- Event Handlers ---
    
    private void OnGeneCountChanged(NodeDefinition gene, int newCount)
    {
        foreach (GameObject item in currentGeneItems)
        {
            if (item != null)
            {
                PlantotronGeneItem geneItem = item.GetComponent<PlantotronGeneItem>();
                if (geneItem != null)
                {
                    geneItem.UpdateDisplay();
                }
            }
        }
        
        if (newCount <= 0)
        {
            RefreshGenesPanel();
        }
    }
    
    // --- Public Methods for Item Interactions ---
    
    public void OnSeedSelected(SeedInstance seed)
    {
        Debug.Log($"[PlantotronUI] OnSeedSelected called with seed: {seed?.seedName ?? "null"}");
    
        if (seed == null)
        {
            Debug.LogError("[PlantotronUI] OnSeedSelected: seed is null!");
            return;
        }
    
        Debug.Log($"[PlantotronUI] Selecting seed: {seed.seedName}, genes count: {seed.currentGenes?.Count ?? 0}");
    
        currentSelectedSeed = seed;
    
        // ENSURE CONTAINERS ARE ENABLED BEFORE REFRESHING
        EnsureContainersEnabled();
    
        Debug.Log("[PlantotronUI] About to refresh sequence panel...");
        RefreshSequencePanel();
    
        Debug.Log("[PlantotronUI] About to show seed details...");
        ShowSeedDetails(seed);
    
        Debug.Log($"[PlantotronUI] Seed selection complete for: {seed.seedName}");
    }
    
    public void OnGeneClicked(NodeDefinition gene)
    {
        ShowGeneDetails(gene);
    }
    
    
    
    // --- UPDATED: Drag & Drop Methods ---
    
    public bool TryAddGeneToSequence(NodeDefinition gene, int insertIndex = -1)
    {
        if (currentSelectedSeed == null || gene == null)
        {
            if (showDebugLogs)
                Debug.Log("[PlantotronUI] Cannot add gene - no seed selected or gene is null");
            return false;
        }
        
        if (!inventory.TryConsumeGene(gene))
        {
            if (showDebugLogs)
                Debug.Log($"[PlantotronUI] Cannot add gene {gene.displayName} - not available in inventory");
            return false;
        }
        
        // Add to seed's gene sequence at specific index
        if (insertIndex >= 0 && insertIndex <= currentSelectedSeed.currentGenes.Count)
        {
            currentSelectedSeed.currentGenes.Insert(insertIndex, gene);
            if (showDebugLogs)
                Debug.Log($"[PlantotronUI] Inserted gene {gene.displayName} at index {insertIndex}");
        }
        else
        {
            currentSelectedSeed.currentGenes.Add(gene);
            if (showDebugLogs)
                Debug.Log($"[PlantotronUI] Added gene {gene.displayName} at end of sequence");
        }
        
        currentSelectedSeed.UpdateModifiedStatus();
        RefreshSequencePanel();
        
        return true;
    }
    
    public bool TryRemoveGeneFromSequence(int index)
    {
        if (currentSelectedSeed == null || index < 0 || index >= currentSelectedSeed.currentGenes.Count)
        {
            if (showDebugLogs)
                Debug.Log("[PlantotronUI] Cannot remove gene - invalid index or no seed selected");
            return false;
        }
        
        NodeDefinition gene = currentSelectedSeed.currentGenes[index];
        if (gene == null) return false;
        
        currentSelectedSeed.currentGenes.RemoveAt(index);
        currentSelectedSeed.UpdateModifiedStatus();
        
        inventory.ReturnGeneToInventory(gene);
        
        RefreshSequencePanel();
        
        if (showDebugLogs)
            Debug.Log($"[PlantotronUI] Removed gene {gene.displayName} from sequence");
        
        return true;
    }
    
    // Replacement method for PlantotronUI.TryMoveGeneInSequence
public bool TryMoveGeneInSequence(int fromIndex, int toIndex)
{
    if (currentSelectedSeed == null || currentSelectedSeed.currentGenes == null)
    {
        if (showDebugLogs)
            Debug.Log("[PlantotronUI] Cannot move gene - no seed selected or genes list is null");
        return false;
    }
        
    if (fromIndex < 0 || fromIndex >= currentSelectedSeed.currentGenes.Count)
    {
        if (showDebugLogs)
            Debug.Log($"[PlantotronUI] Cannot move gene - invalid fromIndex {fromIndex} (valid range: 0-{currentSelectedSeed.currentGenes.Count - 1})");
        return false;
    }
    
    // FIXED: Handle toIndex properly for drop zone insertion
    // When dropping on a drop zone, toIndex might be equal to count (inserting at end)
    if (toIndex < 0 || toIndex > currentSelectedSeed.currentGenes.Count)
    {
        if (showDebugLogs)
            Debug.Log($"[PlantotronUI] Cannot move gene - invalid toIndex {toIndex} (valid range: 0-{currentSelectedSeed.currentGenes.Count})");
        return false;
    }
    
    if (fromIndex == toIndex)
    {
        if (showDebugLogs)
            Debug.Log("[PlantotronUI] No move needed - fromIndex equals toIndex");
        return true; // Not really an error, just no change needed
    }
    
    NodeDefinition gene = currentSelectedSeed.currentGenes[fromIndex];
    if (gene == null)
    {
        if (showDebugLogs)
            Debug.Log($"[PlantotronUI] Cannot move gene - gene at index {fromIndex} is null");
        return false;
    }
    
    // FIXED: Properly handle insertion logic
    // Remove the gene from its current position
    currentSelectedSeed.currentGenes.RemoveAt(fromIndex);
    
    // Adjust insertion index if removing from before the target
    int adjustedToIndex = toIndex;
    if (fromIndex < toIndex)
    {
        adjustedToIndex = toIndex - 1;
    }
    
    // Ensure the adjusted index is still valid after removal
    adjustedToIndex = Mathf.Clamp(adjustedToIndex, 0, currentSelectedSeed.currentGenes.Count);
    
    // Insert at the adjusted position
    currentSelectedSeed.currentGenes.Insert(adjustedToIndex, gene);
    
    // Mark seed as modified
    currentSelectedSeed.UpdateModifiedStatus();
    
    // Refresh the sequence panel to reflect changes
    RefreshSequencePanel();
    
    if (showDebugLogs)
        Debug.Log($"[PlantotronUI] Moved gene {gene.displayName} from index {fromIndex} to {adjustedToIndex} (requested: {toIndex})");
    
    return true;
}
    
    // --- Utility Methods ---
    
    private void ClearContainer(Transform container, List<GameObject> itemList)
    {
        foreach (GameObject item in itemList)
        {
            if (item != null)
                DestroyImmediate(item);
        }
        itemList.Clear();
    }
    
    public SeedInstance GetCurrentSelectedSeed()
    {
        return currentSelectedSeed;
    }
    
    public bool IsUIOpen()
    {
        return isUIOpen;
    }
    
    // <<< NEW: Enable/Disable Drop Zones >>>
    public void EnableDropZones(bool enable)
    {
        if (enable)
        {
            // Create drop zones if they don't exist
            if (currentDropZones.Count == 0)
            {
                CreateDropZonesForDrag();
            }
        
            // Enable all drop zones
            foreach (var dropZone in currentDropZones)
            {
                if (dropZone != null)
                    dropZone.SetEnabled(true);
            }
        
            Debug.Log($"[PlantotronUI] Enabled {currentDropZones.Count} drop zones");
        }
        else
        {
            // Disable all drop zones
            foreach (var dropZone in currentDropZones)
            {
                if (dropZone != null)
                    dropZone.SetEnabled(false);
            }
        
            Debug.Log("[PlantotronUI] Disabled drop zones");
        }
    }
}