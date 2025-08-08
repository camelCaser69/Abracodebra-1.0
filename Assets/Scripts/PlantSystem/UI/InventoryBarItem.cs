// Reworked File: Assets/Scripts/PlantSystem/UI/InventoryBarItem.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Templates;

/// <summary>
/// Represents a selectable item in the inventory bar, rebuilt for the new gene system.
/// </summary>
public class InventoryBarItem
{
    public enum ItemType { Gene, Seed, Tool }

    public ItemType Type { get; private set; }
    public RuntimeGeneInstance GeneInstance { get; private set; }
    public SeedTemplate SeedTemplate { get; private set; }
    public ToolDefinition ToolDefinition { get; private set; }
    public GameObject ViewGameObject { get; private set; } // The UI object in the inventory

    public static InventoryBarItem FromGene(RuntimeGeneInstance instance, GameObject viewObj = null)
    {
        if (instance == null) return null;
        return new InventoryBarItem
        {
            Type = ItemType.Gene,
            GeneInstance = instance,
            ViewGameObject = viewObj
        };
    }

    public static InventoryBarItem FromSeed(SeedTemplate seed, GameObject viewObj = null)
    {
        if (seed == null) return null;
        return new InventoryBarItem
        {
            Type = ItemType.Seed,
            SeedTemplate = seed,
            ViewGameObject = viewObj
        };
    }

    public static InventoryBarItem FromTool(ToolDefinition tool, GameObject viewObj = null)
    {
        if (tool == null) return null;
        return new InventoryBarItem
        {
            Type = ItemType.Tool,
            ToolDefinition = tool,
            ViewGameObject = viewObj
        };
    }

    public string GetDisplayName()
    {
        switch (Type)
        {
            case ItemType.Gene: return GeneInstance?.GetGene()?.geneName ?? "Unknown Gene";
            case ItemType.Seed: return SeedTemplate?.templateName ?? "Unknown Seed";
            case ItemType.Tool: return ToolDefinition?.displayName ?? "Unknown Tool";
            default: return "Invalid Item";
        }
    }

    public Sprite GetIcon()
    {
        switch (Type)
        {
            case ItemType.Gene: return GeneInstance?.GetGene()?.icon;
            case ItemType.Seed: return SeedTemplate?.icon;
            case ItemType.Tool: return ToolDefinition?.icon;
            default: return null;
        }
    }
    
    public bool IsValid()
    {
        switch (Type)
        {
            case ItemType.Gene: return GeneInstance?.GetGene() != null;
            case ItemType.Seed: return SeedTemplate != null;
            case ItemType.Tool: return ToolDefinition != null;
            default: return false;
        }
    }
}