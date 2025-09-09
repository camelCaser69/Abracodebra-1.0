using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Implementations;
using Abracodabra.Genes.Core;

// Note: Ensure your ItemInstance and ItemDefinition classes are accessible here,
// either in the same namespace or with a 'using' statement.

public class HarvestedItem
{
    // MODIFIED: This class now wraps a fully constructed ItemInstance.
    public ItemInstance Item { get; private set; }

    public HarvestedItem(ItemDefinition definition, Dictionary<string, float> dynamicProps = null)
    {
        Item = new ItemInstance(definition, dynamicProps);
    }

    // This is now a convenience method that delegates to the ItemInstance.
    public float GetNutritionValue()
    {
        return Item.GetNutrition();
    }

    // This is now a convenience method that delegates to the ItemDefinition.
    public bool IsConsumable()
    {
        return Item.definition != null && Item.definition.isConsumable;
    }
}