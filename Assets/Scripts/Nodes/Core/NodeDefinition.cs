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
            {
                displayName = string.Join("_", parts.Skip(2).ToArray());
            }
        }
    }
#endif

    private void OnEnable()
    {
        // Auto-add default General ports if none exist.
        if (ports == null || ports.Count == 0)
        {
            ports = new List<PortDefinition>();
            PortDefinition inputPort = new PortDefinition { portName = "Input", portType = PortType.General, isInput = true };
            PortDefinition outputPort = new PortDefinition { portName = "Output", portType = PortType.General, isInput = false };
            ports.Add(inputPort);
            ports.Add(outputPort);
        }
    }
}