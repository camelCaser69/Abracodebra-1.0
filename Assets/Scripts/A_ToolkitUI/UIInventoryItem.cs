using System;
using UnityEngine;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.UI.Toolkit
{
    public class UIInventoryItem
    {
        public Sprite Icon { get; set; }
        public int StackSize { get; set; } = 1;
        public object OriginalData { get; }

        public PlantGeneRuntimeState SeedRuntimeState { get; set; }

        public ItemInstance ResourceInstance { get; }

        public Color BackgroundColor { get; set; } = new Color(0, 0, 0, 0);

        public string CustomName { get; set; } = "";

        // Track remaining uses for tools with limited uses
        public int RemainingUses { get; set; } = -1; // -1 means unlimited

        public UIInventoryItem(SeedTemplate seed)
        {
            OriginalData = seed;
            Icon = seed?.icon;
            SeedRuntimeState = seed?.CreateRuntimeState();
            StackSize = 1; // Seeds start with 1 use
        }

        public UIInventoryItem(ToolDefinition tool)
        {
            OriginalData = tool;
            Icon = tool?.icon;
            
            // Initialize uses based on tool definition
            if (tool != null && tool.limitedUses)
            {
                RemainingUses = tool.initialUses;
            }
            else
            {
                RemainingUses = -1; // Unlimited
            }
        }

        public UIInventoryItem(GeneBase gene)
        {
            OriginalData = gene;
            Icon = gene?.icon;
        }

        public UIInventoryItem(ItemInstance itemInstance)
        {
            OriginalData = itemInstance?.definition;
            ResourceInstance = itemInstance;
            Icon = itemInstance?.definition?.icon;
            StackSize = itemInstance?.stackCount ?? 1;
        }

        public UIInventoryItem(object data)
        {
            OriginalData = data;

            if (data is SeedTemplate seed)
            {
                Icon = seed.icon;
                SeedRuntimeState = seed.CreateRuntimeState();
                StackSize = 1;
            }
            else if (data is ToolDefinition tool)
            {
                Icon = tool.icon;
                if (tool.limitedUses)
                {
                    RemainingUses = tool.initialUses;
                }
                else
                {
                    RemainingUses = -1;
                }
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

        public bool HasCustomColor()
        {
            return BackgroundColor.a > 0.01f;
        }

        public bool IsConsumable()
        {
            if (ResourceInstance != null)
                return ResourceInstance.definition?.isConsumable ?? false;
            if (OriginalData is ItemDefinition itemDef)
                return itemDef.isConsumable;
            return false;
        }

        public float GetNutrition()
        {
            if (ResourceInstance != null)
                return ResourceInstance.GetNutrition();
            if (OriginalData is ItemDefinition itemDef)
                return itemDef.baseNutrition;
            return 0f;
        }

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

        /// <summary>
        /// Returns whether this item should display a counter in the UI.
        /// </summary>
        public bool ShouldShowCounter()
        {
            // Seeds always show count (they're limited/consumable)
            if (OriginalData is SeedTemplate)
            {
                return true;
            }
            
            // Tools with limited uses show remaining uses
            if (OriginalData is ToolDefinition tool && tool.limitedUses)
            {
                return true;
            }
            
            // Resources/items show stack count if stackable (more than 1)
            if (ResourceInstance != null && StackSize > 1)
            {
                return true;
            }
            
            // Items with stack > 1
            if (StackSize > 1)
            {
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Returns the count to display in the UI (stack size, uses remaining, etc.)
        /// </summary>
        public int GetDisplayCount()
        {
            // Tools with limited uses show remaining uses
            if (OriginalData is ToolDefinition tool && tool.limitedUses)
            {
                return RemainingUses >= 0 ? RemainingUses : tool.initialUses;
            }
            
            // Everything else shows stack size
            return StackSize;
        }

        /// <summary>
        /// Consumes one use of the item. Returns true if consumed successfully, false if depleted.
        /// </summary>
        public bool ConsumeUse()
        {
            // Tools with limited uses
            if (OriginalData is ToolDefinition tool && tool.limitedUses)
            {
                if (RemainingUses > 0)
                {
                    RemainingUses--;
                    return true;
                }
                return false; // No uses left
            }
            
            // Seeds and stackable items decrease stack
            if (StackSize > 0)
            {
                StackSize--;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Returns true if this item is depleted (no uses/stacks remaining).
        /// </summary>
        public bool IsDepleted()
        {
            // Tools with limited uses
            if (OriginalData is ToolDefinition tool && tool.limitedUses)
            {
                return RemainingUses <= 0;
            }
            
            // Seeds and stackable items
            if (OriginalData is SeedTemplate || ResourceInstance != null)
            {
                return StackSize <= 0;
            }
            
            return false;
        }

        /// <summary>
        /// Refills tool uses to full capacity.
        /// </summary>
        public void RefillUses()
        {
            if (OriginalData is ToolDefinition tool && tool.limitedUses)
            {
                RemainingUses = tool.initialUses;
            }
        }
    }
}
