// FILE: Assets/Scripts/Genetics/SeedDefinition.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Seed_", menuName = "Genetics/Seed Definition")]
public class SeedDefinition : ScriptableObject
{
    [Header("Basic Information")]
    [Tooltip("Display name for this seed type")]
    public string seedName = "Unknown Seed";
    
    [Tooltip("Description of this seed's characteristics")]
    [TextArea(3, 5)]
    public string description = "A mysterious seed with unknown properties.";
    
    [Tooltip("Icon for this seed type")]
    public Sprite icon;
    
    [Header("Genetic Composition")]
    [Tooltip("Starting genes (nodes) that this seed contains")]
    public List<NodeDefinition> initialGenes = new List<NodeDefinition>();
    
    [Header("Seed Properties")]
    [Tooltip("Is this a vanilla (unmodified) seed type?")]
    public bool isVanillaSeed = true;
    
    [Tooltip("Rarity level for potential future progression systems")]
    [Range(1, 5)]
    public int rarityLevel = 1;
    
    /// <summary>
    /// Creates a deep copy of the initial genes list for use in SeedInstance
    /// </summary>
    public List<NodeDefinition> CloneInitialGenes()
    {
        List<NodeDefinition> clonedGenes = new List<NodeDefinition>();
        if (initialGenes != null)
        {
            foreach (var gene in initialGenes)
            {
                if (gene != null)
                {
                    clonedGenes.Add(gene);
                }
            }
        }
        return clonedGenes;
    }
    
    /// <summary>
    /// Validates that this seed definition has the minimum required genes to grow
    /// </summary>
    public bool IsValidSeed()
    {
        if (initialGenes == null || initialGenes.Count == 0)
            return false;
            
        // Check for required SeedSpawn effect
        foreach (var gene in initialGenes)
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
}