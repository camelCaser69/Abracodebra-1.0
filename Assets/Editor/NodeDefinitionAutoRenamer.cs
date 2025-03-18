using UnityEditor;
using System.IO;

public class NodeDefinitionAutoRenamer : AssetModificationProcessor
{
    static string OnWillCreateAsset(string path)
    {
        if (path.EndsWith(".asset"))
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.Contains("NodeDefinition"))
            {
                string folderPath = Path.GetDirectoryName(path);
                string[] files = Directory.GetFiles(folderPath, "Node_*", SearchOption.TopDirectoryOnly);
                int nextNumber = 1;
                foreach (var file in files)
                {
                    string fName = Path.GetFileNameWithoutExtension(file);
                    if (fName.StartsWith("Node_"))
                    {
                        string numberPart = fName.Substring(5, 3);
                        if (int.TryParse(numberPart, out int num))
                        {
                            if (num >= nextNumber)
                                nextNumber = num + 1;
                        }
                    }
                }
                string newName = $"Node_{nextNumber:D3}_";
                string newPath = Path.Combine(folderPath, newName + ".asset");
                newPath = newPath.Replace("\\", "/");
                return newPath;
            }
        }
        return path;
    }
}