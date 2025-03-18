using UnityEngine;
using UnityEditor;
using System.IO;

public static class NodeDefinitionCreator
{
    private const string MenuPath = "Assets/Create/Nodes/Node Definition (Auto-Named)";

    [MenuItem(MenuPath, false, 50)]
    public static void CreateNodeDefinition()
    {
        // Figure out which folder the user is in, or use Assets/
        string folder = GetSelectedPathOrFallback();

        // Determine the next numeric index for "Node_XXX_"
        int nextNumber = GetNextNodeNumber(folder);

        // e.g., "Node_007_"
        string newName = $"Node_{nextNumber:D3}_";
        string newPath = Path.Combine(folder, newName + ".asset").Replace("\\", "/");

        // Create the NodeDefinition
        NodeDefinition nodeDef = ScriptableObject.CreateInstance<NodeDefinition>();
        AssetDatabase.CreateAsset(nodeDef, newPath);
        
        // Save and refresh, but DO NOT select or focus the ProjectWindow.
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created NodeDefinition at: {newPath}");
        // Notice we don't call Selection.activeObject = nodeDef
        // or EditorUtility.FocusProjectWindow()
        // or ProjectWindowUtil.ShowCreatedAsset(nodeDef)
        // thus we avoid changing the user's folder.
    }

    // (Optional) Validate the menu item so it’s disabled if the game is running
    [MenuItem(MenuPath, true)]
    private static bool ValidateCreateNodeDefinition()
    {
        return !Application.isPlaying;
    }

    private static string GetSelectedPathOrFallback()
    {
        // Attempt to get the folder the user has selected in the Project window
        string path = "Assets";
        foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
        {
            path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path))
            {
                // If it's a file, get its parent folder
                if (File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                }
                break;
            }
        }
        return path;
    }

    private static int GetNextNodeNumber(string folderPath)
    {
        int nextNumber = 1;
        // Look for existing Node_XXX_ files
        string[] files = Directory.GetFiles(folderPath, "Node_*", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            string fName = Path.GetFileNameWithoutExtension(file);
            if (fName.StartsWith("Node_") && fName.Length >= 8)
            {
                // extract the 3-digit number from Node_000_
                string numberPart = fName.Substring(5, 3);
                if (int.TryParse(numberPart, out int num) && num >= nextNumber)
                {
                    nextNumber = num + 1;
                }
            }
        }
        return nextNumber;
    }
}
