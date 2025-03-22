using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class NodeExecutor : MonoBehaviour
{
    [SerializeField] private NodeGraph currentGraph;
    [Header("Debug Settings")]
    public float waitTimeBetweenNodes = 0.5f;
    public TMP_Text debugOutput; // optional UI text

    private Dictionary<HexCoords, NodeData> coordsMap;
    private HashSet<string> visited;

    private void Update()
    {
        // Press SPACE => BFS
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ExecuteGraph();
        }
    }

    public void SetGraph(NodeGraph graph)
    {
        currentGraph = graph;
        Debug.Log("[NodeExecutor] Graph set via SetGraph(). Node count=" + (graph != null ? graph.nodes.Count : 0));
    }

    public NodeGraph GetGraph() => currentGraph;

    public void ExecuteGraph()
    {
        if (currentGraph == null || currentGraph.nodes.Count == 0)
        {
            LogDebug("[NodeExecutor] No graph or no nodes to execute!");
            return;
        }
        StopAllCoroutines();
        ClearDebug();
        BuildCoordsMap();
        visited = new HashSet<string>();

        List<NodeData> startNodes = FindStartNodes();
        if (startNodes.Count == 0)
        {
            LogDebug("[NodeExecutor] No start nodes found. Aborting BFS.");
            return;
        }
        foreach (var startNode in startNodes)
        {
            StartCoroutine(RunChainBFS(startNode));
        }
    }

    private void BuildCoordsMap()
    {
        coordsMap = new Dictionary<HexCoords, NodeData>();
        foreach (var node in currentGraph.nodes)
        {
            coordsMap[node.coords] = node;
        }
    }

    private List<NodeData> FindStartNodes()
    {
        List<NodeData> result = new List<NodeData>();
        foreach (var node in currentGraph.nodes)
        {
            if (!HasInbound(node))
                result.Add(node);
        }
        return result;
    }

    private bool HasInbound(NodeData node)
    {
        foreach (var port in node.ports)
        {
            if (!port.isInput)
                continue;
            int sideIndex = SideIndex(port.side);
            int oppSide = OppositeSideIndex(sideIndex);
            HexCoords neighborCoords = HexCoords.GetNeighbor(node.coords, oppSide);
            if (!coordsMap.ContainsKey(neighborCoords))
                continue;
            var neighborNode = coordsMap[neighborCoords];
            var neighborPort = neighborNode.ports
                .FirstOrDefault(p => !p.isInput && SideIndex(p.side) == oppSide);
            if (neighborPort != null)
                return true;
        }
        return false;
    }

    private IEnumerator RunChainBFS(NodeData startNode)
    {
        Queue<HexCoords> queue = new Queue<HexCoords>();
        queue.Enqueue(startNode.coords);

        while (queue.Count > 0)
        {
            HexCoords coords = queue.Dequeue();
            if (!coordsMap.ContainsKey(coords))
                continue;
            NodeData node = coordsMap[coords];
            if (visited.Contains(node.nodeId))
                continue;
            visited.Add(node.nodeId);

            yield return new WaitForSeconds(waitTimeBetweenNodes);
            ProcessNode(node);

            // For each output side...
            foreach (var port in node.ports)
            {
                if (port.isInput) continue; // skip input
                int sIndex = SideIndex(port.side);
                HexCoords neighborCoords = HexCoords.GetNeighbor(coords, sIndex);
                if (!coordsMap.ContainsKey(neighborCoords))
                    continue;
                NodeData neighborNode = coordsMap[neighborCoords];
                int oppIndex = OppositeSideIndex(sIndex);
                bool hasInputMatch = neighborNode.ports.Any(p => p.isInput && SideIndex(p.side) == oppIndex);
                if (hasInputMatch && !visited.Contains(neighborNode.nodeId))
                {
                    queue.Enqueue(neighborCoords);
                }
            }
        }
        LogDebug("[NodeExecutor] BFS from start node completed.");
    }

    private void ProcessNode(NodeData node)
    {
        LogDebug($"[NodeExecutor] Processing node '{node.nodeDisplayName}' at coords ({node.coords.q}, {node.coords.r}).");

        // If node has an 'Output' effect, find its NodeView => call OutputNodeEffect.
        bool hasOutput = node.effects.Any(e => e.effectType == NodeEffectType.Output);
        if (hasOutput)
        {
            // Locate the NodeView in the scene with matching nodeId.
            NodeView view = FindNodeViewById(node.nodeId);
            if (view != null)
            {
                // Attempt to get OutputNodeEffect on that NodeView
                OutputNodeEffect outputComp = view.GetComponent<OutputNodeEffect>();
                if (outputComp != null)
                {
                    outputComp.Activate(); // Fire the projectile logic
                }
                else
                {
                    LogDebug("[NodeExecutor] Warning: Node has Output effect but no OutputNodeEffect component found.");
                }
            }
            else
            {
                LogDebug("[NodeExecutor] Warning: No NodeView found for Output node. Can't spawn projectile.");
            }
        }
    }
    
    // Helper method to find the NodeView with the same nodeId in the scene.
    private NodeView FindNodeViewById(string nodeId)
    {
        NodeView[] allViews = FindObjectsOfType<NodeView>();
        foreach (var v in allViews)
        {
            if (v.GetNodeData().nodeId == nodeId)
                return v;
        }
        return null;
    }
    
    private void ClearDebug()
    {
        if (debugOutput) debugOutput.text = "";
    }

    private void LogDebug(string msg)
    {
        Debug.Log(msg);
        if (debugOutput)
            debugOutput.text += msg + "\n";
    }

    private int SideIndex(HexSideFlat side)
    {
        // Top=0, One=1, Two=2, Three=3, Four=4, Five=5
        return (int)side;
    }

    private int OppositeSideIndex(int sideIndex)
    {
        // sideIndex + 3 mod 6
        return (sideIndex + 3) % 6;
    }
}
