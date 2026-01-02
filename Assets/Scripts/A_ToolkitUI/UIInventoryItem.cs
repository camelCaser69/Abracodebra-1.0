using UnityEngine;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.UI.Toolkit
{
    /// <summary>
    /// Represents an item in the player's inventory with its visual and data properties.
    /// Supports: SeedTemplate, ToolDefinition, GeneBase, ItemInstance (resources/consumables)
    /// </summary>
    public class UIInventoryItem
    {
        public Sprite Icon { get; private set; }
        public int StackSize { get; set; } = 1;
        public object OriginalData { get; }
        
        // Seed-specific: runtime gene state (settable for when loading from save)
        public PlantGeneRuntimeState SeedRuntimeState { get; set; }

        // Resource-specific: the item instance with dynamic properties
        public ItemInstance ResourceInstance { get; }

        // Custom background color for seed identification (default: transparent)
        public Color BackgroundColor { get; set; } = new Color(0, 0, 0, 0);

        // Custom name for seed (player-editable)
        public string CustomName { get; set; } = "";

        /// <summary>
        /// Create from SeedTemplate
        /// </summary>
        public UIInventoryItem(SeedTemplate seed)
        {
            OriginalData = seed;
            Icon = seed?.icon;
            SeedRuntimeState = seed?.CreateRuntimeState();
        }

        /// <summary>
        /// Create from ToolDefinition
        /// </summary>
        public UIInventoryItem(ToolDefinition tool)
        {
            OriginalData = tool;
            Icon = tool?.icon;
        }

        /// <summary>
        /// Create from GeneBase
        /// </summary>
        public UIInventoryItem(GeneBase gene)
        {
            OriginalData = gene;
            Icon = gene?.icon;
        }

        /// <summary>
        /// Create from ItemInstance (harvested resources, consumables)
        /// </summary>
        public UIInventoryItem(ItemInstance itemInstance)
        {
            OriginalData = itemInstance?.definition;
            ResourceInstance = itemInstance;
            Icon = itemInstance?.definition?.icon;
            StackSize = itemInstance?.stackCount ?? 1;
        }

        /// <summary>
        /// Generic constructor for backwards compatibility
        /// </summary>
        public UIInventoryItem(object data)
        {
            OriginalData = data;

            if (data is SeedTemplate seed)
            {
                Icon = seed.icon;
                SeedRuntimeState = seed.CreateRuntimeState();
            }
            else if (data is ToolDefinition tool)
            {
                Icon = tool.icon;
            }
            else if (data is GeneBase gene)
            {
                Icon = gene.icon;
            }
            else if (data is ItemInstance item)
            {
                Icon = item.definition?.icon;
                StackSize = item.stackCount;
            }
            else if (data is ItemDefinition itemDef)
            {
                Icon = itemDef.icon;
            }
        }

        /// <summary>
        /// Check if this item has a custom background color set
        /// </summary>
        public bool HasCustomColor()
        {
            return BackgroundColor.a > 0.01f;
        }

        /// <summary>
        /// Check if this is a consumable item
        /// </summary>
        public bool IsConsumable()
        {
            if (ResourceInstance != null)
                return ResourceInstance.definition?.isConsumable ?? false;
            if (OriginalData is ItemDefinition itemDef)
                return itemDef.isConsumable;
            return false;
        }

        /// <summary>
        /// Get nutrition value (for consumables)
        /// </summary>
        public float GetNutrition()
        {
            if (ResourceInstance != null)
                return ResourceInstance.GetNutrition();
            if (OriginalData is ItemDefinition itemDef)
                return itemDef.baseNutrition;
            return 0f;
        }

        /// <summary>
        /// Get the display name for this item (custom name if set, otherwise default)
        /// </summary>
        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(CustomName))
            {
                return CustomName;
            }

            return OriginalData switch
            {
                SeedTemplate seed => seed.templateName,
                ToolDefinition tool => tool.displayName,
                GeneBase gene => gene.geneName,
                ItemDefinition itemDef => itemDef.itemName,
                ItemInstance item => item.definition?.itemName ?? "Unknown Resource",
                _ => "Unknown Item"
            };
        }

        /// <summary>
        /// Get a description for this item
        /// </summary>
        public string GetDescription()
        {
            return OriginalData switch
            {
                SeedTemplate seed => seed.description,
                ToolDefinition tool => tool.GetTooltipDescription(),
                GeneBase gene => gene.description,
                ItemDefinition itemDef => itemDef.description,
                _ => ""
            };
        }
    }
}
