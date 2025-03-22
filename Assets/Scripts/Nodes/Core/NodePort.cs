using System;
using UnityEngine;

public enum PortType { General, Mana, Condition }

public enum HexSideFlat { Top, One, Two, Three, Four, Five }

[Serializable]
public class NodePort
{
    public bool isInput;
    public PortType portType;
    public HexSideFlat side;

    public NodePort(bool isInput, PortType portType, HexSideFlat side)
    {
        this.isInput = isInput;
        this.portType = portType;
        this.side = side;
    }

    public NodePort() { }
}