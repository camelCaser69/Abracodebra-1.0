using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(NodeEffectData))]
public class NodeEffectDrawer : PropertyDrawer
{
    // Define a fixed height for the standard fields plus some padding.
    // This will be added to the height of any complex fields.
    private const float BASE_PROPERTY_HEIGHT = 42f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get all property references
        var effectTypeProp = property.FindPropertyRelative("effectType");
        var isPassiveProp = property.FindPropertyRelative("isPassive");
        var seedDataProp = property.FindPropertyRelative("seedData");

        // --- Draw the standard fields ---
        Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(currentRect, effectTypeProp);
        currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.PropertyField(currentRect, isPassiveProp);
        currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // --- Draw the context-sensitive fields ---
        NodeEffectType currentType = (NodeEffectType)effectTypeProp.enumValueIndex;

        if (currentType == NodeEffectType.SeedSpawn)
        {
            // Let Unity draw the SeedSpawnData fields automatically. This is robust and respects layout.
            EditorGUI.PropertyField(currentRect, seedDataProp, true);
        }
        else
        {
            // For other types, draw the primary/secondary values as before
            DrawStandardValueFields(currentRect, property, currentType);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Start with the height of the two standard fields (EffectType, IsPassive).
        float totalHeight = BASE_PROPERTY_HEIGHT;

        NodeEffectType currentType = (NodeEffectType)property.FindPropertyRelative("effectType").enumValueIndex;

        if (currentType == NodeEffectType.SeedSpawn)
        {
            // Get the automatic height for the entire SeedSpawnData property.
            totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("seedData"), true);
        }
        else
        {
            // Calculate height for standard primary/secondary value fields.
            // This logic remains the same as it was working correctly.
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
    
    // Helper method to keep the OnGUI method cleaner
    private void DrawStandardValueFields(Rect startRect, SerializedProperty property, NodeEffectType currentType)
    {
        var primaryValueProp = property.FindPropertyRelative("primaryValue");
        var secondaryValueProp = property.FindPropertyRelative("secondaryValue");
        var scentDefRefProp = property.FindPropertyRelative("scentDefinitionReference");
        
        GUIContent primaryLabel = new GUIContent("Primary Value");
        GUIContent secondaryLabel = new GUIContent("Secondary Value");
        bool showSecondary = false;
        bool showScentField = false;

        // Label customization logic (same as before)
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