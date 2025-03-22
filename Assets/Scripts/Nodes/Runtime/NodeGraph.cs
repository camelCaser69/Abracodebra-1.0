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