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

        // New ones.
        SerializedProperty leafPatternProp      = property.FindPropertyRelative("leafPattern");
        SerializedProperty growthRandomnessProp = property.FindPropertyRelative("growthRandomness");

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
            case NodeEffectType.Seed:
            {
                // Min stem length
                var line1 = NextLineRect();
                valueProp.floatValue = EditorGUI.FloatField(line1,
                    new GUIContent("Min Stem Length", ""), valueProp.floatValue);

                // Max stem length
                var line2 = NextLineRect();
                secondaryProp.floatValue = EditorGUI.FloatField(line2,
                    new GUIContent("Max Stem Length", ""), secondaryProp.floatValue);

                // Growth speed
                var line3 = NextLineRect();
                extra1Prop.floatValue = EditorGUI.FloatField(line3,
                    new GUIContent("Growth Speed (sec)", ""), extra1Prop.floatValue);

                // Leaf gap
                var line4 = NextLineRect();
                extra2Prop.floatValue = EditorGUI.FloatField(line4,
                    new GUIContent("Leaf Gap", ""), extra2Prop.floatValue);

                // Leaf Pattern => 0=Parallel, 1=Alternating
                var line5 = NextLineRect();
                leafPatternProp.intValue = EditorGUI.IntSlider(line5,
                    new GUIContent("Leaf Pattern", "(0=parallel,1=alt)"), leafPatternProp.intValue, 0, 1);

                // Growth Randomness => [0..2]
                var line6 = NextLineRect();
                growthRandomnessProp.floatValue = EditorGUI.Slider(line6,
                    new GUIContent("Growth Randomness", "0=straight, 0.5=random, 0.5=always diagonal"), growthRandomnessProp.floatValue, 0f, 1f);
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
                // +1 line
                totalHeight += (lineHeight + spacing);
                break;

            case NodeEffectType.Output:
                // +1 line
                totalHeight += (lineHeight + spacing);
                break;

            case NodeEffectType.Burning:
                // +2 lines
                totalHeight += (lineHeight + spacing) * 2;
                break;

            case NodeEffectType.Seed:
                // min length + max length + growth speed + leaf gap + leaf pattern + randomness => 6 lines
                totalHeight += (lineHeight + spacing) * 6;
                break;

            default:
                // fallback +1 line
                totalHeight += (lineHeight + spacing);
                break;
        }
        return totalHeight;
    }
}
