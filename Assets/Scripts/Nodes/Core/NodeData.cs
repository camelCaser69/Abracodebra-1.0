using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeData
{
    public string nodeId;
    public string nodeDisplayName;
    public Vector2 editorPosition; // For UI positioning
    public HexCoords coords;       // Hex-based position
    public Color backgroundColor = Color.gray;
    public string description = "";
    public List<NodePort> ports;   // Up to 6 ports (max 1 per side)
    public List<NodeEffectData> effects;

    public NodeData()
    {
        nodeId = Guid.NewGuid().ToString();
        ports = new List<NodePort>();
        effects = new List<NodeEffectData>();
        coords = new HexCoords(0, 0);
    }
}