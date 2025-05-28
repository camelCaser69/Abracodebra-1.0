// FILE: Assets/Scripts/Nodes/Seeds/PlayerGeneticsInventory.cs (UPDATED)
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class PlayerGeneticsInventory : MonoBehaviour
{
    [System.Serializable]
    public class GeneCount
    {
        public NodeDefinition gene;
        public int count;
        
        public GeneCount(NodeDefinition gene, int count)
        {
            this.gene = gene;
            this.count = count;
        }
    }

    public static PlayerGeneticsInventory Instance { get; private set; }
    
    [Header("Starting Inventory")]
    [Tooltip("Genes the player starts with and their counts")]
    public List<GeneCount> startingGenes = new List<GeneCount>();
    
    [Tooltip("Seeds (SeedDefinitions) the player starts with")]
    public List<SeedDefinition> startingSeedDefinitions = new List<SeedDefinition>();
    
    [Header("Current Inventory")]
    [SerializeField] private List<GeneCount> availableGenes = new List<GeneCount>();
    [SerializeField] private List<SeedInstance> availableSeeds = new List<SeedInstance>();
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Events for UI updates
    public event Action OnInventoryChanged;
    public event Action<NodeDefinition, int> OnGeneCountChanged;
    public event Action<SeedInstance> OnSeedAdded;
    public event Action<SeedInstance> OnSeedRemoved;
    
    // Public accessors
    public List<GeneCount> AvailableGenes => availableGenes;
    public List<SeedInstance> AvailableSeeds => availableSeeds;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        InitializeStartingInventory();
    }
    
    private void InitializeStartingInventory()
    {
        // Add starting genes with their counts
        foreach (var geneCount in startingGenes)
        {
            if (geneCount.gene != null && geneCount.count > 0)
            {
                AddGeneCount(geneCount.gene, geneCount.count);
                if (showDebugLogs)
                    Debug.Log($"[PlayerGeneticsInventory] Added starting gene: {geneCount.gene.displayName} x{geneCount.count}");
            }
        }
        
        // Add starting seeds
        foreach (var seedDef in startingSeedDefinitions)
        {
            if (seedDef != null)
            {
                SeedInstance newSeed = new SeedInstance(seedDef);
                availableSeeds.Add(newSeed);
                if (showDebugLogs)
                    Debug.Log($"[PlayerGeneticsInventory] Added starting seed: {newSeed.seedName}");
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"[PlayerGeneticsInventory] Initialized with {availableGenes.Count} gene types and {availableSeeds.Count} seeds");
            
        OnInventoryChanged?.Invoke();
    }
    
    // --- Gene Count Management ---
    
    /// <summary>
    /// Adds genes to inventory with specified count
    /// </summary>
    public bool AddGeneCount(NodeDefinition gene, int count)
    {
        if (gene == null || count <= 0)
        {
            Debug.LogWarning("[PlayerGeneticsInventory] Tried to add invalid gene or count!");
            return false;
        }
        
        var existingGene = availableGenes.FirstOrDefault(g => g.gene == gene);
        if (existingGene != null)
        {
            existingGene.count += count;
        }
        else
        {
            availableGenes.Add(new GeneCount(gene, count));
        }
        
        if (showDebugLogs)
            Debug.Log($"[PlayerGeneticsInventory] Added {count} of gene: {gene.displayName}");
            
        OnGeneCountChanged?.Invoke(gene, GetGeneCount(gene));
        OnInventoryChanged?.Invoke();
        return true;
    }
    
    /// <summary>
    /// Tries to consume one count of a gene. Returns true if successful.
    /// </summary>
    public bool TryConsumeGene(NodeDefinition gene)
    {
        if (gene == null) return false;
        
        var geneCount = availableGenes.FirstOrDefault(g => g.gene == gene);
        if (geneCount == null || geneCount.count <= 0)
        {
            if (showDebugLogs)
                Debug.Log($"[PlayerGeneticsInventory] Cannot consume gene {gene.displayName} - not available or count is 0");
            return false;
        }
        
        geneCount.count--;
        
        if (geneCount.count <= 0)
        {
            availableGenes.Remove(geneCount);
            if (showDebugLogs)
                Debug.Log($"[PlayerGeneticsInventory] Removed gene {gene.displayName} from inventory (count reached 0)");
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"[PlayerGeneticsInventory] Consumed gene {gene.displayName}, remaining: {geneCount.count}");
        }
        
        OnGeneCountChanged?.Invoke(gene, GetGeneCount(gene));
        OnInventoryChanged?.Invoke();
        return true;
    }
    
    /// <summary>
    /// Returns a gene to inventory (increases count by 1)
    /// </summary>
    public void ReturnGeneToInventory(NodeDefinition gene)
    {
        if (gene == null) return;
        
        AddGeneCount(gene, 1);
        if (showDebugLogs)
            Debug.Log($"[PlayerGeneticsInventory] Returned gene {gene.displayName} to inventory");
    }
    
    /// <summary>
    /// Gets the current count of a specific gene
    /// </summary>
    public int GetGeneCount(NodeDefinition gene)
    {
        if (gene == null) return 0;
        
        var geneCount = availableGenes.FirstOrDefault(g => g.gene == gene);
        return geneCount?.count ?? 0;
    }
    
    /// <summary>
    /// Checks if player has at least one of the specified gene
    /// </summary>
    public bool HasGene(NodeDefinition gene)
    {
        return GetGeneCount(gene) > 0;
    }
    
    // --- Seed Management (Simplified) ---
    
    public bool AddSeed(SeedInstance seed)
    {
        if (seed == null)
        {
            Debug.LogWarning("[PlayerGeneticsInventory] Tried to add null seed!");
            return false;
        }
        
        availableSeeds.Add(seed);
        if (showDebugLogs)
            Debug.Log($"[PlayerGeneticsInventory] Added seed: {seed.seedName}");
            
        OnSeedAdded?.Invoke(seed);
        OnInventoryChanged?.Invoke();
        return true;
    }
    
    public bool RemoveSeed(SeedInstance seed)
    {
        if (seed == null || !availableSeeds.Contains(seed))
            return false;
            
        availableSeeds.Remove(seed);
        if (showDebugLogs)
            Debug.Log($"[PlayerGeneticsInventory] Removed seed: {seed.seedName}");
            
        OnSeedRemoved?.Invoke(seed);
        OnInventoryChanged?.Invoke();
        return true;
    }
    
    /// <summary>
    /// Gets seeds that are valid for planting
    /// </summary>
    public List<SeedInstance> GetPlantableSeeds()
    {
        return availableSeeds.Where(seed => seed != null && seed.IsValidForPlanting()).ToList();
    }
    
    /// <summary>
    /// Creates and adds a new seed from a SeedDefinition
    /// </summary>
    public SeedInstance AddSeedFromDefinition(SeedDefinition seedDef)
    {
        if (seedDef == null)
        {
            Debug.LogWarning("[PlayerGeneticsInventory] Tried to add seed from null definition!");
            return null;
        }
        
        SeedInstance newSeed = new SeedInstance(seedDef);
        AddSeed(newSeed);
        return newSeed;
    }
    
    /// <summary>
    /// Gets inventory statistics for debugging/UI
    /// </summary>
    public string GetInventoryStats()
    {
        int totalGenes = availableGenes.Sum(g => g.count);
        int vanillaSeeds = availableSeeds.Count(s => !s.isModified);
        int modifiedSeeds = availableSeeds.Count(s => s.isModified);
        int plantableSeeds = GetPlantableSeeds().Count;
        
        return $"Gene Types: {availableGenes.Count} (Total: {totalGenes}) | Seeds: {availableSeeds.Count} " +
               $"(Vanilla: {vanillaSeeds}, Modified: {modifiedSeeds}, Plantable: {plantableSeeds})";
    }
}