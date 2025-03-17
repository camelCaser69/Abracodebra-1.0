using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Added for Skip()

[CreateAssetMenu(fileName = "NodeDefinition", menuName = "Nodes/NodeDefinition")]
public class NodeDefinition : ScriptableObject
{
    public string displayName;
    public Color backgroundColor = Color.gray;
    [TextArea] public string description; // This is used to auto-fill NodeData.description later.
    public List<PortDefinition> ports;
    public List<NodeEffectData> effects = new List<NodeEffectData>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            // Expected format: Node_XXX_[Suffix]
            string[] parts = fileName.Split('_');
            if (parts.Length >= 3)
            {
                // Use everything after the numeric part as the display name.
                displayName = string.Join("_", parts.Skip(2).ToArray());
            }
        }
    }
#endif
}