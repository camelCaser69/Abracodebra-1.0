// Assets/Scripts/PlantSystem/UI/InventoryColorManager.cs
using UnityEngine;
using System.Linq;

public class InventoryColorManager : MonoBehaviour
{
    public static InventoryColorManager Instance { get; set; }

    [Header("Cell Background Colors")]
    [SerializeField] Color toolCellColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray
    [SerializeField] Color seedCellColor = new Color(0.8f, 1f, 0.8f, 1f); // Light Green
    [SerializeField] Color passiveGeneCellColor = new Color(0.8f, 0.8f, 1f, 1f); // Light Blue
    [SerializeField] Color activeGeneCellColor = new Color(1f, 0.8f, 0.8f, 1f); // Light Red
    [SerializeField] Color defaultCellColor = new Color(0.9f, 0.9f, 0.9f, 1f); // Light Gray

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
        if (toolDefinition != null)
        {
            return toolCellColor;
        }

        if (nodeData != null && nodeDefinition != null)
        {
            if (nodeData.IsSeed())
            {
                return seedCellColor;
            }

            if (nodeData.effects != null && nodeData.effects.Count > 0)
            {
                // Check if any effect is considered active
                bool hasActiveEffects = nodeData.effects.Any(e => e != null && e.IsActive);

                if (hasActiveEffects)
                {
                    return activeGeneCellColor;
                }
                else
                {
                    return passiveGeneCellColor;
                }
            }
        }

        return defaultCellColor;
    }

    public enum ItemCategory
    {
        Tool,
        Seed,
        PassiveGene,
        ActiveGene,
        Default
    }

    public ItemCategory GetItemCategory(NodeData nodeData, NodeDefinition nodeDefinition, ToolDefinition toolDefinition)
    {
        if (toolDefinition != null) return ItemCategory.Tool;

        if (nodeData != null && nodeDefinition != null)
        {
            if (nodeData.IsSeed()) return ItemCategory.Seed;

            if (nodeData.effects != null && nodeData.effects.Count > 0)
            {
                bool hasActiveEffects = nodeData.effects.Any(e => e != null && e.IsActive);
                return hasActiveEffects ? ItemCategory.ActiveGene : ItemCategory.PassiveGene;
            }
        }

        return ItemCategory.Default;
    }
}