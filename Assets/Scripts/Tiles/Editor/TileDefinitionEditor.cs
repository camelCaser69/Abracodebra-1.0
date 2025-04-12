#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TileDefinition))]
public class TileDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        TileDefinition tileDefinition = (TileDefinition)target;
        
        EditorGUILayout.Space();
        if (GUILayout.Button("UPDATE COLOR IN SCENE", GUILayout.Height(30)))
        {
            tileDefinition.UpdateColor();
        }
    }
}
#endif