using UnityEngine;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.UI.Toolkit
{
    /// <summary>
    /// Represents an item in the player's inventory with its visual and data properties
    /// </summary>
    public class UIInventoryItem
    {
        public Sprite Icon { get; }
        public int StackSize { get; set; } = 1;
        public object OriginalData { get; }
        public PlantGeneRuntimeState SeedRuntimeState { get; }

        // Custom background color for seed identification (default: transparent)
        public Color BackgroundColor { get; set; } = new Color(0, 0, 0, 0);

        // Custom name for seed (player-editable)
        public string CustomName { get; set; } = "";

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
        }

        /// <summary>
        /// Check if this item has a custom background color set
        /// </summary>
        public bool HasCustomColor()
        {
            return BackgroundColor.a > 0.01f; // Has any alpha
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
                _ => "Unknown Item"
            };
        }
    }
}
