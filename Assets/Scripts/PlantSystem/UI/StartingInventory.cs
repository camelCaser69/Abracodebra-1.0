using UnityEngine;
using System.Collections.Generic;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;
using Abracodabra.UI.Genes; // Assuming ToolDefinition is here for now

[CreateAssetMenu(fileName = "NewStartingInventory", menuName = "Abracodabra/Core/Starting Inventory")]
public class StartingInventory : ScriptableObject
{
    [Header("Starting Genes")]
    [Tooltip("These are raw genes that the player can slot into seeds.")]
    public List<GeneBase> startingGenes = new List<GeneBase>();

    [Header("Starting Seeds")]
    [Tooltip("These are pre-configured seed templates that will appear in the inventory.")]
    public List<SeedTemplate> startingSeeds = new List<SeedTemplate>();

    [Header("Starting Tools")]
    [Tooltip("Tools the player begins the game with.")]
    public List<ToolDefinition> startingTools = new List<ToolDefinition>();
}