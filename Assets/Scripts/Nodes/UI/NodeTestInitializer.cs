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

        // Auto-spawn nodes from the library.
        if (definitionLibrary != null && definitionLibrary.autoSpawnNodes != null)
        {
            foreach (var nodeDef in definitionLibrary.autoSpawnNodes)
            {
                // Here we simply call CreateNodeAtMouse, but you might want to specify positions.
                // For demonstration, we use a fixed position offset (e.g., based on index).
                Vector2 spawnPos = new Vector2(100 * testGraph.nodes.Count, 100 * testGraph.nodes.Count);
                NodeData newNode = new NodeData();
                newNode.nodeDisplayName = nodeDef.displayName;
                newNode.backgroundColor = nodeDef.backgroundColor;
                newNode.description = nodeDef.description;
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
                    newNode.effects.Add(effectCopy);
                }
                foreach (var portDef in nodeDef.ports)
                {
                    NodePort nodePort = new NodePort
                    {
                        isInput = portDef.isInput,
                        portType = portDef.portType,
                        side = portDef.side
                    };
                    newNode.ports.Add(nodePort);
                }

                // Snap position based on current contentRect.
                float hexSizeValue = (HexGridManager.Instance != null) ? HexGridManager.Instance.hexSize : 50f;
                // Adjust spawnPos relative to content center.
                Vector2 adjustedSpawn = spawnPos - editorController.ContentRect.rect.center;
                HexCoords hc = HexCoords.WorldToHex(adjustedSpawn, hexSizeValue);
                newNode.coords = hc;
                Vector2 snappedPos = hc.HexToWorld(hexSizeValue) + editorController.ContentRect.rect.center;
                newNode.editorPosition = snappedPos;

                testGraph.nodes.Add(newNode);
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
