// FILE: Assets/Scripts/Core/CircularReferencesCleaner.cs
using UnityEngine;
using System.Collections;

public class CircularReferencesCleaner : MonoBehaviour
{
    private static CircularReferencesCleaner instance;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (instance == null)
        {
            GameObject cleanerGO = new GameObject("CircularReferencesCleaner");
            instance = cleanerGO.AddComponent<CircularReferencesCleaner>();
            DontDestroyOnLoad(cleanerGO);
        }
    }
    
    void Awake()
    {
        StartCoroutine(ContinuousCleanup());
    }
    
    IEnumerator ContinuousCleanup()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            // Clean all NodeViews
            NodeView[] nodeViews = FindObjectsOfType<NodeView>();
            foreach (var nodeView in nodeViews)
            {
                var nodeData = nodeView.GetNodeData();
                if (nodeData != null)
                {
                    nodeData.ForceCleanNestedSequences();
                }
            }
            
            // Clean all ToolViews
            ToolView[] toolViews = FindObjectsOfType<ToolView>();
            foreach (var toolView in toolViews)
            {
                var nodeData = toolView.GetNodeData();
                if (nodeData != null)
                {
                    nodeData.storedSequence = null; // Tools never have sequences
                }
            }
            
            // Clean PlantGrowth
            PlantGrowth[] plants = FindObjectsOfType<PlantGrowth>();
            foreach (var plant in plants)
            {
                // Use reflection to access private field
                var nodeGraphField = typeof(PlantGrowth).GetField("nodeGraph", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (nodeGraphField != null)
                {
                    NodeGraph graph = nodeGraphField.GetValue(plant) as NodeGraph;
                    if (graph != null && graph.nodes != null)
                    {
                        foreach (var node in graph.nodes)
                        {
                            if (node != null)
                            {
                                node.ForceCleanNestedSequences();
                            }
                        }
                    }
                }
            }
        }
    }
}