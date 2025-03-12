using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeGraph
{
    public List<NodeData> nodes;
    [NonSerialized]
    public Dictionary<string, List<string>> adjacency;

    public NodeGraph()
    {
        nodes = new List<NodeData>();
        adjacency = new Dictionary<string, List<string>>();
    }
}