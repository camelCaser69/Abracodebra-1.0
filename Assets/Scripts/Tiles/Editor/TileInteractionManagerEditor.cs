#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TileInteractionManager))]
public class TileInteractionManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        TileInteractionManager manager = (TileInteractionManager)target;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Tilemap Management", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("UPDATE SORTING ORDER", GUILayout.Height(30)))
        {
            manager.UpdateSortingOrder();
            EditorUtility.SetDirty(manager);
        }
        
        if (GUILayout.Button("UPDATE ALL COLORS", GUILayout.Height(30)))
        {
            manager.UpdateAllColors();
            EditorUtility.SetDirty(manager);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Add a button to update all overlays
        if (GUILayout.Button("UPDATE ALL OVERLAYS", GUILayout.Height(30)))
        {
            manager.UpdateAllOverlays();
            EditorUtility.SetDirty(manager);
        }
        
        EditorGUILayout.EndVertical();
        
        // Show helpful info about sorting order
        EditorGUILayout.HelpBox("Sorting Order: First item in list gets highest sorting order value (" + 
                                manager.baseSortingOrder + "). Each subsequent item is " + 
                                (manager.baseSortingOrder - 1) + ", " + 
                                (manager.baseSortingOrder - 2) + ", etc.", MessageType.Info);
        
        // Show helpful info about overlay shader
        if (manager.overlayShader == null)
        {
            EditorGUILayout.HelpBox("Please assign the TilemapOverlay shader in the 'Overlay Shader' field to enable texture overlays.", MessageType.Warning);
        }
    }
}
#endif