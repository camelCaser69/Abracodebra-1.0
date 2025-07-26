using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(NodeEffectData))]
public class NodeEffectDrawer : PropertyDrawer
{
    const float BASE_PROPERTY_HEIGHT = 65f;

    bool IsCastType(NodeEffectType type)
    {
        return type == NodeEffectType.TimerCast ||
               type == NodeEffectType.ProximityCast ||
               type == NodeEffectType.EatCast ||
               type == NodeEffectType.LeafLossCast;
    }

    void DrawStandardValueFields(Rect position, SerializedProperty property, NodeEffectType currentType)
    {
        SerializedProperty primaryValueProp = property.FindPropertyRelative("primaryValue");
        SerializedProperty secondaryValueProp = property.FindPropertyRelative("secondaryValue");

        EditorGUI.PropertyField(position, primaryValueProp, new GUIContent("Primary Value"));
        position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        switch (currentType)
        {
            case NodeEffectType.StemLength:
            case NodeEffectType.PoopAbsorption:
            case NodeEffectType.ScentModifier:
                EditorGUI.PropertyField(position, secondaryValueProp, new GUIContent("Secondary Value"));
                break;
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty effectTypeProp = property.FindPropertyRelative("effectType");
        SerializedProperty isPassiveProp = property.FindPropertyRelative("isPassive");
        SerializedProperty consumedOnTriggerProp = property.FindPropertyRelative("consumedOnTrigger");
        SerializedProperty seedDataProp = property.FindPropertyRelative("seedData");

        Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(currentRect, effectTypeProp);
        currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        EditorGUI.PropertyField(currentRect, isPassiveProp);
        currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        NodeEffectType currentType = (NodeEffectType)effectTypeProp.enumValueIndex;

        if (IsCastType(currentType))
        {
            EditorGUI.PropertyField(currentRect, consumedOnTriggerProp, new GUIContent("Consumed on Trigger"));
            currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        // REMOVED: GrowBerry nodeDefinitionReference field - no longer needed!

        // REMOVED: Special handling for ScentModifier - no longer needed
        
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
            totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        // REMOVED: GrowBerry height calculation - no longer needed!

        if (currentType == NodeEffectType.SeedSpawn)
        {
            totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("seedData"), true);
        }
        else
        {
            totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            switch (currentType)
            {
                case NodeEffectType.StemLength:
                case NodeEffectType.PoopAbsorption:
                case NodeEffectType.ScentModifier:
                    totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    break;
            }
        }

        return totalHeight;
    }
}