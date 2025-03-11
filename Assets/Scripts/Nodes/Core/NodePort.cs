/* Assets/Scripts/Nodes/Core/NodePort.cs
   Represents a single port on a NodeData (input or output). */

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodePort
{
    public string portId;
    public string portName;
    public PortType portType;
    public List<string> connectedPortIds;

    public NodePort()
    {
        portId = Guid.NewGuid().ToString();
        connectedPortIds = new List<string>();
    }
}
