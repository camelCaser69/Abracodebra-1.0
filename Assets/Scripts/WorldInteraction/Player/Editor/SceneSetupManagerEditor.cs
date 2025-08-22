// FILE: Assets/Scripts/Editor/SceneSetupManagerEditor.cs
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SceneSetupManager))]
public class SceneSetupManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SceneSetupManager setupManager = (SceneSetupManager)target;

        EditorGUILayout.Space(10);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.padding = new RectOffset(10, 10, 10, 10);
        buttonStyle.fontSize = 13;
        
        if (GUILayout.Button("Setup Scene Now (Move Player & Camera)", buttonStyle))
        {
            setupManager.SetupScene();
        }
    }
}