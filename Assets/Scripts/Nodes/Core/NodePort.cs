// Assets/Scripts/Nodes/Core/NodePort.cs
// Represents a single port on a NodeData (input or output).

using System;
using System.Collections.Generic;

[Serializable]
public class NodePort
{
    public string portId;
    public string portName;    // e.g., "In", "Out", "ManaIn"
    public string portType;    // e.g., "Mana", "Trigger", "Float"
    
    // Each port can connect to multiple other ports, stored by their unique IDs.
    public List<string> connectedPortIds;

    public NodePort()
    {
        portId = Guid.NewGuid().ToString();
        connectedPortIds = new List<string>();
    }
}