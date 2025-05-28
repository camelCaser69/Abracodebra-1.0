// FILE: Assets/Scripts/Genetics/SeedInstance.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class SeedInstance
{
    [Header("Seed Identity")]
    public string seedName;
    public string seedId; // Unique identifier for this specific seed instance
    
    [Header("Genetic Data")]
    public SeedDefinition baseSeedDefinition;
    public List<NodeDefinition> currentGenes = new List<NodeDefinition>();
    
    [Header("Metadata")]
    public bool isModified = false;
    public DateTime creationTime;
    
    // Default constructor
    public SeedInstance()
    {
        seedId = Guid.NewGuid().ToString();
        creationTime = DateTime.Now;
    }
    
    // Constructor from SeedDefinition
    public SeedInstance(SeedDefinition seedDef)
    {
        if (seedDef == null)
        {
            Debug.LogError("Cannot create SeedInstance from null SeedDefinition!");
            return;
        }
        
        seedId = Guid.NewGuid().ToString();
        creationTime = DateTime.Now;
        baseSeedDefinition = seedDef;
        seedName = seedDef.seedName;
        currentGenes = seedDef.CloneInitialGenes();
        isModified = false;
    }
    
    /// <summary>
    /// Copy constructor for creating duplicates
    /// </summary>
    public SeedInstance(SeedInstance original)
    {
        if (original == null)
        {
            Debug.LogError("Cannot create SeedInstance from null original!");
            return;
        }
        
        seedId = Guid.NewGuid().ToString(); // Always generate new ID
        creationTime = DateTime.Now;
        baseSeedDefinition = original.baseSeedDefinition;
        seedName = original.seedName;
        isModified = original.isModified;
        
        // Deep copy the genes list
        currentGenes = new List<NodeDefinition>();
        if (original.currentGenes != null)
        {
            foreach (var gene in original.currentGenes)
            {
                if (gene != null)
                {
                    currentGenes.Add(gene);
                }
            }
        }
    }
    
    /// <summary>
    /// Checks if current genes match the original seed definition
    /// </summary>
    public bool GenesMatchOriginal()
    {
        if (baseSeedDefinition == null || baseSeedDefinition.initialGenes == null)
            return false;
            
        if (currentGenes == null)
            return baseSeedDefinition.initialGenes.Count == 0;
            
        if (currentGenes.Count != baseSeedDefinition.initialGenes.Count)
            return false;
            
        for (int i = 0; i < currentGenes.Count; i++)
        {
            if (currentGenes[i] != baseSeedDefinition.initialGenes[i])
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Updates the modified flag based on current gene state
    /// </summary>
    public void UpdateModifiedStatus()
    {
        isModified = !GenesMatchOriginal();
        
        // Update seed name to reflect modification
        if (baseSeedDefinition != null)
        {
            if (isModified && !seedName.Contains("(Modified)"))
            {
                seedName = baseSeedDefinition.seedName + " (Modified)";
            }
            else if (!isModified && seedName.Contains("(Modified)"))
            {
                seedName = baseSeedDefinition.seedName;
            }
        }
    }
    
    /// <summary>
    /// Adds a gene to the seed at the specified index
    /// </summary>
    public void AddGene(NodeDefinition gene, int index = -1)
    {
        if (gene == null) return;
        
        if (currentGenes == null)
            currentGenes = new List<NodeDefinition>();
            
        if (index < 0 || index >= currentGenes.Count)
        {
            currentGenes.Add(gene);
        }
        else
        {
            currentGenes.Insert(index, gene);
        }
        
        UpdateModifiedStatus();
    }
    
    /// <summary>
    /// Removes a gene from the seed at the specified index
    /// </summary>
    public bool RemoveGeneAt(int index)
    {
        if (currentGenes == null || index < 0 || index >= currentGenes.Count)
            return false;
            
        currentGenes.RemoveAt(index);
        UpdateModifiedStatus();
        return true;
    }
    
    /// <summary>
    /// Moves a gene from one position to another
    /// </summary>
    public bool MoveGene(int fromIndex, int toIndex)
    {
        if (currentGenes == null || fromIndex < 0 || fromIndex >= currentGenes.Count || 
            toIndex < 0 || toIndex >= currentGenes.Count || fromIndex == toIndex)
            return false;
            
        NodeDefinition gene = currentGenes[fromIndex];
        currentGenes.RemoveAt(fromIndex);
        currentGenes.Insert(toIndex, gene);
        UpdateModifiedStatus();
        return true;
    }
    
    /// <summary>
    /// Converts this seed instance to a NodeGraph for planting
    /// </summary>
    public NodeGraph ToNodeGraph()
    {
        NodeGraph graph = new NodeGraph();
        
        if (currentGenes == null || currentGenes.Count == 0)
        {
            Debug.LogWarning($"SeedInstance '{seedName}' has no genes to convert to NodeGraph!");
            return graph;
        }
        
        // Convert each gene to NodeData
        for (int i = 0; i < currentGenes.Count; i++)
        {
            NodeDefinition gene = currentGenes[i];
            if (gene == null) continue;
            
            NodeData nodeData = new NodeData
            {
                nodeId = Guid.NewGuid().ToString(),
                nodeDisplayName = gene.displayName,
                orderIndex = i, // Top-to-bottom becomes order index
                effects = gene.CloneEffects(),
                canBeDeleted = true // Allow deletion in UI if needed
            };
            
            graph.nodes.Add(nodeData);
        }
        
        return graph;
    }
    
    /// <summary>
    /// Validates that this seed has the minimum requirements to grow
    /// </summary>
    public bool IsValidForPlanting()
    {
        if (currentGenes == null || currentGenes.Count == 0)
            return false;
            
        // Check for required SeedSpawn effect
        foreach (var gene in currentGenes)
        {
            if (gene != null && gene.effects != null)
            {
                foreach (var effect in gene.effects)
                {
                    if (effect != null && effect.effectType == NodeEffectType.SeedSpawn && effect.isPassive)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets a summary description of this seed's genetic composition
    /// </summary>
    public string GetGeneticSummary()
    {
        if (currentGenes == null || currentGenes.Count == 0)
            return "No genes present.";
            
        List<string> geneNames = new List<string>();
        foreach (var gene in currentGenes)
        {
            if (gene != null && !string.IsNullOrEmpty(gene.displayName))
            {
                geneNames.Add(gene.displayName);
            }
        }
        
        if (geneNames.Count == 0)
            return "No valid genes present.";
            
        return string.Join(" → ", geneNames);
    }
}