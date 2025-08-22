using UnityEngine;
using UnityEditor;
using WegoSystem;

namespace WegoSystem.Editor
{
    [CustomEditor(typeof(ResolutionManager))]
    public class ResolutionManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector fields first
            DrawDefaultInspector();

            // Get a reference to the script we're inspecting
            ResolutionManager manager = (ResolutionManager)target;

            // Add some space for clarity
            EditorGUILayout.Space(10);

            // Create a nice, big button
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding = new RectOffset(10, 10, 8, 8);
            buttonStyle.fontSize = 12;

            if (GUILayout.Button("Apply Current Profile in Editor", buttonStyle))
            {
                // Call the public method on the manager script
                // This will execute the logic even outside of Play mode
                if (manager != null)
                {
                    manager.ApplyProfileInEditor();
                    Debug.Log("Applied resolution profile settings in the editor.");
                }
            }
            
            EditorGUILayout.HelpBox("Press this button to apply the 'Current Profile Index' settings to the Main Camera. This is useful for previewing changes without entering Play mode.", MessageType.Info);
        }
    }
}