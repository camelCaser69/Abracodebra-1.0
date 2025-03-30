using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;

public class NodeEditorGridController : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("Maximum number of cells (gene capacity)")]
    public int maxCells = 8;
    [Tooltip("Spacing between cells (pixels)")]
    public float cellSpacing = 10f;
    [Tooltip("Size (width/height in pixels) for each cell")]
    public float cellSize = 64f;
    [Tooltip("Parent RectTransform where cells will be instantiated")]
    public RectTransform cellsParent;

    [Header("Prefabs & Library")]
    [Tooltip("Prefab for an empty cell")]
    public NodeCell cellPrefab;
    [Tooltip("Default NodeView prefab (used if a NodeDefinition has none assigned)")]
    public GameObject defaultNodeViewPrefab;
    [Tooltip("Library of node definitions")]
    public NodeDefinitionLibrary definitionLibrary;

    [Header("TMP Dropdown")]
    [Tooltip("TMP_Dropdown used for node selection (should be inactive by default)")]
    public TMP_Dropdown nodeDropdown;

    [Header("Execution")]
    [Tooltip("Reference to the NodeExecutor to update its graph")]
    public NodeExecutor nodeExecutor;

    // List of instantiated cell references
    private List<NodeCell> nodeCells = new List<NodeCell>();

    private void Start()
    {
        if (nodeDropdown != null)
            nodeDropdown.gameObject.SetActive(false);

        CreateCells();

        if (nodeExecutor != null)
            nodeExecutor.SetGraph(new NodeGraph());

        RefreshGraph();
    }

    // Create exactly maxCells cells, arranged in one centered row.
    private void CreateCells()
    {
        // Clear previous children if any.
        foreach (Transform child in cellsParent)
        {
            Destroy(child.gameObject);
        }
        nodeCells.Clear();

        // Total width = (maxCells * cellSize) + ((maxCells - 1) * cellSpacing)
        float totalWidth = maxCells * cellSize + (maxCells - 1) * cellSpacing;
        // Starting x position (centered): -totalWidth/2 + cellSize/2
        float startX = -totalWidth / 2f + cellSize / 2f;

        for (int i = 0; i < maxCells; i++)
        {
            NodeCell cell = Instantiate(cellPrefab, cellsParent);
            RectTransform rt = cell.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(cellSize, cellSize);
            float xPos = startX + i * (cellSize + cellSpacing);
            rt.anchoredPosition = new Vector2(xPos, 0f);
            cell.Init(i, this);
            nodeCells.Add(cell);
        }
    }

    // Called by a NodeCell when right-clicked (and empty)
    public void OnEmptyCellRightClicked(NodeCell cell, PointerEventData eventData)
    {
        if (nodeDropdown == null || definitionLibrary == null) return;

        // Build dropdown options using TMP_Dropdown.OptionData.
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData("Select Node")); // default option
        foreach (var def in definitionLibrary.definitions)
        {
            options.Add(new TMP_Dropdown.OptionData(def.displayName));
        }
        nodeDropdown.ClearOptions();
        nodeDropdown.AddOptions(options);
        nodeDropdown.value = 0;
        nodeDropdown.RefreshShownValue();

        // Position dropdown at pointer.
        RectTransform dropdownRect = nodeDropdown.GetComponent<RectTransform>();
        dropdownRect.position = eventData.position;

        nodeDropdown.gameObject.SetActive(true);
        nodeDropdown.onValueChanged.RemoveAllListeners();
        nodeDropdown.onValueChanged.AddListener((val) => OnDropdownValueChanged(val, cell));
    }

    private void OnDropdownValueChanged(int value, NodeCell cell)
    {
        nodeDropdown.gameObject.SetActive(false);
        if (value == 0) return; // "Select Node" option
        int index = value - 1;
        if (index >= 0 && index < definitionLibrary.definitions.Count)
        {
            NodeDefinition def = definitionLibrary.definitions[index];
            cell.SetNodeDefinition(def);
            RefreshGraph();
        }
    }

    // Rebuild the NodeGraph from all non-empty cells, in order from left to right.
    public void RefreshGraph()
    {
        if (nodeExecutor == null || nodeExecutor.currentGraph == null) return;
        nodeExecutor.currentGraph.nodes.Clear();
        foreach (var cell in nodeCells.OrderBy(c => c.cellIndex))
        {
            if (cell.HasNode())
            {
                nodeExecutor.currentGraph.nodes.Add(cell.GetNodeData());
            }
        }
    }

    // Called by NodeDraggable when a node is dropped; checks if the drop is on a cell.
    public bool HandleNodeDrop(NodeDraggable draggedNode, Vector2 screenPosition)
    {
        foreach (var cell in nodeCells)
        {
            RectTransform cellRect = cell.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(cellRect, screenPosition))
            {
                // If the cell already has a node, swap them.
                if (cell.HasNode())
                {
                    NodeView existingView = cell.GetNodeView();
                    if (existingView != null)
                    {
                        // Get original cell of dragged node.
                        NodeCell originCell = draggedNode.GetComponentInParent<NodeCell>();
                        if (originCell != null)
                        {
                            existingView.transform.SetParent(originCell.transform, false);
                            existingView.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                        }
                    }
                }
                // Place the dragged node in this cell.
                draggedNode.SetParent(cell.transform);
                return true;
            }
        }
        return false;
    }
}
