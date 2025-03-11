// Assets/Scripts/Nodes/Runtime/NodeGraph.cs
// Holds a list of NodeData to form a single node graph.

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeGraph
{
    public List<NodeData> nodes;

    public NodeGraph()
    {
        nodes = new List<NodeData>();
    }
}