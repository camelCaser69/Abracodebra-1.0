using System.Linq;
using UnityEngine;

public class InventoryColorManager : MonoBehaviour
{
    public static InventoryColorManager Instance { get; private set; }

    [Header("Cell Background Colors by Category")]
    [SerializeField] private Color toolCellColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray
    [SerializeField] private Color seedCellColor = new Color(0.8f, 1f, 0.8f, 1f); // Light Green
    [SerializeField] private Color passiveGeneCellColor = new Color(0.8f, 0.8f, 1f, 1f); // Light Blue
    [SerializeField] private Color activeGeneCellColor = new Color(1f, 0.8f, 0.8f, 1f); // Light Red
    [SerializeField] private Color defaultCellColor = new Color(0.9f, 0.9f, 0.9f, 1f); // Light Gray

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
        // Tool
        if (toolDefinition != null)
        {
            return toolCellColor;
        }

        // Node-based items
        if (nodeData != null && nodeDefinition != null)
        {
            // Check if it's a seed
            if (nodeData.IsSeed())
            {
                return seedCellColor;
            }

            // Check if it has any effects
            if (nodeData.effects != null && nodeData.effects.Count > 0)
            {
                // Check if all effects are passive
                bool hasActiveEffects = nodeData.effects.Any(e => e != null && !e.isPassive);
                
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

    // Helper method to categorize items
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
                bool hasActiveEffects = nodeData.effects.Any(e => e != null && !e.isPassive);
                return hasActiveEffects ? ItemCategory.ActiveGene : ItemCategory.PassiveGene;
            }
        }
        
        return ItemCategory.Default;
    }
}