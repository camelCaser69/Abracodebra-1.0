using UnityEditor;
using UnityEngine;
using WegoSystem;

[CustomPropertyDrawer(typeof(NodeEffectData))]
public class NodeEffectDrawer : PropertyDrawer
{
    // Adjusted base height to account for the new "Consumed on Trigger" field potentially showing
    private const float BASE_PROPERTY_HEIGHT = 58f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var effectTypeProp = property.FindPropertyRelative("effectType");
        var isPassiveProp = property.FindPropertyRelative("isPassive");
        var consumedOnTriggerProp = property.FindPropertyRelative("consumedOnTrigger");
        var seedDataProp = property.FindPropertyRelative("seedData");

        Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(currentRect, effectTypeProp);
        currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        EditorGUI.PropertyField(currentRect, isPassiveProp);
        currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        NodeEffectType currentType = (NodeEffectType)effectTypeProp.enumValueIndex;

        // Only show "Consumed on Trigger" for cast types
        if (IsCastType(currentType))
        {
            EditorGUI.PropertyField(currentRect, consumedOnTriggerProp, new GUIContent("Consumed on Trigger"));
            currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        if (currentType == NodeEffectType.SeedSpawn)
        {
            EditorGUI.PropertyField(currentRect, seedDataProp, true);
        }
        else
        {
            DrawStandardValueFields(currentRect, property, currentType);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = BASE_PROPERTY_HEIGHT;

        NodeEffectType currentType = (NodeEffectType)property.FindPropertyRelative("effectType").enumValueIndex;

        if (IsCastType(currentType))
        {
            // Add height for the "Consumed on Trigger" checkbox
            totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        if (currentType == NodeEffectType.SeedSpawn)
        {
            totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("seedData"), true);
        }
        else
        {
            totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Primary value is always shown

            switch (currentType)
            {
                case NodeEffectType.StemLength:
                case NodeEffectType.PoopAbsorption:
                case NodeEffectType.ScentModifier:
                    totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // For secondary value
                    break;
            }

            if (currentType == NodeEffectType.ScentModifier)
            {
                totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // For scent reference
            }
        }

        return totalHeight;
    }

    private bool IsCastType(NodeEffectType type)
    {
        return type == NodeEffectType.TimerCast ||
               type == NodeEffectType.ProximityCast ||
               type == NodeEffectType.EatCast ||
               type == NodeEffectType.LeafLossCast;
    }

    private void DrawStandardValueFields(Rect startRect, SerializedProperty property, NodeEffectType currentType)
    {
        var primaryValueProp = property.FindPropertyRelative("primaryValue");
        var secondaryValueProp = property.FindPropertyRelative("secondaryValue");
        var scentDefRefProp = property.FindPropertyRelative("scentDefinitionReference");

        GUIContent primaryLabel = new GUIContent("Primary Value");
        GUIContent secondaryLabel = new GUIContent("Secondary Value");
        bool showSecondary = false;
        bool showScentField = false;

        switch (currentType)
        {
            case NodeEffectType.EnergyStorage: primaryLabel.text = "Max Energy Increase"; break;
            case NodeEffectType.EnergyPerTick: primaryLabel.text = "Energy Per Tick"; break;
            case NodeEffectType.EnergyCost: primaryLabel.text = "Energy Cost"; break;
            case NodeEffectType.StemLength: primaryLabel.text = "Min Segments Add"; secondaryLabel.text = "Max Segments Add"; showSecondary = true; break;
            case NodeEffectType.GrowthSpeed: primaryLabel.text = "Ticks Per Stage"; break;
            case NodeEffectType.LeafGap: primaryLabel.text = "Segments Between Leaves"; break;
            case NodeEffectType.LeafPattern: primaryLabel.text = "Pattern Type"; break;
            case NodeEffectType.StemRandomness: primaryLabel.text = "Wobble Chance (0-1)"; break;
            case NodeEffectType.Cooldown: primaryLabel.text = "Cooldown Ticks"; break;
            case NodeEffectType.CastDelay: primaryLabel.text = "Delay Ticks"; break;
            case NodeEffectType.PoopAbsorption: primaryLabel.text = "Detection Radius"; secondaryLabel.text = "Energy Per Poop"; showSecondary = true; break;
            case NodeEffectType.Damage: primaryLabel.text = "Damage Multiplier Add"; break;
            case NodeEffectType.GrowBerry: primaryLabel.text = "Enabled"; break;
            case NodeEffectType.ScentModifier: primaryLabel.text = "Radius Modifier"; secondaryLabel.text = "Strength Modifier"; showSecondary = true; showScentField = true; break;

            // Spellcrafting Types
            case NodeEffectType.TimerCast: primaryLabel.text = "Tick Interval"; break;
            case NodeEffectType.ProximityCast: primaryLabel.text = "Detection Range (Tiles)"; break;
            case NodeEffectType.Nutritious: primaryLabel.text = "Hunger Restored"; break; // <<< NEW
            // EatCast and LeafLossCast don't have special values for now
        }

        Rect currentRect = startRect;

        EditorGUI.PropertyField(currentRect, primaryValueProp, primaryLabel);
        currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        if (showSecondary)
        {
            EditorGUI.PropertyField(currentRect, secondaryValueProp, secondaryLabel);
            currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        if (showScentField)
        {
            EditorGUI.PropertyField(currentRect, scentDefRefProp, new GUIContent("Scent Definition"));
        }
    }
}