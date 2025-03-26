using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(NodeEffectData))]
public class NodeEffectDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty typeProp        = property.FindPropertyRelative("effectType");
        SerializedProperty valueProp       = property.FindPropertyRelative("effectValue");
        SerializedProperty secondaryProp   = property.FindPropertyRelative("secondaryValue");
        SerializedProperty extra1Prop      = property.FindPropertyRelative("extra1");
        SerializedProperty extra2Prop      = property.FindPropertyRelative("extra2");

        // Draw effectType
        Rect typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(typeRect, typeProp, new GUIContent("Effect Type"));
        NodeEffectType effectType = (NodeEffectType)typeProp.enumValueIndex;
        float yOffset = typeRect.yMax + EditorGUIUtility.standardVerticalSpacing;

        Rect NextLineRect()
        {
            Rect r = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
            yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return r;
        }

        // Draw fields based on effectType
        switch (effectType)
        {
            case NodeEffectType.ManaCost:
            {
                GUIContent content = new GUIContent("Mana Cost", "Amount of mana required to process this node.");
                Rect line = NextLineRect();
                valueProp.floatValue = EditorGUI.FloatField(line, content, valueProp.floatValue);
                break;
            }
            case NodeEffectType.Damage:
            {
                GUIContent content = new GUIContent("Damage", "Damage value contributed by this node.");
                Rect line = NextLineRect();
                valueProp.floatValue = EditorGUI.FloatField(line, content, valueProp.floatValue);
                break;
            }
            case NodeEffectType.Output:
            {
                GUIContent content = new GUIContent("Output", "This node outputs the final chain result (no parameters).");
                Rect line = NextLineRect();
                EditorGUI.LabelField(line, content);
                break;
            }
            case NodeEffectType.Burning:
            {
                // Fire DPS in value, Duration in secondaryValue
                GUIContent contentDps = new GUIContent("Fire DPS", "Damage per second of burning.");
                Rect line1 = NextLineRect();
                valueProp.floatValue = EditorGUI.FloatField(line1, contentDps, valueProp.floatValue);

                GUIContent contentDur = new GUIContent("Duration", "Duration (seconds) of burning effect.");
                Rect line2 = NextLineRect();
                secondaryProp.floatValue = EditorGUI.FloatField(line2, contentDur, secondaryProp.floatValue);
                break;
            }
            case NodeEffectType.AimSpread:
            {
                GUIContent content = new GUIContent("Aim Spread Modifier", "Modifier to add to the wizard's base aim spread.");
                Rect line = NextLineRect();
                valueProp.floatValue = EditorGUI.FloatField(line, content, valueProp.floatValue);
                break;
            }
            case NodeEffectType.Piercing:
            {
                GUIContent content = new GUIContent("Piercing", "Set to 1 for piercing, 0 otherwise.");
                Rect line = NextLineRect();
                valueProp.floatValue = EditorGUI.FloatField(line, content, valueProp.floatValue);
                break;
            }
            case NodeEffectType.FriendlyFire:
            {
                GUIContent content = new GUIContent("Friendly Fire", "Set to 1 for friendly fire, 0 for none.");
                Rect line = NextLineRect();
                valueProp.floatValue = EditorGUI.FloatField(line, content, valueProp.floatValue);
                break;
            }
            case NodeEffectType.SeedSpawn:
            {
                GUIContent content = new GUIContent("Seed Spawner", "Base effect required to spawn a plant. Add other plant effects to customize.");
                Rect line = NextLineRect();
                EditorGUI.LabelField(line, content);
                break;
            }
            case NodeEffectType.StemLength:
            {
                // Min stem length
                var line1 = NextLineRect();
                valueProp.floatValue = EditorGUI.FloatField(line1,
                    new GUIContent("Min Stem Length", "Minimum length of the stem in cells"), valueProp.floatValue);

                // Max stem length
                var line2 = NextLineRect();
                secondaryProp.floatValue = EditorGUI.FloatField(line2,
                    new GUIContent("Max Stem Length", "Maximum length of the stem in cells"), secondaryProp.floatValue);
                break;
            }
            case NodeEffectType.GrowthSpeed:
            {
                // Growth speed
                var line = NextLineRect();
                valueProp.floatValue = EditorGUI.FloatField(line,
                    new GUIContent("Growth Speed (sec)", "Seconds per growth step"), valueProp.floatValue);
                break;
            }
            case NodeEffectType.LeafGap:
            {
                // Leaf gap
                var line = NextLineRect();
                valueProp.floatValue = EditorGUI.FloatField(line,
                    new GUIContent("Leaf Gap", "0=leaves on every cell, 1=every 2nd cell, etc."), valueProp.floatValue);
                break;
            }
            // Update the LeafPattern case in the NodeEffectDataDrawer.cs OnGUI method
            case NodeEffectType.LeafPattern:
            {
                // Leaf Pattern as regular float field with improved tooltip
                var line = NextLineRect();
                valueProp.floatValue = EditorGUI.FloatField(line, 
                    new GUIContent("Leaf Pattern", 
                        "Choose a leaf pattern by number:\n" +
                        "0 = Parallel (leaves on both sides at same height)\n" +
                        "1 = Offset-Parallel (right side leaves always higher)\n" +
                        "2 = Alternating (L/R/R/L/L/R/R/L rotation pattern)\n" +
                        "3 = Double-Spiral (leaves spiral up the stem)\n" +
                        "4 = One-Sided (two leaves on right side only)"), 
                    valueProp.floatValue);
                break;
            }
            case NodeEffectType.StemRandomness:
            {
                // Growth Randomness => [0..1]
                var line = NextLineRect();
                valueProp.floatValue = EditorGUI.Slider(line,
                    new GUIContent("Growth Randomness", "0=straight up, 1=always diagonal"), valueProp.floatValue, 0f, 1f);
                break;
            }
            default:
            {
                // Fallback for unrecognized effect
                GUIContent content = new GUIContent("Value", "");
                Rect line = NextLineRect();
                valueProp.floatValue = EditorGUI.FloatField(line, content, valueProp.floatValue);
                break;
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty typeProp = property.FindPropertyRelative("effectType");
        NodeEffectType effectType = (NodeEffectType)typeProp.enumValueIndex;

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // Start with 1 line for the effectType + spacing
        float totalHeight = lineHeight + spacing;

        switch (effectType)
        {
            case NodeEffectType.ManaCost:
            case NodeEffectType.Damage:
            case NodeEffectType.AimSpread:
            case NodeEffectType.Piercing:
            case NodeEffectType.FriendlyFire:
            case NodeEffectType.GrowthSpeed:
            case NodeEffectType.LeafGap:
            case NodeEffectType.LeafPattern:
            case NodeEffectType.StemRandomness:
                // +1 line each
                totalHeight += (lineHeight + spacing);
                break;

            case NodeEffectType.Output:
            case NodeEffectType.SeedSpawn:
                // +1 line
                totalHeight += (lineHeight + spacing);
                break;

            case NodeEffectType.Burning:
                // +2 lines
                totalHeight += (lineHeight + spacing) * 2;
                break;

            case NodeEffectType.StemLength:
                // +2 lines (min and max)
                totalHeight += (lineHeight + spacing) * 2;
                break;

            default:
                // fallback +1 line
                totalHeight += (lineHeight + spacing);
                break;
        }
        return totalHeight;
    }
}