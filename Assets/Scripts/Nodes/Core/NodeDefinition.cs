using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NodeDefinition", menuName = "Nodes/NodeDefinition")]
public class NodeDefinition : ScriptableObject
{
    public string displayName;
    public Color backgroundColor = Color.gray;
    [TextArea]
    public string description;
    public Sprite thumbnail; // Thumbnail for UI

    // Effects remain unchanged (you can add/remove variables as needed)
    public List<NodeEffectData> effects = new List<NodeEffectData>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            displayName = fileName;
        }
    }
#endif
}