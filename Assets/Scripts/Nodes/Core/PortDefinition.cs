// Assets/Scripts/Nodes/Core/PortDefinition.cs
using System;
using UnityEngine;

[Serializable]
public class PortDefinition
{
    public string portName;
    public string portType; // e.g. "Mana", "General"
    
    // Distinguish input vs. output (or use an enum if you prefer).
    public bool isInput;
}