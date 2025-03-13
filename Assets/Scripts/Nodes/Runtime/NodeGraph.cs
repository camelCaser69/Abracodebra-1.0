using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeGraph
{
    public List<NodeData> nodes;
    [NonSerialized] public Dictionary<string, List<string>> adjacency;
    [NonSerialized] public Dictionary<string, string> manaConnections;

    public NodeGraph()
    {
        nodes = new List<NodeData>();
        adjacency = new Dictionary<string, List<string>>();
        manaConnections = new Dictionary<string, string>();
    }
}