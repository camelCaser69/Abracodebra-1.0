using UnityEditor;
using System.IO;

public class NodeDefinitionAutoRenamer : AssetModificationProcessor
{
    static string OnWillCreateAsset(string path)
    {
        if (path.EndsWith(".asset"))
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            // If the file name is still the default "NodeDefinition"
            if (fileName == "NodeDefinition")
            {
                string folderPath = Path.GetDirectoryName(path);
                string[] files = Directory.GetFiles(folderPath, "Node_*", SearchOption.TopDirectoryOnly);
                int nextNumber = 1;
                foreach (var file in files)
                {
                    string fName = Path.GetFileNameWithoutExtension(file);
                    if (fName.StartsWith("Node_") && fName.Length >= 8)
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
                return Path.Combine(Path.GetDirectoryName(path), newName + ".asset").Replace("\\", "/");
            }
        }
        return path;
    }
}