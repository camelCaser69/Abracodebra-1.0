using UnityEngine;

public class NodeTestInitializer : MonoBehaviour
{
    [SerializeField] private NodeEditorController editorController;
    [SerializeField] private NodeExecutor nodeExecutor;
    [SerializeField] private NodeDefinitionLibrary definitionLibrary; // Ensure this is assigned.

    private NodeGraph testGraph;

    private void Start()
    {
        // Create a new graph.
        testGraph = new NodeGraph();

        // Load the graph into the editor.
        if (editorController != null)
        {
            editorController.LoadGraph(testGraph);
        }
        else
        {
            Debug.LogWarning("[NodeTestInitializer] Missing NodeEditorController reference.");
        }

        // ============================
        // AUTO-SPAWN NODES FROM LIBRARY
        // ============================
        if (definitionLibrary != null && definitionLibrary.autoSpawnNodes != null)
        {
            foreach (var nodeDef in definitionLibrary.autoSpawnNodes)
            {
                // We use a simple offset logic: each node is placed 100 units to the right/down from the previous.
                Vector2 spawnPos = new Vector2(100 * testGraph.nodes.Count, 100 * testGraph.nodes.Count);

                // -----------------
                // CREATE THE NODE
                // -----------------
                NodeData newNode = new NodeData();
                newNode.nodeDisplayName = nodeDef.displayName;
                newNode.backgroundColor = nodeDef.backgroundColor;
                newNode.description = nodeDef.description;

                // Copy NodeDefinition effects => newNode.effects
                foreach (var defEffect in nodeDef.effects)
                {
                    NodeEffectData effectCopy = new NodeEffectData
                    {
                        effectType = defEffect.effectType,
                        effectValue = defEffect.effectValue,
                        secondaryValue = defEffect.secondaryValue,
                        extra1 = defEffect.extra1,
                        extra2 = defEffect.extra2
                    };
                    
                    // Add the effect to the node
                    newNode.effects.Add(effectCopy);
                }

                // Copy NodeDefinition ports => newNode.ports
                foreach (var portDef in nodeDef.ports)
                {
                    NodePort nodePort = new NodePort
                    {
                        isInput  = portDef.isInput,
                        portType = portDef.portType,
                        side     = portDef.side
                    };
                    newNode.ports.Add(nodePort);
                }

                // Snap position based on current contentRect from the editor.
                float hexSizeValue = (HexGridManager.Instance != null) 
                    ? HexGridManager.Instance.hexSize 
                    : 50f;

                // Adjust spawnPos relative to content center.
                Vector2 adjustedSpawn = spawnPos - editorController.ContentRect.rect.center;
                HexCoords hc = HexCoords.WorldToHex(adjustedSpawn, hexSizeValue);
                newNode.coords = hc;
                Vector2 snappedPos = hc.HexToWorld(hexSizeValue) + editorController.ContentRect.rect.center;
                newNode.editorPosition = snappedPos;

                // Add the newly created node to the graph.
                testGraph.nodes.Add(newNode);

                // Create the NodeView in the editor UI.
                editorController.CreateNodeView(newNode);
            }
        }
        else
        {
            Debug.LogWarning("[NodeTestInitializer] No autoSpawnNodes defined in NodeDefinitionLibrary.");
        }

        // Pass the same graph to the executor.
        if (nodeExecutor != null)
        {
            nodeExecutor.SetGraph(testGraph);
        }
        else
        {
            Debug.LogWarning("[NodeTestInitializer] Missing NodeExecutor reference.");
        }
    }
}