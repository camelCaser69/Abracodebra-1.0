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
    }
}
