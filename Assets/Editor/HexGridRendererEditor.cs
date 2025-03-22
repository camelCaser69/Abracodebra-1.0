using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HexGridRenderer))]
public class HexGridRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("UPDATE"))
        {
            HexGridRenderer grid = (HexGridRenderer)target;
            grid.SetVerticesDirty();
        }
    }
}