using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(NodeEffectData))]
public class NodeEffectDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get properties
        var effectTypeProp = property.FindPropertyRelative("effectType");
        var primaryValueProp = property.FindPropertyRelative("primaryValue");
        var secondaryValueProp = property.FindPropertyRelative("secondaryValue");
        var isPassiveProp = property.FindPropertyRelative("isPassive");
        // var scentIdentifierProp = property.FindPropertyRelative("scentIdentifier"); // REMOVED
        var scentDefRefProp = property.FindPropertyRelative("scentDefinitionReference"); // ADDED

        // Calculate rects - basic vertical layout
        Rect typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        Rect passiveRect = new Rect(position.x, typeRect.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
        Rect primaryRect = new Rect(position.x, passiveRect.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
        Rect secondaryRect = new Rect(position.x, primaryRect.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
        Rect scentRect = new Rect(position.x, secondaryRect.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight); // Reuse rect position

        // Draw fields
        EditorGUI.PropertyField(typeRect, effectTypeProp);
        EditorGUI.PropertyField(passiveRect, isPassiveProp);

        // Determine labels based on type
        GUIContent primaryLabel = new GUIContent(primaryValueProp.displayName);
        GUIContent secondaryLabel = new GUIContent(secondaryValueProp.displayName);

        NodeEffectType currentType = (NodeEffectType)effectTypeProp.enumValueIndex;

        bool showScentField = false;
        switch (currentType)
        {
            case NodeEffectType.ScentModifier:
                primaryLabel.text = "Radius Bonus"; // Contextual label
                secondaryLabel.text = "Strength Bonus"; // Contextual label
                showScentField = true;
                break;
            case NodeEffectType.Damage:
                primaryLabel.text = "Damage Multiplier Add";
                break;
             // Add other cases...
            default:
                break;
        }

        EditorGUI.PropertyField(primaryRect, primaryValueProp, primaryLabel);
        EditorGUI.PropertyField(secondaryRect, secondaryValueProp, secondaryLabel);

        // Conditionally draw scent definition Object Field
        if (showScentField)
        {
            // Draw Object Field restricted to ScentDefinition type
            EditorGUI.ObjectField(scentRect, scentDefRefProp, typeof(ScentDefinition), new GUIContent("Scent Definition")); // Use correct label
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight; // Type
        height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight; // Passive
        height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight; // Primary
        height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight; // Secondary

        // Add height for scent field only if needed
        var effectTypeProp = property.FindPropertyRelative("effectType");
        NodeEffectType currentType = (NodeEffectType)effectTypeProp.enumValueIndex;
        if (currentType == NodeEffectType.ScentModifier)
        {
            height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight; // Scent Definition Reference
        }

        return height;
    }
}