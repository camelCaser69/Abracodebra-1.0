// Assets/Scripts/WorldInteraction/Player/HarvestedItem.cs

using System.Linq;
using WegoSystem;

public class HarvestedItem
{
    public NodeData HarvestedNodeData { get; set; }

    public HarvestedItem(NodeData data)
    {
        HarvestedNodeData = data;
    }

    public float GetNutritionValue()
    {
        if (HarvestedNodeData?.effects == null) return 0f;

        return HarvestedNodeData.effects
            .Where(e => e.effectType == NodeEffectType.Nutritious)
            .Sum(e => e.primaryValue);
    }

    public bool IsConsumable()
    {
        return GetNutritionValue() > 0;
    }
}