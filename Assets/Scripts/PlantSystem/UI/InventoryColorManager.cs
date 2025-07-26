// Assets/Scripts/PlantSystem/UI/InventoryColorManager.cs
using UnityEngine;
using System.Linq;

public class InventoryColorManager : MonoBehaviour
{
    public static InventoryColorManager Instance { get; private set; }

    [Header("Cell Background Colors")]
    [SerializeField] private Color toolCellColor = new Color(0.5f, 0.5f, 0.5f, 1f);       // Gray
    [SerializeField] private Color seedCellColor = new Color(0.8f, 1f, 0.8f, 1f);       // Light Green
    [SerializeField] private Color passiveGeneCellColor = new Color(0.8f, 0.8f, 1f, 1f);    // Light Blue
    [SerializeField] private Color activeGeneCellColor = new Color(1f, 0.8f, 0.8f, 1f);     // Light Red / Orange
    [SerializeField] private Color payloadGeneCellColor = new Color(1f, 0.7f, 1f, 1f);    // Light Magenta
    [SerializeField] private Color defaultCellColor = new Color(0.9f, 0.9f, 0.9f, 1f);    // Light Gray

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public Color GetCellColorForItem(NodeData nodeData, NodeDefinition nodeDefinition, ToolDefinition toolDefinition)
    {
        switch (GetItemCategory(nodeData, nodeDefinition, toolDefinition))
        {
            case ItemCategory.Tool:
                return toolCellColor;
            case ItemCategory.Seed:
                return seedCellColor;
            case ItemCategory.PassiveGene:
                return passiveGeneCellColor;
            case ItemCategory.ActiveGene:
                return activeGeneCellColor;
            case ItemCategory.PayloadGene:
                return payloadGeneCellColor;
            default:
                return defaultCellColor;
        }
    }

    public enum ItemCategory
    {
        Tool,
        Seed,
        PassiveGene,
        ActiveGene,
        PayloadGene,
        Default
    }

    public ItemCategory GetItemCategory(NodeData nodeData, NodeDefinition nodeDefinition, ToolDefinition toolDefinition)
    {
        if (toolDefinition != null) return ItemCategory.Tool;

        if (nodeData != null && nodeDefinition != null)
        {
            if (nodeData.IsSeed()) return ItemCategory.Seed;

            // This logic is now based on the NodeDefinition's ActivationType, fixing the compiler error.
            switch (nodeDefinition.ActivationType)
            {
                case GeneActivationType.Passive:
                    return ItemCategory.PassiveGene;
                case GeneActivationType.Active:
                    return ItemCategory.ActiveGene;
                case GeneActivationType.Payload:
                    return ItemCategory.PayloadGene;
            }
        }

        return ItemCategory.Default;
    }
}