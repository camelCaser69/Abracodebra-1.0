// FILE: Assets/Scripts/Editor/ProceduralMapGeneratorEditor.cs
using UnityEngine;
using UnityEditor;
using WegoSystem.ProceduralGeneration;

namespace WegoSystem.ProceduralGeneration.Editor
{
    [CustomEditor(typeof(ProceduralMapGenerator))]
    public class ProceduralMapGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ProceduralMapGenerator generator = (ProceduralMapGenerator)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate Map"))
            {
                generator.GenerateMap();
            }

            if (GUILayout.Button("Clear Map"))
            {
                generator.ClearMap();
            }
        }
    }
}