using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NodeDefinition))]
public class NodeDefinitionEditor : Editor
{
    // Serialized properties for all the fields in NodeDefinition
    SerializedProperty displayName;
    SerializedProperty description;
    SerializedProperty thumbnail;
    SerializedProperty thumbnailTintColor;
    SerializedProperty backgroundColor;
    SerializedProperty nodeViewPrefab;
    SerializedProperty effects;

    private void OnEnable()
    {
        displayName = serializedObject.FindProperty("displayName");
        description = serializedObject.FindProperty("description");
        thumbnail = serializedObject.FindProperty("thumbnail");
        thumbnailTintColor = serializedObject.FindProperty("thumbnailTintColor");
        backgroundColor = serializedObject.FindProperty("backgroundColor");
        nodeViewPrefab = serializedObject.FindProperty("nodeViewPrefab");
        effects = serializedObject.FindProperty("effects");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // "Display" section
        EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(displayName);
        EditorGUILayout.PropertyField(description);
        EditorGUILayout.PropertyField(thumbnail);
        EditorGUILayout.PropertyField(thumbnailTintColor);
        EditorGUILayout.PropertyField(backgroundColor);

        EditorGUILayout.Space();

        // "Prefab & Effects" section
        EditorGUILayout.LabelField("Prefab & Effects", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(nodeViewPrefab);

        // Draw the effects field with a minimum height
        EditorGUILayout.PropertyField(effects, new GUIContent("Effects"), true, GUILayout.MinHeight(1300f));

        serializedObject.ApplyModifiedProperties();
    }
}