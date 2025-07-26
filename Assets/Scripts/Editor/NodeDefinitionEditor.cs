// Assets/Scripts/Editor/NodeDefinitionEditor.cs
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NodeDefinition))]
public class NodeDefinitionEditor : Editor
{
    // Properties to draw manually
    SerializedProperty activationTypeProp;
    SerializedProperty displayNameProp;
    SerializedProperty descriptionProp;
    SerializedProperty thumbnailProp;
    SerializedProperty thumbnailTintColorProp;
    SerializedProperty backgroundColorProp;
    SerializedProperty nodeViewPrefabProp;
    SerializedProperty effectsProp;

    void OnEnable()
    {
        // Cache serialized properties
        activationTypeProp = serializedObject.FindProperty("activationType");
        displayNameProp = serializedObject.FindProperty("displayName");
        descriptionProp = serializedObject.FindProperty("description");
        thumbnailProp = serializedObject.FindProperty("thumbnail");
        thumbnailTintColorProp = serializedObject.FindProperty("thumbnailTintColor");
        backgroundColorProp = serializedObject.FindProperty("backgroundColor");
        nodeViewPrefabProp = serializedObject.FindProperty("nodeViewPrefab");
        effectsProp = serializedObject.FindProperty("effects");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // --- Custom Activation Type Selector ---
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gene Activation Type", EditorStyles.boldLabel);

        // Record changes for undo functionality
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(activationTypeProp, new GUIContent("Activation Type"));
        if (EditorGUI.EndChangeCheck())
        {
            // This block executes if the enum value was changed by the user
            serializedObject.ApplyModifiedProperties();
        }

        // Show a help box explaining what the selected type does
        ShowActivationTypeHelp((GeneActivationType)activationTypeProp.enumValueIndex);
        EditorGUILayout.Space();

        // --- Draw the rest of the properties manually ---
        EditorGUILayout.LabelField("Display Properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(displayNameProp);
        EditorGUILayout.PropertyField(descriptionProp);
        EditorGUILayout.PropertyField(thumbnailProp);
        EditorGUILayout.PropertyField(thumbnailTintColorProp);
        EditorGUILayout.PropertyField(backgroundColorProp);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("System Properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(nodeViewPrefabProp);

        EditorGUILayout.Space();
        
        // Use the custom property drawer for effects
        EditorGUILayout.PropertyField(effectsProp, new GUIContent("Node Effects"), true);


        serializedObject.ApplyModifiedProperties();
    }

    private void ShowActivationTypeHelp(GeneActivationType type)
    {
        string helpText = type switch
        {
            GeneActivationType.Passive => "Passive: This gene is always active. Its effects are calculated once when the plant is created to determine its base stats (like growth speed, max energy, etc.).",
            GeneActivationType.Active => "Active: This gene executes during the plant's mature cooldown cycle and consumes energy. It can perform an action itself, or act as a Trigger for a subsequent Payload gene.",
            GeneActivationType.Payload => "Payload: This gene does nothing on its own. It is only activated when a preceding Active/Trigger gene in the sequence executes.",
            _ => "Unknown activation type."
        };

        EditorGUILayout.HelpBox(helpText, MessageType.Info);
    }
}