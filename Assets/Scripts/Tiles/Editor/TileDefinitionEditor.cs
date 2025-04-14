#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TileDefinition))]
public class TileDefinitionEditor : Editor
{
    private SerializedProperty overlaysProperty;
    private bool showOverlays = true;

    private void OnEnable()
    {
        overlaysProperty = serializedObject.FindProperty("overlays");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Texture Overlay Actions", EditorStyles.boldLabel);
        
        TileDefinition tileDefinition = (TileDefinition)target;
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("UPDATE COLOR IN SCENE", GUILayout.Height(30)))
        {
            tileDefinition.UpdateColor();
        }
        
        if (GUILayout.Button("UPDATE OVERLAYS IN SCENE", GUILayout.Height(30)))
        {
            tileDefinition.UpdateOverlays();
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Show helpful information
        if (tileDefinition.overlays != null && tileDefinition.overlays.Length > 0)
        {
            EditorGUILayout.HelpBox(
                "Overlay textures will be applied in order (first overlay at bottom, last at top)." +
                "\n\nIf you don't see your changes, press the UPDATE OVERLAYS button above.", 
                MessageType.Info);
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif