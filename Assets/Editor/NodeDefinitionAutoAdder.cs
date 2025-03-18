using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class NodeDefinitionAutoAdder : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            NodeDefinition nodeDef = AssetDatabase.LoadAssetAtPath<NodeDefinition>(assetPath);
            if (nodeDef != null)
            {
                string folderPath = Path.GetDirectoryName(assetPath);
                // Find an existing NodeDefinitionLibrary in the same folder.
                string[] libraryFiles = Directory.GetFiles(folderPath, "*.asset", SearchOption.TopDirectoryOnly)
                    .Where(f => f.Contains("NodeDefinitionLibrary")).ToArray();
                if (libraryFiles.Length > 0)
                {
                    NodeDefinitionLibrary library = AssetDatabase.LoadAssetAtPath<NodeDefinitionLibrary>(libraryFiles[0]);
                    if (library != null && !library.definitions.Contains(nodeDef))
                    {
                        library.definitions.Add(nodeDef);
                        EditorUtility.SetDirty(library);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }
    }
}