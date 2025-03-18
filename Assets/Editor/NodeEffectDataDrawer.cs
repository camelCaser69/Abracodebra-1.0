using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(NodeEffectData))]
public class NodeEffectDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get sub-properties.
        SerializedProperty typeProp = property.FindPropertyRelative("effectType");
        SerializedProperty valueProp = property.FindPropertyRelative("effectValue");
        SerializedProperty secondaryProp = property.FindPropertyRelative("secondaryValue");

        // Draw the effect type popup.
        Rect typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(typeRect, typeProp, new GUIContent("Effect Type"));

        NodeEffectType effectType = (NodeEffectType)typeProp.enumValueIndex;
        float yOffset = typeRect.yMax + EditorGUIUtility.standardVerticalSpacing;

        // Helper method without default parameter.
        Rect NextLineRect(float height)
        {
            Rect r = new Rect(position.x, yOffset, position.width, height);
            yOffset += height + EditorGUIUtility.standardVerticalSpacing;
            return r;
        }

        // Prepare GUIContent for labels with tooltips.
        GUIContent content = new GUIContent();
        switch (effectType)
        {
            case NodeEffectType.ManaCost:
                content.text = "Mana Cost";
                content.tooltip = "Amount of mana required to process this node.";
                {
                    Rect line = NextLineRect(EditorGUIUtility.singleLineHeight);
                    valueProp.floatValue = EditorGUI.FloatField(line, content, valueProp.floatValue);
                }
                break;
            case NodeEffectType.Damage:
                content.text = "Damage";
                content.tooltip = "Damage value contributed by this node.";
                {
                    Rect line = NextLineRect(EditorGUIUtility.singleLineHeight);
                    valueProp.floatValue = EditorGUI.FloatField(line, content, valueProp.floatValue);
                }
                break;
            case NodeEffectType.Output:
                content.text = "Output";
                content.tooltip = "This node outputs the final chain result (no parameters).";
                {
                    Rect line = NextLineRect(EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(line, content);
                }
                break;
            case NodeEffectType.Burning:
                {
                    content.text = "Fire DPS";
                    content.tooltip = "Damage per second of burning.";
                    Rect line1 = NextLineRect(EditorGUIUtility.singleLineHeight);
                    valueProp.floatValue = EditorGUI.FloatField(line1, content, valueProp.floatValue);
                    
                    content.text = "Duration";
                    content.tooltip = "Duration (seconds) of burning effect.";
                    Rect line2 = NextLineRect(EditorGUIUtility.singleLineHeight);
                    secondaryProp.floatValue = EditorGUI.FloatField(line2, content, secondaryProp.floatValue);
                }
                break;
            case NodeEffectType.AimSpread:
                content.text = "Aim Spread Modifier";
                content.tooltip = "Modifier to add to the wizard's base aim spread.";
                {
                    Rect line = NextLineRect(EditorGUIUtility.singleLineHeight);
                    valueProp.floatValue = EditorGUI.FloatField(line, content, valueProp.floatValue);
                }
                break;
            case NodeEffectType.Piercing:
                content.text = "Piercing";
                content.tooltip = "Set to 1 for piercing (projectile will hit multiple enemies), 0 otherwise.";
                {
                    Rect line = NextLineRect(EditorGUIUtility.singleLineHeight);
                    valueProp.floatValue = EditorGUI.FloatField(line, content, valueProp.floatValue);
                }
                break;
            case NodeEffectType.FriendlyFire:
                content.text = "Friendly Fire";
                content.tooltip = "Set to 1 to enable friendly fire (damage allies), 0 to disable.";
                {
                    Rect line = NextLineRect(EditorGUIUtility.singleLineHeight);
                    valueProp.floatValue = EditorGUI.FloatField(line, content, valueProp.floatValue);
                }
                break;
            default:
                content.text = "Value";
                content.tooltip = "";
                {
                    Rect line = NextLineRect(EditorGUIUtility.singleLineHeight);
                    valueProp.floatValue = EditorGUI.FloatField(line, content, valueProp.floatValue);
                }
                break;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty typeProp = property.FindPropertyRelative("effectType");
        NodeEffectType effectType = (NodeEffectType)typeProp.enumValueIndex;

        float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        switch (effectType)
        {
            case NodeEffectType.Output:
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                break;
            case NodeEffectType.Burning:
                height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
                break;
            case NodeEffectType.ManaCost:
            case NodeEffectType.Damage:
            case NodeEffectType.AimSpread:
            case NodeEffectType.Piercing:
            case NodeEffectType.FriendlyFire:
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                break;
            default:
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                break;
        }
        return height;
    }
}
