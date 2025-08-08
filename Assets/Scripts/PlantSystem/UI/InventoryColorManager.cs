// Reworked File: Assets/Scripts/PlantSystem/UI/InventoryColorManager.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;

public class InventoryColorManager : MonoBehaviour
{
    public static InventoryColorManager Instance { get; private set; }

    [SerializeField] private Color toolCellColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color seedCellColor = new Color(0.8f, 1f, 0.8f, 1f);
    [SerializeField] private Color passiveGeneCellColor = new Color(0.8f, 0.8f, 1f, 1f);
    [SerializeField] private Color activeGeneCellColor = new Color(1f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color modifierGeneCellColor = new Color(1f, 1f, 0.7f, 1f); // Yellowish
    [SerializeField] private Color payloadGeneCellColor = new Color(1f, 0.7f, 1f, 1f);
    [SerializeField] private Color defaultCellColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public Color GetCellColorForItem(GeneBase gene, SeedTemplate seed, ToolDefinition tool)
    {
        switch (GetItemCategory(gene, seed, tool))
        {
            case ItemCategory.Tool: return toolCellColor;
            case ItemCategory.Seed: return seedCellColor;
            case ItemCategory.PassiveGene: return passiveGeneCellColor;
            case ItemCategory.ActiveGene: return activeGeneCellColor;
            case ItemCategory.ModifierGene: return modifierGeneCellColor;
            case ItemCategory.PayloadGene: return payloadGeneCellColor;
            default: return defaultCellColor;
        }
    }

    public enum ItemCategory
    {
        Tool,
        Seed,
        PassiveGene,
        ActiveGene,
        ModifierGene,
        PayloadGene,
        Default
    }

    public ItemCategory GetItemCategory(GeneBase gene, SeedTemplate seed, ToolDefinition tool)
    {
        if (tool != null) return ItemCategory.Tool;
        if (seed != null) return ItemCategory.Seed;
        if (gene != null)
        {
            switch (gene.Category)
            {
                case GeneCategory.Passive: return ItemCategory.PassiveGene;
                case GeneCategory.Active: return ItemCategory.ActiveGene;
                case GeneCategory.Modifier: return ItemCategory.ModifierGene;
                case GeneCategory.Payload: return ItemCategory.PayloadGene;
            }
        }
        return ItemCategory.Default;
    }
}