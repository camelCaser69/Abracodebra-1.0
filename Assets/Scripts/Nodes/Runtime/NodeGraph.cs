using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeGraph
{
    public List<NodeData> nodes;

    // Dictionary for general connections: maps a node's ID to a list of child node IDs.
    [NonSerialized]
    public Dictionary<string, List<string>> adjacency;

    // Dictionary for mana connections: maps a target node's ID to the source node's ID.
    [NonSerialized]
    public Dictionary<string, string> manaConnections;

    public NodeGraph()
    {
        nodes = new List<NodeData>();
        adjacency = new Dictionary<string, List<string>>();
        manaConnections = new Dictionary<string, string>();
    }
}