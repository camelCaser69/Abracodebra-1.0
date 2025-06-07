using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[CustomEditor(typeof(NodeDefinitionLibrary))]
public class NodeDefinitionLibraryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("UPDATE"))
        {
            UpdateLibrary();
        }
    }

    private void UpdateLibrary()
    {
        NodeDefinitionLibrary library = (NodeDefinitionLibrary)target;

        // Get the folder of the current library asset.
        string libraryPath = AssetDatabase.GetAssetPath(library);
        string folderPath = Path.GetDirectoryName(libraryPath);

        // Find all .asset files in the folder.
        string[] assetFiles = Directory.GetFiles(folderPath, "*.asset", SearchOption.TopDirectoryOnly);

        // Load NodeDefinition assets from that folder.
        List<NodeDefinition> defs = new List<NodeDefinition>();
        foreach (string file in assetFiles)
        {
            // Ignore the library asset itself.
            if (file == libraryPath)
                continue;

            NodeDefinition def = AssetDatabase.LoadAssetAtPath<NodeDefinition>(file);
            if (def != null)
                defs.Add(def);
        }

        // Sort definitions by the numeric portion in the file name (assumes "Node_XXX_...").
        defs = defs.OrderBy(d => {
            string name = d.name;
            if (name.StartsWith("Node_") && name.Length >= 8)
            {
                string numStr = name.Substring(5, 3);
                if (int.TryParse(numStr, out int num))
                    return num;
            }
            return int.MaxValue; // if no number is found, sort last.
        }).ToList();

        library.definitions = defs;
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Debug.Log("NodeDefinitionLibrary updated. Total definitions: " + library.definitions.Count);
    }
}