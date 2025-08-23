using UnityEngine;
using UnityEditor;
using WegoSystem;

// REMOVED: The problematic 'namespace WegoSystem.Editor' is gone.
// The script now resides in the global namespace, which is standard for editor scripts.

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
            if (manager != null)
            {
                manager.UpdateSortingOrder();
                EditorUtility.SetDirty(manager);
            }
        }

        if (GUILayout.Button("UPDATE ALL COLORS", GUILayout.Height(30)))
        {
            if (manager != null)
            {
                manager.UpdateAllColors();
                EditorUtility.SetDirty(manager);
            }
        }

        EditorGUILayout.EndHorizontal();

        if (manager != null)
        {
            EditorGUILayout.HelpBox("Order: First item in list gets highest sorting order value (" +
                                    manager.baseSortingOrder + "). Each subsequent item is " +
                                    (manager.baseSortingOrder - 1) + ", " +
                                    (manager.baseSortingOrder - 2) + ", etc.", MessageType.Info);
        }
    }
}