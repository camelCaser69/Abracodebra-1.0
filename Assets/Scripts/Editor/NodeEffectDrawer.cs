using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(NodeEffectData))]
public class NodeEffectDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        var effectTypeProp = property.FindPropertyRelative("effectType");
        var primaryValueProp = property.FindPropertyRelative("primaryValue");
        var secondaryValueProp = property.FindPropertyRelative("secondaryValue");
        var isPassiveProp = property.FindPropertyRelative("isPassive");
        var scentDefRefProp = property.FindPropertyRelative("scentDefinitionReference");

        Rect typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        Rect passiveRect = new Rect(position.x, typeRect.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
        Rect primaryRect = new Rect(position.x, passiveRect.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
        Rect secondaryRect = new Rect(position.x, primaryRect.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
        Rect scentRect = new Rect(position.x, secondaryRect.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);

        EditorGUI.PropertyField(typeRect, effectTypeProp);
        EditorGUI.PropertyField(passiveRect, isPassiveProp);

        NodeEffectType currentType = (NodeEffectType)effectTypeProp.enumValueIndex;

        // Set contextual labels based on effect type
        GUIContent primaryLabel = new GUIContent("Primary Value");
        GUIContent secondaryLabel = new GUIContent("Secondary Value");
        bool showSecondary = false;
        bool showScentField = false;

        switch (currentType) {
            // Energy & Resources
            case NodeEffectType.EnergyStorage:
                primaryLabel.text = "Max Energy Increase";
                primaryLabel.tooltip = "Additional maximum energy capacity";
                break;

            case NodeEffectType.EnergyPerTick:
                primaryLabel.text = "Energy Per Tick";
                primaryLabel.tooltip = "Energy generated each tick (affected by sunlight and leaves)";
                break;

            case NodeEffectType.EnergyCost:
                primaryLabel.text = "Energy Cost";
                primaryLabel.tooltip = "Energy required to execute mature cycle";
                break;

            // Growth & Structure
            case NodeEffectType.StemLength:
                primaryLabel.text = "Min Segments Add";
                primaryLabel.tooltip = "Minimum stem segments to add";
                secondaryLabel.text = "Max Segments Add";
                secondaryLabel.tooltip = "Maximum stem segments to add (will randomly pick between min and max)";
                showSecondary = true;
                break;

            case NodeEffectType.GrowthSpeed:
                primaryLabel.text = "Ticks Per Stage";
                primaryLabel.tooltip = "Number of ticks between each growth stage (lower = faster)";
                break;

            case NodeEffectType.LeafGap:
                primaryLabel.text = "Segments Between Leaves";
                primaryLabel.tooltip = "0 = leaves every segment, 1 = leaves every 2 segments, etc.";
                break;

            case NodeEffectType.LeafPattern:
                primaryLabel.text = "Pattern Type";
                primaryLabel.tooltip = "0=Symmetrical, 1=Offset, 2=Alternating, 3=Spiral, 4=Dense";
                break;

            case NodeEffectType.StemRandomness:
                primaryLabel.text = "Wobble Chance (0-1)";
                primaryLabel.tooltip = "Probability that stem will grow sideways";
                break;

            // Timing
            case NodeEffectType.Cooldown:
                primaryLabel.text = "Cooldown Ticks";
                primaryLabel.tooltip = "Ticks between mature cycle activations";
                break;

            case NodeEffectType.CastDelay:
                primaryLabel.text = "Delay Ticks";
                primaryLabel.tooltip = "Ticks to wait before starting growth";
                break;

            // Environmental
            case NodeEffectType.PoopAbsorption:
                primaryLabel.text = "Detection Radius";
                primaryLabel.tooltip = "Radius in tiles to detect fertilizer";
                secondaryLabel.text = "Energy Per Poop";
                secondaryLabel.tooltip = "Energy gained when absorbing fertilizer";
                showSecondary = true;
                break;

            // Combat & Effects
            case NodeEffectType.Damage:
                primaryLabel.text = "Damage Multiplier Add";
                primaryLabel.tooltip = "Adds to damage multiplier (1.0 = +100% damage)";
                break;

            // Spawning
            case NodeEffectType.GrowBerry:
                primaryLabel.text = "Enabled";
                primaryLabel.tooltip = "Set to 1 to enable berry growth";
                break;

            case NodeEffectType.SeedSpawn:
                primaryLabel.text = "Enabled";
                primaryLabel.tooltip = "Set to 1 to make this a seed container";
                break;

            // Modifiers
            case NodeEffectType.ScentModifier:
                primaryLabel.text = "Radius Modifier";
                primaryLabel.tooltip = "Adds/subtracts from scent radius";
                secondaryLabel.text = "Strength Modifier";
                secondaryLabel.tooltip = "Adds/subtracts from scent strength";
                showSecondary = true;
                showScentField = true;
                break;
        }

        EditorGUI.PropertyField(primaryRect, primaryValueProp, primaryLabel);
        
        if (showSecondary) {
            EditorGUI.PropertyField(secondaryRect, secondaryValueProp, secondaryLabel);
        }

        if (showScentField) {
            EditorGUI.PropertyField(scentRect, scentDefRefProp, new GUIContent("Scent Definition", "Which scent type to modify"));
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        float height = EditorGUIUtility.singleLineHeight; // Type
        height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight; // Passive
        height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight; // Primary
        height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight; // Secondary

        var effectTypeProp = property.FindPropertyRelative("effectType");
        NodeEffectType currentType = (NodeEffectType)effectTypeProp.enumValueIndex;
        if (currentType == NodeEffectType.ScentModifier) {
            height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight; // Scent Definition Reference
        }

        return height;
    }
}