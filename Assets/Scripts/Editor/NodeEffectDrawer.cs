// Assets/Scripts/Editor/NodeEffectDrawer.cs
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(NodeEffectData))]
public class NodeEffectDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty effectTypeProp = property.FindPropertyRelative("effectType");
        SerializedProperty consumedOnTriggerProp = property.FindPropertyRelative("consumedOnTrigger");
        SerializedProperty seedDataProp = property.FindPropertyRelative("seedData");

        Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(currentRect, effectTypeProp);
        currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        NodeEffectType currentType = (NodeEffectType)effectTypeProp.enumValueIndex;

        // NEW: Show passive/active indicator
        EditorGUI.BeginDisabledGroup(true);
        bool isPassive = NodeEffectTypeHelper.IsPassiveEffect(currentType);
        string effectNatureLabel = isPassive ? "Passive" : "Active";
        EditorGUI.LabelField(currentRect, "Effect Type", effectNatureLabel);
        EditorGUI.EndDisabledGroup();
        currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // Only show consumedOnTrigger for trigger effects
        if (NodeEffectTypeHelper.IsTriggerEffect(currentType))
        {
            EditorGUI.PropertyField(currentRect, consumedOnTriggerProp, new GUIContent("Consumed on Trigger"));
            currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        // Conditional value fields
        if (currentType == NodeEffectType.SeedSpawn)
        {
            EditorGUI.PropertyField(currentRect, seedDataProp, true);
        }
        else
        {
            // Only show value fields if needed
            DrawStandardValueFields(currentRect, property);
        }

        EditorGUI.EndProperty();
    }

    void DrawStandardValueFields(Rect position, SerializedProperty property)
    {
        NodeEffectType currentType = (NodeEffectType)property.FindPropertyRelative("effectType").enumValueIndex;
        SerializedProperty primaryValueProp = property.FindPropertyRelative("primaryValue");
        SerializedProperty secondaryValueProp = property.FindPropertyRelative("secondaryValue");

        // Show primary value with appropriate label
        if (NodeEffectTypeHelper.RequiresPrimaryValue(currentType))
        {
            string primaryLabel = GetPrimaryValueLabel(currentType);
            EditorGUI.PropertyField(position, primaryValueProp, new GUIContent(primaryLabel));
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        // Show secondary value only when needed
        if (NodeEffectTypeHelper.RequiresSecondaryValue(currentType))
        {
            string secondaryLabel = GetSecondaryValueLabel(currentType);
            EditorGUI.PropertyField(position, secondaryValueProp, new GUIContent(secondaryLabel));
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2; // Type dropdown + Passive/Active label
        NodeEffectType currentType = (NodeEffectType)property.FindPropertyRelative("effectType").enumValueIndex;

        if (NodeEffectTypeHelper.IsTriggerEffect(currentType))
        {
            totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        if (currentType == NodeEffectType.SeedSpawn)
        {
            totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("seedData"), true);
        }
        else
        {
            if (NodeEffectTypeHelper.RequiresPrimaryValue(currentType))
            {
                totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            if (NodeEffectTypeHelper.RequiresSecondaryValue(currentType))
            {
                totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        return totalHeight + EditorGUIUtility.standardVerticalSpacing;
    }

    string GetPrimaryValueLabel(NodeEffectType type)
    {
        switch (type)
        {
            case NodeEffectType.EnergyStorage: return "Max Energy";
            case NodeEffectType.EnergyPerTick: return "Energy/Tick";
            case NodeEffectType.EnergyCost: return "Energy Cost";
            case NodeEffectType.StemLength: return "Min Length";
            case NodeEffectType.GrowthSpeed: return "Ticks/Stage";
            case NodeEffectType.LeafGap: return "Gap Size";
            case NodeEffectType.LeafPattern: return "Pattern ID";
            case NodeEffectType.StemRandomness: return "Wobble %";
            case NodeEffectType.PoopAbsorption: return "Radius";
            case NodeEffectType.Damage: return "Damage %";
            case NodeEffectType.TimerCast: return "Tick Interval";
            case NodeEffectType.ProximityCast: return "Range";
            case NodeEffectType.Cooldown: return "Cooldown Ticks";
            case NodeEffectType.CastDelay: return "Delay Ticks";
            case NodeEffectType.Nutritious: return "Hunger Restore";
            case NodeEffectType.ScentModifier: return "Radius Mod";
            default: return "Value";
        }
    }

    string GetSecondaryValueLabel(NodeEffectType type)
    {
        switch (type)
        {
            case NodeEffectType.StemLength: return "Max Length";
            case NodeEffectType.PoopAbsorption: return "Energy Gain";
            case NodeEffectType.ScentModifier: return "Strength Mod";
            default: return "Secondary";
        }
    }
}