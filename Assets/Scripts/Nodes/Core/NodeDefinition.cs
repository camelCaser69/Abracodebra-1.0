using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "NodeDefinition", menuName = "Nodes/NodeDefinition")]
public class NodeDefinition : ScriptableObject
{
    public string displayName;
    public Color backgroundColor = Color.gray;
    [TextArea] public string description;
    public List<PortDefinition> ports;
    public List<NodeEffectData> effects = new List<NodeEffectData>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            string[] parts = fileName.Split('_');
            if (parts.Length >= 3)
                displayName = string.Join("_", parts.Skip(2).ToArray());
        }
    }
#endif

    private void OnEnable()
    {
        if (ports == null || ports.Count == 0)
        {
            ports = new List<PortDefinition>();
            // Default: one input on Top and one output on Three (opposite for flat-top)
            ports.Add(new PortDefinition { isInput = true, portType = PortType.General, side = HexSideFlat.Top });
            ports.Add(new PortDefinition { isInput = false, portType = PortType.General, side = HexSideFlat.Three });
        }
    }
}