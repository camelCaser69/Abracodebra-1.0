using UnityEngine;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.UI.Toolkit {
    public class UIInventoryItem {
        public Sprite Icon { get; set; }
        public int StackSize { get; set; } = 1;
        public object OriginalData { get; }

        public PlantGeneRuntimeState SeedRuntimeState { get; set; }

        public ItemInstance ResourceInstance { get; }

        public Color BackgroundColor { get; set; } = new Color(0, 0, 0, 0);

        public string CustomName { get; set; } = "";

        public UIInventoryItem(SeedTemplate seed) {
            OriginalData = seed;
            Icon = seed?.icon;
            SeedRuntimeState = seed?.CreateRuntimeState();
        }

        public UIInventoryItem(ToolDefinition tool) {
            OriginalData = tool;
            Icon = tool?.icon;
        }

        public UIInventoryItem(GeneBase gene) {
            OriginalData = gene;
            Icon = gene?.icon;
        }

        public UIInventoryItem(ItemInstance itemInstance) {
            OriginalData = itemInstance?.definition;
            ResourceInstance = itemInstance;
            Icon = itemInstance?.definition?.icon;
            StackSize = itemInstance?.stackCount ?? 1;
        }

        public UIInventoryItem(object data) {
            OriginalData = data;

            if (data is SeedTemplate seed) {
                Icon = seed.icon;
                SeedRuntimeState = seed.CreateRuntimeState();
            }
            else if (data is ToolDefinition tool) {
                Icon = tool.icon;
            }
            else if (data is GeneBase gene) {
                Icon = gene.icon;
            }
            else if (data is ItemInstance item) {
                Icon = item.definition?.icon;
                StackSize = item.stackCount;
            }
            else if (data is ItemDefinition itemDef) {
                Icon = itemDef.icon;
            }
        }

        public bool HasCustomColor() {
            return BackgroundColor.a > 0.01f;
        }

        public bool IsConsumable() {
            if (ResourceInstance != null)
                return ResourceInstance.definition?.isConsumable ?? false;
            if (OriginalData is SeedTemplate)
                return true;
            return false;
        }
        
        /// <summary>
        /// Determines if this item should show a counter in the UI
        /// </summary>
        public bool ShouldShowCounter() {
            // Seeds always show count (they're consumable/stackable)
            if (OriginalData is SeedTemplate) {
                return true;
            }
            
            // Tools with limited uses show remaining uses
            if (OriginalData is ToolDefinition tool && tool.limitedUses) {
                return true;
            }
            
            // Stackable resources show stack count
            if (StackSize > 1) {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets the display count for this item
        /// For tools: remaining uses from ToolSwitcher
        /// For seeds/resources: stack size
        /// </summary>
        public int GetDisplayCount() {
            if (OriginalData is ToolDefinition tool && tool.limitedUses) {
                // Get remaining uses from ToolSwitcher
                if (ToolSwitcher.Instance != null && ToolSwitcher.Instance.CurrentTool == tool) {
                    return ToolSwitcher.Instance.CurrentRemainingUses;
                }
                // If not the current tool, get from the dictionary
                return GetToolUsesFromSwitcher(tool);
            }
            
            return StackSize;
        }
        
        /// <summary>
        /// Get tool uses from ToolSwitcher's internal tracking
        /// </summary>
        int GetToolUsesFromSwitcher(ToolDefinition tool) {
            if (ToolSwitcher.Instance == null || tool == null) {
                return tool?.initialUses ?? 0;
            }
            
            // ToolSwitcher tracks uses in its internal dictionary
            // If this tool is the current tool, return CurrentRemainingUses
            if (ToolSwitcher.Instance.CurrentTool == tool) {
                return ToolSwitcher.Instance.CurrentRemainingUses;
            }
            
            // Otherwise return initial uses (ToolSwitcher doesn't expose the dictionary directly)
            // In practice, the hotbar typically shows the currently selected tool
            return tool.initialUses;
        }

        public string GetDisplayName() {
            if (!string.IsNullOrEmpty(CustomName)) {
                return CustomName;
            }

            return OriginalData switch {
                SeedTemplate seed => seed.templateName,
                ToolDefinition tool => tool.displayName,
                GeneBase gene => gene.geneName,
                ItemDefinition itemDef => itemDef.itemName,
                _ => "Unknown Item"
            };
        }

        public string GetDescription() {
            return OriginalData switch {
                SeedTemplate seed => seed.description,
                ToolDefinition tool => tool.GetTooltipDescription(),
                GeneBase gene => gene.description,
                ItemDefinition itemDef => itemDef.description,
                _ => ""
            };
        }
    }
}
