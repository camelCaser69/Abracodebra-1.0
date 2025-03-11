// Assets/Scripts/Nodes/Core/PortDefinition.cs
using System;
using UnityEngine;

public enum PortType
{
    General,
    Mana,
    Condition
}

[Serializable]
public class PortDefinition
{
    public string portName;
    public PortType portType;
    public bool isInput;
}
