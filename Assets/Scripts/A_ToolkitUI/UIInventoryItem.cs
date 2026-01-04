// File: Assets/Scripts/A_ToolkitUI/UIInventoryItem.cs
using UnityEngine;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.UI.Toolkit
{
    /// <summary>
    /// Unified inventory item wrapper for all item types.
    /// Replaces legacy InventoryBarItem class.
    /// </summary>
    public class UIInventoryItem
    {
        public enum ItemType { Gene, Seed, Tool, Resource }

        // Core properties
        public Sprite Icon { get; set; }
        public int StackSize { get; set; } = 1;
        public object OriginalData { get; }
        public Color BackgroundColor { get; set; } = new Color(0, 0, 0, 0);
        public string CustomName { get; set; } = "";

        // Type-specific data
        public PlantGeneRuntimeState SeedRuntimeState { get; set; }
        public ItemInstance ResourceInstance { get; }
        public RuntimeGeneInstance GeneInstance { get; }

        /// <summary>
        /// Returns the item type based on OriginalData.
        /// </summary>
        public ItemType Type
        {
            get
            {
                return OriginalData switch
                {
                    SeedTemplate => ItemType.Seed,
                    ToolDefinition => ItemType.Tool,
                    GeneBase => ItemType.Gene,
                    ItemDefinition => ItemType.Resource,
                    _ => ResourceInstance != null ? ItemType.Resource : ItemType.Gene
                };
            }
        }

        // Direct accessors for type-specific data
        public SeedTemplate SeedTemplate => OriginalData as SeedTemplate;
        public ToolDefinition ToolDefinition => OriginalData as ToolDefinition;
        public GeneBase Gene => OriginalData as GeneBase;
        public ItemDefinition ItemDefinition => OriginalData as ItemDefinition ?? ResourceInstance?.definition;

        #region Constructors

        public UIInventoryItem(SeedTemplate seed)
        {
            OriginalData = seed;
            Icon = seed?.icon;
            SeedRuntimeState = seed?.CreateRuntimeState();
        }

        public UIInventoryItem(ToolDefinition tool)
        {
            OriginalData = tool;
            Icon = tool?.icon;
        }

        public UIInventoryItem(GeneBase gene)
        {
            OriginalData = gene;
            Icon = gene?.icon;
            GeneInstance = gene != null ? new RuntimeGeneInstance(gene) : null;
        }

        public UIInventoryItem(RuntimeGeneInstance geneInstance)
        {
            GeneInstance = geneInstance;
            var gene = geneInstance?.GetGene();
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

            switch (data)
            {
                case SeedTemplate seed:
                    Icon = seed.icon;
                    SeedRuntimeState = seed.CreateRuntimeState();
                    break;
                case ToolDefinition tool:
                    Icon = tool.icon;
                    break;
                case GeneBase gene:
                    Icon = gene.icon;
                    GeneInstance = new RuntimeGeneInstance(gene);
                    break;
                case ItemInstance item:
                    Icon = item.definition?.icon;
                    StackSize = item.stackCount;
                    break;
                case ItemDefinition itemDef:
                    Icon = itemDef.icon;
                    break;
            }
        }

        #endregion

        #region Static Factory Methods

        public static UIInventoryItem FromSeed(SeedTemplate seed)
        {
            return seed != null ? new UIInventoryItem(seed) : null;
        }

        public static UIInventoryItem FromTool(ToolDefinition tool)
        {
            return tool != null ? new UIInventoryItem(tool) : null;
        }

        public static UIInventoryItem FromGene(GeneBase gene)
        {
            return gene != null ? new UIInventoryItem(gene) : null;
        }

        public static UIInventoryItem FromGene(RuntimeGeneInstance instance)
        {
            return instance != null ? new UIInventoryItem(instance) : null;
        }

        public static UIInventoryItem FromItem(ItemInstance instance)
        {
            return instance?.definition != null ? new UIInventoryItem(instance) : null;
        }

        #endregion

        #region Validation & Display

        public bool IsValid()
        {
            return Type switch
            {
                ItemType.Seed => SeedTemplate != null && SeedRuntimeState != null,
                ItemType.Tool => ToolDefinition != null,
                ItemType.Gene => Gene != null || GeneInstance?.GetGene() != null,
                ItemType.Resource => ResourceInstance?.definition != null || ItemDefinition != null,
                _ => false
            };
        }

        public bool HasCustomColor()
        {
            return BackgroundColor.a > 0.01f;
        }

        public bool IsConsumable()
        {
            if (ResourceInstance != null)
                return ResourceInstance.definition?.isConsumable ?? false;
            if (OriginalData is SeedTemplate)
                return true;
            return false;
        }

        public bool ShouldShowCounter()
        {
            if (OriginalData is SeedTemplate)
                return true;

            if (OriginalData is ToolDefinition tool && tool.limitedUses)
                return true;

            if (StackSize > 1)
                return true;

            return false;
        }

        public int GetDisplayCount()
        {
            if (OriginalData is ToolDefinition tool && tool.limitedUses)
            {
                if (ToolSwitcher.Instance != null && ToolSwitcher.Instance.CurrentTool == tool)
                    return ToolSwitcher.Instance.CurrentRemainingUses;

                return tool.initialUses;
            }

            return StackSize;
        }

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(CustomName))
                return CustomName;

            return OriginalData switch
            {
                SeedTemplate seed => seed.templateName,
                ToolDefinition tool => tool.displayName,
                GeneBase gene => gene.geneName,
                ItemDefinition itemDef => itemDef.itemName,
                _ => GeneInstance?.GetGene()?.geneName ?? "Unknown Item"
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
                _ => GeneInstance?.GetGene()?.description ?? ""
            };
        }

        public Sprite GetIcon()
        {
            return Icon;
        }

        public string GetTypeDisplayString()
        {
            return Type switch
            {
                ItemType.Seed => "Seed",
                ItemType.Tool => ToolDefinition != null ? $"Tool - {ToolDefinition.toolType}" : "Tool",
                ItemType.Gene => Gene != null ? $"Gene - {Gene.Category}" : "Gene",
                ItemType.Resource => "Resource",
                _ => "Unknown"
            };
        }

        #endregion
    }
}