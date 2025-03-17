using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class NodeDefinitionPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, 
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            NodeDefinition nodeDef = AssetDatabase.LoadAssetAtPath<NodeDefinition>(assetPath);
            if (nodeDef != null)
            {
                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                // Check if file name already starts with "Node_"
                if (!fileName.StartsWith("Node_"))
                {
                    string folderPath = Path.GetDirectoryName(assetPath);
                    // Get all files in folder that start with "Node_"
                    var files = Directory.GetFiles(folderPath, "Node_*", SearchOption.TopDirectoryOnly);
                    int nextNumber = 1;
                    foreach (var file in files)
                    {
                        string fName = Path.GetFileNameWithoutExtension(file);
                        string[] parts = fName.Split('_');
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int num))
                        {
                            if (num >= nextNumber)
                                nextNumber = num + 1;
                        }
                    }
                    string newName = $"Node_{nextNumber:D3}_";
                    AssetDatabase.RenameAsset(assetPath, newName);
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}