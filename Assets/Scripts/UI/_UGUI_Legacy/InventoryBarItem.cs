using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Templates;

// Note: ItemInstance and ItemDefinition are assumed to be in the global namespace
// or a namespace included via 'using'. If they are in a specific namespace,
// you will need to add the corresponding 'using' statement at the top.

public class InventoryBarItem
{
    // MODIFIED: Renamed 'Item' to 'Resource' for clarity against 'FoodItem', etc.
    public enum ItemType { Gene, Seed, Tool, Resource }

    public ItemType Type { get; set; }

    // Existing properties
    public RuntimeGeneInstance GeneInstance { get; set; }
    public ToolDefinition ToolDefinition { get; set; }
    public SeedTemplate SeedTemplate { get; set; }
    public PlantGeneRuntimeState SeedRuntimeState { get; set; }

    // NEW: Property for the new item system
    public ItemInstance ItemInstance { get; set; }


    // --- Factory Methods ---

    public static InventoryBarItem FromGene(RuntimeGeneInstance instance)
    {
        if (instance == null) return null;
        return new InventoryBarItem
        {
            Type = ItemType.Gene,
            GeneInstance = instance
        };
    }

    public static InventoryBarItem FromSeed(SeedTemplate seed)
    {
        if (seed == null) return null;
        return new InventoryBarItem
        {
            Type = ItemType.Seed,
            SeedTemplate = seed,
            SeedRuntimeState = seed.CreateRuntimeState()
        };
    }

    public static InventoryBarItem FromTool(ToolDefinition tool)
    {
        if (tool == null) return null;
        return new InventoryBarItem
        {
            Type = ItemType.Tool,
            ToolDefinition = tool
        };
    }

    // NEW: Factory method for creating an inventory item from an ItemInstance
    public static InventoryBarItem FromItem(ItemInstance instance)
    {
        if (instance == null || instance.definition == null) return null;
        return new InventoryBarItem
        {
            Type = ItemType.Resource,
            ItemInstance = instance
        };
    }


    // --- Helper Methods ---

    public string GetDisplayName()
    {
        switch (Type)
        {
            case ItemType.Gene:
                return GeneInstance?.GetGene()?.geneName ?? "Unknown Gene";
            case ItemType.Seed:
                return SeedTemplate?.templateName ?? "Unknown Seed";
            case ItemType.Tool:
                return ToolDefinition?.displayName ?? "Unknown Tool";
            case ItemType.Resource: // NEW
                return ItemInstance?.definition?.itemName ?? "Unknown Item";
            default:
                return "Invalid Item";
        }
    }

    public Sprite GetIcon()
    {
        switch (Type)
        {
            case ItemType.Gene:
                return GeneInstance?.GetGene()?.icon;
            case ItemType.Seed:
                return SeedTemplate?.icon;
            case ItemType.Tool:
                return ToolDefinition?.icon;
            case ItemType.Resource: // NEW
                return ItemInstance?.definition?.icon;
            default:
                return null;
        }
    }

    public bool IsValid()
    {
        switch (Type)
        {
            case ItemType.Gene:
                return GeneInstance?.GetGene() != null;
            case ItemType.Seed:
                return SeedTemplate != null && SeedRuntimeState != null;
            case ItemType.Tool:
                return ToolDefinition != null;
            case ItemType.Resource: // NEW
                return ItemInstance?.definition != null;
            default:
                return false;
        }
    }
}