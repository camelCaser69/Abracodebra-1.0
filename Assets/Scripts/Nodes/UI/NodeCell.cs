using UnityEngine;
using UnityEngine.EventSystems;

public class NodeCell : MonoBehaviour, IPointerClickHandler
{
    public int cellIndex;
    private NodeEditorGridController controller;
    private NodeData nodeData;   // Null if cell is empty.
    private NodeView nodeView;   // Reference to NodeView child, if any.

    public void Init(int index, NodeEditorGridController gridController)
    {
        cellIndex = index;
        controller = gridController;
    }

    public bool HasNode()
    {
        return nodeData != null;
    }

    public NodeData GetNodeData()
    {
        return nodeData;
    }

    // Returns the NodeView component (if any)
    public NodeView GetNodeView()
    {
        return nodeView;
    }

    // Called when the user selects a node definition from the dropdown.
    public void SetNodeDefinition(NodeDefinition def)
    {
        // Create new NodeData using the definition.
        nodeData = new NodeData()
        {
            nodeId = System.Guid.NewGuid().ToString(),
            nodeDisplayName = def.displayName,
            effects = def.CloneEffects() // Using the method defined in NodeDefinition.
        };

        // Remove any existing NodeView.
        if (nodeView != null)
        {
            Destroy(nodeView.gameObject);
            nodeView = null;
        }

        // Determine which NodeView prefab to use:
        GameObject prefabToUse = def.nodeViewPrefab != null ? def.nodeViewPrefab : controller.defaultNodeViewPrefab;
        if (prefabToUse == null)
        {
            Debug.LogError("No NodeView prefab assigned in NodeDefinition or default in NodeEditorGridController.");
            return;
        }

        // Instantiate the NodeView prefab as a child of this cell.
        GameObject nodeViewObj = Instantiate(prefabToUse, transform);
        nodeView = nodeViewObj.GetComponent<NodeView>();
        if (nodeView != null)
        {
            nodeView.Initialize(nodeData, def.thumbnail, def.backgroundColor, def.description, nodeData.effects);
        }
        
        

    }

    // On right-click, if empty, notify the controller to show the dropdown.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (!HasNode())
            {
                controller.OnEmptyCellRightClicked(this, eventData);
            }
            else
            {
                // Optionally, add logic for right-click on an occupied cell.
            }
        }
    }
}
