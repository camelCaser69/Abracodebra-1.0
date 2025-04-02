using UnityEngine;
using System.Collections.Generic;
using System; // Needed for [Serializable]

// Define the configuration for a single initial node
[Serializable] // Make it visible and editable in the Inspector
public struct InitialNodeConfig
{
    [Tooltip("The Node Definition to spawn.")]
    public NodeDefinition nodeDefinition;

    [Tooltip("The zero-based index of the cell where this node should spawn (0 is the leftmost cell).")]
    [Min(0)]
    public int cellIndex;

    [Tooltip("Can the player drag this initial node to other cells?")]
    public bool canMove;

    [Tooltip("Can the player delete this initial node using the Delete key?")]
    public bool canDelete;
}


[CreateAssetMenu(fileName = "NodeDefinitionLibrary", menuName = "Nodes/NodeDefinitionLibrary")]
public class NodeDefinitionLibrary : ScriptableObject
{
    [Header("Available Node Definitions")]
    [Tooltip("List of all Node Definitions available in the dropdown menu.")]
    public List<NodeDefinition> definitions; // Your existing list

    [Header("Initial Node Layout")]
    [Tooltip("Nodes to automatically spawn in specific slots when the grid initializes.")]
    public List<InitialNodeConfig> initialNodes; // <<< NEW LIST ADDED
}