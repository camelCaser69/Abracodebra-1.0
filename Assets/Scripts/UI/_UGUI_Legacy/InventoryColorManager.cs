using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;

// Note: Add 'using' for ItemDefinition if it's in a namespace.

public class InventoryColorManager : MonoBehaviour
{
    public static InventoryColorManager Instance { get; set; }

    [Header("Cell Background Colors")]
    [SerializeField] private Color toolCellColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color seedCellColor = new Color(0.8f, 1f, 0.8f, 1f);
    [SerializeField] private Color passiveGeneCellColor = new Color(0.8f, 0.8f, 1f, 1f);
    [SerializeField] private Color activeGeneCellColor = new Color(1f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color modifierGeneCellColor = new Color(1f, 1f, 0.7f, 1f);
    [SerializeField] private Color payloadGeneCellColor = new Color(1f, 0.7f, 1f, 1f);
    // NEW: Color for harvested resources/items
    [SerializeField] private Color resourceCellColor = new Color(0.9f, 0.85f, 0.7f, 1f);
    [SerializeField] private Color defaultCellColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public Color GetCellColorForItem(GeneBase gene, SeedTemplate seed, ToolDefinition tool, ItemDefinition item)
    {
        switch (GetItemCategory(gene, seed, tool, item))
        {
            case ItemUIType.Tool: return toolCellColor;
            case ItemUIType.Seed: return seedCellColor;
            case ItemUIType.PassiveGene: return passiveGeneCellColor;
            case ItemUIType.ActiveGene: return activeGeneCellColor;
            case ItemUIType.ModifierGene: return modifierGeneCellColor;
            case ItemUIType.PayloadGene: return payloadGeneCellColor;
            case ItemUIType.Resource: return resourceCellColor; // NEW
            default: return defaultCellColor;
        }
    }

    // Renamed for clarity to avoid confusion with the ItemCategory enum.
    public enum ItemUIType
    {
        Tool,
        Seed,
        PassiveGene,
        ActiveGene,
        ModifierGene,
        PayloadGene,
        Resource, // NEW
        Default
    }

    public ItemUIType GetItemCategory(GeneBase gene, SeedTemplate seed, ToolDefinition tool, ItemDefinition item)
    {
        if (tool != null) return ItemUIType.Tool;
        if (seed != null) return ItemUIType.Seed;
        if (item != null) return ItemUIType.Resource; // NEW
        if (gene != null)
        {
            switch (gene.Category)
            {
                case GeneCategory.Passive: return ItemUIType.PassiveGene;
                case GeneCategory.Active: return ItemUIType.ActiveGene;
                case GeneCategory.Modifier: return ItemUIType.ModifierGene;
                case GeneCategory.Payload: return ItemUIType.PayloadGene;
            }
        }
        return ItemUIType.Default;
    }
}