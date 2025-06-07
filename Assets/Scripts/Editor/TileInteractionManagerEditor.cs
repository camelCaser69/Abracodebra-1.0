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
        
        EditorGUILayout.Space();
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
        
        EditorGUILayout.HelpBox("Order: First item in list gets highest sorting order value (" + 
                                manager.baseSortingOrder + "). Each subsequent item is " + 
                                (manager.baseSortingOrder - 1) + ", " + 
                                (manager.baseSortingOrder - 2) + ", etc.", MessageType.Info);
    }
}
#endif