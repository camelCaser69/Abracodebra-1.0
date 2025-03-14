﻿// NodeDefinition.cs (unchanged except for your new effects in the list)
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NodeDefinition", menuName = "Nodes/NodeDefinition")]
public class NodeDefinition : ScriptableObject
{
    public string displayName;
    public Color backgroundColor = Color.gray;
    [TextArea] public string description;

    public List<PortDefinition> ports;
    public List<NodeEffectData> effects = new List<NodeEffectData>();
}