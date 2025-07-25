using System.Linq;
using System.Collections.Generic;

public class HarvestedItem
{
    public NodeData HarvestedNodeData { get; private set; }

    public HarvestedItem(NodeData data)
    {
        HarvestedNodeData = data;
    }

    public float GetNutritionValue()
    {
        if (HarvestedNodeData?.effects == null) return 0f;

        // Sum up all nutritious effects on the item
        return HarvestedNodeData.effects
            .Where(e => e.effectType == NodeEffectType.Nutritious)
            .Sum(e => e.primaryValue);
    }
}