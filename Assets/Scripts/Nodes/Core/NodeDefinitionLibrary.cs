using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NodeDefinitionLibrary", menuName = "Nodes/NodeDefinitionLibrary")]
public class NodeDefinitionLibrary : ScriptableObject
{
    public List<NodeDefinition> definitions;
    // New: List of nodes to auto-spawn at game start.
    public List<NodeDefinition> autoSpawnNodes;
}