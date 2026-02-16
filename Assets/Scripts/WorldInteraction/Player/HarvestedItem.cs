using UnityEngine;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Implementations;
using Abracodabra.Genes.Core;
using System.Collections.Generic;

public class HarvestedItem {
    public ItemInstance Item { get; set; }

    // ✅ MODIFIED: Constructor now accepts payload list
    public HarvestedItem(ItemDefinition definition, Dictionary<string, float> dynamicProps = null, List<RuntimeGeneInstance> payloads = null) {
        Item = new ItemInstance(definition, dynamicProps, payloads);
    }

    public float GetNutritionValue() {
        return Item.GetNutrition();
    }

    public bool IsConsumable() {
        return Item.definition != null && Item.definition.isConsumable;
    }
}