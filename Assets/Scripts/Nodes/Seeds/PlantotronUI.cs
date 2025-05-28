// FILE: Assets/Scripts/Nodes/Seeds/PlantotronUI.cs (FINAL FIXED)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

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
    
    [Header("Details Panel Components")]
    public TMP_Text detailsTitleText;
    public TMP_Text detailsDescriptionText;
    public ScrollRect detailsScrollRect;
    
    [Header("Control Buttons")]
    public Button closeButton;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Current state
    private SeedInstance currentSelectedSeed;
    private PlayerGeneticsInventory inventory;
    
    // UI item lists
    private List<GameObject> currentGeneItems = new List<GameObject>();
    private List<GameObject> currentSeedItems = new List<GameObject>();
    private List<GameObject> currentSequenceItems = new List<GameObject>();
    
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
        
        // Subscribe to inventory events
        inventory.OnInventoryChanged += RefreshAllPanels;
        inventory.OnGeneCountChanged += OnGeneCountChanged;
        
        // Ensure UI starts closed
        if (mainPanel != null)
            mainPanel.SetActive(false);
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
        if (geneListContainer == null) { Debug.LogError("[PlantotronUI] Gene List Container not assigned!", this); isValid = false; }
        if (seedListContainer == null) { Debug.LogError("[PlantotronUI] Seed List Container not assigned!", this); isValid = false; }
        if (sequenceContainer == null) { Debug.LogError("[PlantotronUI] Sequence Container not assigned!", this); isValid = false; }
        
        if (!isValid)
        {
            Debug.LogError("[PlantotronUI] Missing required components! UI will not function properly.", this);
            enabled = false;
        }
    }
    
    // --- Main UI Control ---
    
    public void OpenUI()
    {
        if (mainPanel != null)
            mainPanel.SetActive(true);
            
        RefreshAllPanels();
        ClearDetails();
        
        if (showDebugLogs)
            Debug.Log("[PlantotronUI] UI Opened");
    }
    
    public void CloseUI()
    {
        if (mainPanel != null)
            mainPanel.SetActive(false);
            
        // Clear current selection
        currentSelectedSeed = null;
        
        if (showDebugLogs)
            Debug.Log("[PlantotronUI] UI Closed");
    }
    
    // --- Panel Refresh Methods ---
    
    private void RefreshAllPanels()
    {
        RefreshGenesPanel();
        RefreshSeedsPanel();
        RefreshSequencePanel();
    }
    
    private void RefreshGenesPanel()
    {
        if (inventory == null || geneListContainer == null) return;
        
        // Clear existing items
        ClearContainer(geneListContainer, currentGeneItems);
        
        // Create UI items for each gene type
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
        
        // Clear existing items
        ClearContainer(seedListContainer, currentSeedItems);
        
        // Create UI items for each seed
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
    
    private void RefreshSequencePanel()
    {
        if (sequenceContainer == null) return;
        
        // Clear existing items
        ClearContainer(sequenceContainer, currentSequenceItems);
        
        if (currentSelectedSeed == null || currentSelectedSeed.currentGenes == null)
            return;
        
        // Create UI items for each gene in the sequence
        for (int i = 0; i < currentSelectedSeed.currentGenes.Count; i++)
        {
            if (currentSelectedSeed.currentGenes[i] != null)
            {
                GameObject sequenceItem = CreateSequenceItem(currentSelectedSeed.currentGenes[i], i);
                if (sequenceItem != null)
                    currentSequenceItems.Add(sequenceItem);
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"[PlantotronUI] Refreshed sequence panel: {currentSequenceItems.Count} genes");
    }
    
    // --- Item Creation Methods ---
    
    private GameObject CreateGeneItem(PlayerGeneticsInventory.GeneCount geneCount)
    {
        if (geneItemPrefab == null || geneListContainer == null || geneCount.gene == null)
            return null;
            
        GameObject item = Instantiate(geneItemPrefab, geneListContainer);
        
        // Setup the gene item component
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
        
        // Setup the seed item component
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
        
        // Setup the sequence item component
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
        
        // Add effect information
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
        
        // Add count information
        int count = inventory?.GetGeneCount(gene) ?? 0;
        details += $"\n\n<b>Available:</b> {count}";
        
        detailsDescriptionText.text = details;
        
        // Scroll to top
        if (detailsScrollRect != null)
            detailsScrollRect.verticalNormalizedPosition = 1f;
    }
    
    public void ShowSeedDetails(SeedInstance seed)
    {
        if (seed == null || detailsTitleText == null || detailsDescriptionText == null)
            return;
            
        detailsTitleText.text = seed.seedName;
        
        string details = "";
        
        // Add basic info
        if (seed.baseSeedDefinition != null && !string.IsNullOrEmpty(seed.baseSeedDefinition.description))
        {
            details += seed.baseSeedDefinition.description + "\n\n";
        }
        
        // Add modification info
        details += $"<b>Status:</b> {(seed.isModified ? "Modified" : "Vanilla")}\n";
        details += $"<b>Genes:</b> {(seed.currentGenes?.Count ?? 0)}\n";
        details += $"<b>Plantable:</b> {(seed.IsValidForPlanting() ? "Yes" : "No")}\n\n";
        
        // Add genetic composition
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
        
        // Scroll to top
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
        // Update existing gene items display
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
        
        // If count reached 0, refresh the entire panel to remove it
        if (newCount <= 0)
        {
            RefreshGenesPanel();
        }
    }
    
    // --- Public Methods for Item Interactions ---
    
    public void OnSeedSelected(SeedInstance seed)
    {
        currentSelectedSeed = seed;
        RefreshSequencePanel();
        ShowSeedDetails(seed);
        
        if (showDebugLogs)
            Debug.Log($"[PlantotronUI] Selected seed: {seed.seedName}");
    }
    
    public void OnGeneClicked(NodeDefinition gene)
    {
        ShowGeneDetails(gene);
    }
    
    // --- Drag & Drop Methods ---
    
    public bool TryAddGeneToSequence(NodeDefinition gene, int insertIndex = -1)
    {
        if (currentSelectedSeed == null || gene == null)
        {
            if (showDebugLogs)
                Debug.Log("[PlantotronUI] Cannot add gene - no seed selected or gene is null");
            return false;
        }
        
        // Try to consume the gene from inventory
        if (!inventory.TryConsumeGene(gene))
        {
            if (showDebugLogs)
                Debug.Log($"[PlantotronUI] Cannot add gene {gene.displayName} - not available in inventory");
            return false;
        }
        
        // Add to seed's gene sequence
        if (insertIndex >= 0 && insertIndex < currentSelectedSeed.currentGenes.Count)
        {
            currentSelectedSeed.currentGenes.Insert(insertIndex, gene);
        }
        else
        {
            currentSelectedSeed.currentGenes.Add(gene);
        }
        
        currentSelectedSeed.UpdateModifiedStatus();
        RefreshSequencePanel();
        
        if (showDebugLogs)
            Debug.Log($"[PlantotronUI] Added gene {gene.displayName} to sequence");
        
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
        
        // Remove from sequence
        currentSelectedSeed.currentGenes.RemoveAt(index);
        currentSelectedSeed.UpdateModifiedStatus();
        
        // Return to inventory
        inventory.ReturnGeneToInventory(gene);
        
        RefreshSequencePanel();
        
        if (showDebugLogs)
            Debug.Log($"[PlantotronUI] Removed gene {gene.displayName} from sequence");
        
        return true;
    }
    
    public bool TryMoveGeneInSequence(int fromIndex, int toIndex)
    {
        if (currentSelectedSeed == null || currentSelectedSeed.currentGenes == null)
            return false;
            
        if (fromIndex < 0 || fromIndex >= currentSelectedSeed.currentGenes.Count ||
            toIndex < 0 || toIndex >= currentSelectedSeed.currentGenes.Count ||
            fromIndex == toIndex)
            return false;
            
        NodeDefinition gene = currentSelectedSeed.currentGenes[fromIndex];
        currentSelectedSeed.currentGenes.RemoveAt(fromIndex);
        currentSelectedSeed.currentGenes.Insert(toIndex, gene);
        currentSelectedSeed.UpdateModifiedStatus();
        
        RefreshSequencePanel();
        
        if (showDebugLogs)
            Debug.Log($"[PlantotronUI] Moved gene {gene.displayName} from {fromIndex} to {toIndex}");
        
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
}